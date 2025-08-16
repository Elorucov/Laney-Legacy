using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects.Auth;
using QRCoder;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Pages.Dialogs {

    public sealed partial class QRAuthUI : Modal {
        static string _anonymToken = null;
        string _authHash = null;
        CancellationTokenSource _cts = new CancellationTokenSource();

        public QRAuthUI() {
            this.InitializeComponent();
            Closed += QRAuthUI_Closed;
        }

        private void QRAuthUI_Closed(object sender, object e) {
            Closed -= QRAuthUI_Closed;
            _cts.Cancel();
        }

        private void Initialize(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                try {
                    if (string.IsNullOrEmpty(_anonymToken)) {
                        var response = await DirectAuth.GetAnonymTokenAsync(AppParameters.VKMApplicationID, AppParameters.VKMSecret);
                        _anonymToken = response.Token;
                    }

                    var resp2 = await Auth.GetAuthCode(_anonymToken, Locale.Get("lang"), $"Laney v{ApplicationInfo.GetVersion(true)}", AppParameters.VKMApplicationID);
                    if (resp2 is GetAuthCodeResponse authCode) {
                        _authHash = authCode.AuthHash;

                        await GenerateQR(authCode.AuthUrl);
                        LoadingPhase.Visibility = Visibility.Collapsed;
                        FirstPhase.Visibility = Visibility.Visible;

                        RunCodeCheckerTask();
                    } else {
                        Functions.ShowHandledErrorDialog(resp2);
                        Hide();
                    }
                } catch (Exception ex) {
                    Functions.ShowHandledErrorDialog(ex);
                    Hide();
                }
            })();
        }

        private async Task GenerateQR(string url) {
            AuthLinkButton.Content = url;

            PayloadGenerator.Url gen = new PayloadGenerator.Url(url);
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(gen.ToString(), QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData)) {
                byte[] qrCodeImagePng = qrCode.GetGraphic(20, new byte[] { 0, 0, 0 }, new byte[] { 255, 255, 255 });
                using (var stream = new InMemoryRandomAccessStream()) {
                    using (var writer = new DataWriter(stream.GetOutputStreamAt(0))) {
                        writer.WriteBytes(qrCodeImagePng);
                        await writer.StoreAsync();
                    }
                    var image = new BitmapImage();
                    await image.SetSourceAsync(stream);

                    QR.Source = image;
                }
            }
        }

        private void RunCodeCheckerTask() {
            Task task = Task.Factory.StartNew(async () => await CheckAuthCode(), _cts.Token);
        }

        private async Task CheckAuthCode() {
            bool loop = true;
            while (loop) {
                try {
                    var response = await Auth.CheckAuthCode(_anonymToken, Locale.Get("lang"), AppParameters.VKMApplicationID, _authHash);
                    if (response is CheckAuthCodeResponse checkResult) {
                        switch (checkResult.Status) {
                            case 0:
                                break;
                            case 1:
                                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                                    FirstPhase.Visibility = Visibility.Collapsed;
                                    SecondPhase.Visibility = Visibility.Visible;
                                });
                                break;
                            case 2:
                            case 3:
                                loop = false;
                                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                                    Hide(checkResult.Status == 3 ? null : new Tuple<long, string, string>(checkResult.UserId, checkResult.AccessToken, _anonymToken));
                                    _anonymToken = null;
                                });
                                break;
                            case 4:
                                loop = false;
                                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                                    FirstPhase.Visibility = Visibility.Collapsed;
                                    SecondPhase.Visibility = Visibility.Collapsed;
                                    ErrorPhase.Visibility = Visibility.Visible;
                                });
                                break;
                            default:
                                loop = false;
                                break;
                        }
                    } else {
                        var err = Functions.GetNormalErrorInfo(response);
                        Log.Error($"API error while requesting auth.checkAuthCode! {err.Item1}: {err.Item2}");
                    }
                    await Task.Delay(2000);
                } catch (TaskCanceledException) {
                } catch (Exception ex) {
                    Log.Error($"A unhandled exception occured while requesting auth.checkAuthCode: {ex.Message}.");
                }
            }
        }
    }
}
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Auth;
using System;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    public sealed partial class DirectAuthPage : Page {
        enum DirectAuth2Flow { Login, Password, PhoneValidation, PhoneValidationFinal, TwoFactor, Captcha, SecondPhase }

        public DirectAuthPage() {
            this.InitializeComponent();
            if (Theme.IsMicaAvailable) Background = null;
            Loaded += async (a, b) => {
                SetUpStatusBar();
                await GetAnonymToken();
            };
            AuthBtn.Click += async (a, b) => await DoAuth();
            GoToFlow(DirectAuth2Flow.Login);
        }

        private void GoBack(object sender, RoutedEventArgs e) {
            Frame.GoBack();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            if (e.NewSize.Width > 576) {
                Box.Width = 480;
                Box.MaxHeight = 540;
                BoxBackground.Visibility = Visibility.Visible;
                if (AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile") BoxBottomPadding.Visibility = Visibility.Visible;
            } else {
                Box.Width = Double.NaN;
                Box.MaxHeight = Double.PositiveInfinity;
                BoxBackground.Visibility = Visibility.Collapsed;
                if (AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile") BoxBottomPadding.Visibility = Visibility.Collapsed;
            }
        }

        private void SetUpStatusBar() {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile") {
                DisplayInformation di = DisplayInformation.GetForCurrentView();
                SetUpStatusBar(di.CurrentOrientation);
                di.OrientationChanged += (a, b) => {
                    SetUpStatusBar(a.CurrentOrientation);
                };
                UpdateLayoutDueNavBar(ApplicationView.GetForCurrentView().VisibleBounds);
                ApplicationView.GetForCurrentView().VisibleBoundsChanged += (c, d) => {
                    UpdateLayoutDueNavBar(c.VisibleBounds);
                };
            } else {
                BoxTopPadding.Height = 32;
                BoxBottomPadding.Height = 32;
            }
        }

        private void UpdateLayoutDueNavBar(Rect vb) {
            DisplayInformation di = DisplayInformation.GetForCurrentView();
            Rect ws = Window.Current.Bounds;
            if (di.CurrentOrientation == DisplayOrientations.Portrait || di.CurrentOrientation == DisplayOrientations.PortraitFlipped) {
                BoxBottomPadding.Height = ws.Height - vb.Bottom;
            } else {
                BoxBottomPadding.Height = 0;
            }
        }

        private void SetUpStatusBar(DisplayOrientations currentOrientation) {
            StatusBar sb = StatusBar.GetForCurrentView();
            if (currentOrientation == DisplayOrientations.Portrait || currentOrientation == DisplayOrientations.PortraitFlipped) {
                ApplicationView.GetForCurrentView().ExitFullScreenMode();
                BoxTopPadding.Height = sb.OccludedRect.Height;
            } else {
                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            }
        }

        //

        private void OnTextBoxKetUp(object sender, KeyRoutedEventArgs e) {
            if (e.Key == VirtualKey.Enter) new System.Action(async () => { await DoAuth(); })();
        }

        DirectAuth2Flow currentFlow = DirectAuth2Flow.Login;
        Panel currentFlowPanel = null;
        bool isPhoneConfirmation = false;

        private void GoToFlow(DirectAuth2Flow flow) {
            if (currentFlowPanel != null) currentFlowPanel.Visibility = Visibility.Collapsed;
            currentFlow = flow;
            foreach (FrameworkElement flows in FlowsRoot.Children) {
                if (flows != null) flows.Visibility = Visibility.Collapsed;
            }
            AdditionalArea.Child = null;
            AuthBtn.Content = Locale.Get("continue");

            FlowTitle.Visibility = flow == DirectAuth2Flow.Login ? Visibility.Collapsed : Visibility.Visible;
            switch (flow) {
                case DirectAuth2Flow.Login:
                    LoginFlow.Visibility = Visibility.Visible;
                    currentFlowPanel = LoginFlow;
                    break;
                case DirectAuth2Flow.Password:
                    FindName(nameof(PasswordFlow));
                    PasswordFlow.Visibility = Visibility.Visible;
                    Password.Focus(FocusState.Programmatic);
                    currentFlowPanel = PasswordFlow;
                    FlowTitle.Text = Locale.Get("password_flow_title");
                    break;
                case DirectAuth2Flow.PhoneValidation:
                    FindName(nameof(PhoneValidationFlow));
                    PhoneValidationFlow.Visibility = Visibility.Visible;
                    PVCode.Focus(FocusState.Programmatic);
                    currentFlowPanel = PhoneValidationFlow;
                    FlowTitle.Text = Locale.Get("pv_flow_title");
                    break;
                case DirectAuth2Flow.PhoneValidationFinal:
                    FindName(nameof(PhoneValidFinalFlow));
                    PhoneValidFinalFlow.Visibility = Visibility.Visible;
                    Password2.Focus(FocusState.Programmatic);
                    currentFlowPanel = PhoneValidFinalFlow;
                    FlowTitle.Text = Locale.Get("password_flow_title");
                    break;
                case DirectAuth2Flow.TwoFactor:
                    FindName(nameof(TwoFAFlow));
                    TwoFAFlow.Visibility = Visibility.Visible;
                    TwoFACode.Focus(FocusState.Programmatic);
                    currentFlowPanel = TwoFAFlow;
                    FlowTitle.Text = Locale.Get("2fa_flow_title");
                    break;
                case DirectAuth2Flow.Captcha:
                    FindName(nameof(CaptchaFlow));
                    CaptchaFlow.Visibility = Visibility.Visible;
                    CaptchaCode.Focus(FocusState.Programmatic);
                    currentFlowPanel = CaptchaFlow;
                    FlowTitle.Text = String.Empty;
                    break;
                case DirectAuth2Flow.SecondPhase:
                    FindName(nameof(SecondPhaseAuthFlow));
                    SecondPhaseAuthFlow.Visibility = Visibility.Visible;
                    currentFlowPanel = SecondPhaseAuthFlow;
                    FlowTitle.Text = String.Empty;
                    BackButton.IsEnabled = false;
                    AuthBtn.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        #region Anonym token from VKM

        string anonymToken = null;

        private async Task GetAnonymToken() {
            try {
                ScreenSpinner<AnonymToken> ssp = new ScreenSpinner<AnonymToken>();
                var resp = await ssp.ShowAsync(DirectAuth.GetAnonymTokenAsync(AppParameters.ApplicationID, AppParameters.ApplicationSecret));
                anonymToken = resp.Token;
                Log.Info($"{nameof(DirectAuthPage)}: successfully get an anonym token.");
            } catch (Exception ex) {
                Log.Error($"{nameof(DirectAuthPage)}: failed to get an anonym token! 0x{ex.HResult.ToString("x8")}: {ex.Message.Trim()}");
                Functions.ShowHandledErrorDialog(ex);
                Frame.GoBack();
            }
        }

        #endregion

        private void ShowError(string err) {
            Error.Visibility = String.IsNullOrEmpty(err) ? Visibility.Collapsed : Visibility.Visible;
            if (!String.IsNullOrEmpty(err)) Error.Text = err;
        }

        private async Task DoAuth() {
            switch (currentFlow) {
                case DirectAuth2Flow.Login:
                    await CheckLogin();
                    break;
                case DirectAuth2Flow.Password:
                case DirectAuth2Flow.PhoneValidationFinal:
                case DirectAuth2Flow.TwoFactor:
                case DirectAuth2Flow.Captcha:
                    await DoAuthDirect();
                    break;
                case DirectAuth2Flow.PhoneValidation:
                    await ValidatePhoneConfirm();
                    break;
            }
        }

        private async Task CheckLogin() {
            if (String.IsNullOrEmpty(UserName.Text)) return;
            AuthBtn.IsEnabled = false;
            ShowError(null);

            string login = UserName.Text;
            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            var response = await ssp.ShowAsync(Auth.ValidateLogin(anonymToken, Locale.Get("lang"), Functions.GetDeviceId(), UserName.Text));

            AuthBtn.IsEnabled = true;

            if (response is ValidateLoginResponse resp) {
                UserName.Text = login;
                if (resp.IsEmail && resp.EmailRegAllowed) {
                    ShowError(Locale.Get("pv_profile_not_exist"));
                    return;
                }
                if (resp.Result == "need_password") {
                    GoToFlow(DirectAuth2Flow.Password);
                    PasswordInfo.Text = String.Format(Locale.GetForFormat("password_flow"), login);
                } else if (resp.Result == "need_phone_confirm") {
                    await ValidatePhone(anonymToken, resp.SID, resp.Phone);
                } else {
                    ShowError($"{Locale.Get("global_error")}!\nUnknown validation result: {resp.Result}");
                }
            } else {
                if (response is VKErrorResponse vkerr && vkerr.error.error_code == 5400) {
                    ShowError(Locale.Get("pv_profile_not_exist"));
                }
                var info = Functions.GetNormalErrorInfo(response);
                ShowError($"{info.Item1}\n{info.Item2}");
            }
        }

        private async Task ValidatePhone(string anonymToken, string sid, string phone) {
            AuthBtn.IsEnabled = false;

            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            var pvresponse = await ssp.ShowAsync(Auth.ValidatePhone(anonymToken, Locale.Get("lang"), Functions.GetDeviceId(), sid, String.Empty));
            if (pvresponse is ValidatePhoneResponse pvresp) {
                GoToFlow(DirectAuth2Flow.PhoneValidation);
                PVCode.MaxLength = pvresp.CodeLength;
                PVCode.Tag = pvresp.SID;
                PVCode.Text = String.Empty;

                switch (pvresp.ValidationType) {
                    case "sms":
                        PVInfo.Text = String.Format(Locale.GetForFormat("2fa_sms"), phone);
                        break;
                    case "callreset":
                        PVInfo.Text = String.Format(Locale.GetForFormat("2fa_callreset"), phone);
                        break;
                    case "push":
                        PVInfo.Text = Locale.Get("2fa_push");
                        break;
                    case "email":
                        PVInfo.Text = String.Format(Locale.GetForFormat("2fa_email"), pvresp.MaskedEmail);
                        break;
                    default:
                        PVInfo.Text = pvresp.ValidationType;
                        break;
                }

                if (pvresp.ValidationResend != pvresp.ValidationType) AddRevalidateButton(anonymToken, sid, phone);
            } else {
                var info = Functions.GetNormalErrorInfo(pvresponse);
                ShowError($"{info.Item1}\n{info.Item2}");
            }

            AuthBtn.IsEnabled = true;
        }

        private void AddRevalidateButton(string anonymToken, string sid, string phone) {
            Button btn = new Button {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = Locale.Get("pv_other_method")
            };

            btn.Click += async (a, b) => {
                btn.IsEnabled = false;
                await ValidatePhone(anonymToken, sid, phone);
            };

            AdditionalArea.Child = btn;
        }

        private async Task ValidatePhoneConfirm() {
            if (String.IsNullOrEmpty(PVCode.Text)) return;
            AuthBtn.IsEnabled = false;
            ShowError(null);

            string sid = PVCode.Tag as string;
            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            var response = await ssp.ShowAsync(Auth.ValidatePhoneConfirm(anonymToken, Locale.Get("lang"), Functions.GetDeviceId(), sid, UserName.Text, PVCode.Text));

            AuthBtn.IsEnabled = true;

            if (response is ValidatePhoneConfirmResponse resp) {
                if (!resp.ProfileExist) {
                    await new ContentDialog {
                        Title = Locale.Get("global_error"),
                        Content = Locale.Get("pv_profile_not_exist"),
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                    Frame.GoBack();
                } else {
                    GoToFlow(DirectAuth2Flow.PhoneValidationFinal);
                    isPhoneConfirmation = true;

                    if (!String.IsNullOrEmpty(resp.Profile.Photo)) await ProfileAvatar.SetUriSourceAsync(new Uri(resp.Profile.Photo));
                    ProfileName.Text = $"{resp.Profile.FirstName} {resp.Profile.LastName}";
                    ProfileAvatar.DisplayName = ProfileName.Text;
                    ProfilePhone.Text = resp.Profile.Phone;
                    Password2.Tag = resp.SID;
                }
            } else {
                var info = Functions.GetNormalErrorInfo(response);
                ShowError($"{info.Item1}\n{info.Item2}");
            }
        }

        private async Task DoAuthDirect() {
            if (String.IsNullOrEmpty(UserName.Text)) return;
            if (isPhoneConfirmation) {
                if (String.IsNullOrEmpty(Password2.Password) || Password2.Password.Length < 6) return;
            } else {
                if (String.IsNullOrEmpty(Password.Password) || Password.Password.Length < 6) return;
            }

            AuthBtn.IsEnabled = false;
            ShowError(null);

            try {
                string code = null;
                string sid = null;
                string captchaSid = null;
                string captchaKey = null;

                if (currentFlow == DirectAuth2Flow.TwoFactor) {
                    code = TwoFACode.Text;
                } else if (currentFlow == DirectAuth2Flow.Captcha) {
                    captchaSid = CaptchaImg.Tag.ToString();
                    captchaKey = CaptchaCode.Text;
                } else if (isPhoneConfirmation) {
                    sid = Password2.Tag as string;
                }

                ScreenSpinner<DirectAuthResponse> ssp = new ScreenSpinner<DirectAuthResponse>();
                var response = !isPhoneConfirmation
                    ? await ssp.ShowAsync(DirectAuth.AuthAsync(Locale.Get("lang"), AppParameters.ApplicationID, AppParameters.ApplicationSecret, AppParameters.Scope, UserName.Text, Password.Password,
                    code, captchaSid, captchaKey))
                    : await ssp.ShowAsync(DirectAuth.AuthByPhoneConfirmationSIDAsync(Locale.Get("lang"), AppParameters.ApplicationID, AppParameters.ApplicationSecret, AppParameters.Scope, UserName.Text, Password2.Password,
                    sid, captchaSid, captchaKey));

                if (response.UserId.IsUser()) {
                    AppParameters.UserID = response.UserId;
                    AppParameters.AccessToken = response.AccessToken;
                } else if (!String.IsNullOrEmpty(response.Error)) {
                    await HandleError(response);
                } else {
                    ShowError(Locale.Get("global_error"));
                }
            } catch (Exception ex) {
                var info = Functions.GetNormalErrorInfo(ex);
                ShowError($"{info.Item1}\n{info.Item2}");
            }

            AuthBtn.IsEnabled = true;
        }

        private async Task HandleError(DirectAuthResponse err) {
            switch (err.Error) {
                case "invalid_client":
                    ShowError(!String.IsNullOrEmpty(err.ErrorDescription) ? err.ErrorDescription : $"{Locale.Get("global_error")}: {err.ErrorType}");
                    break;
                case "invalid_request":
                    ShowError(err.ErrorType == "wrong_otp" ? Locale.Get("wrong_otp_code") : $"{Locale.Get("global_error")}: {err.ErrorType}");
                    break;
                case "need_validation":
                    if (err.BanInfo != null) {
                        ShowError($"{err.BanInfo.MemberName}. {err.BanInfo.Message}");
                    } else {
                        if (!String.IsNullOrEmpty(err.ValidationType)) {
                            GoToFlow(DirectAuth2Flow.TwoFactor);
                            TwoFACode.Focus(FocusState.Keyboard);
                            if (err.ValidationType == "2fa_app") {
                                TwoFAInfo.Text = Locale.Get("2fa_app");
                            } else if (err.ValidationType == "2fa_sms") {
                                TwoFAInfo.Text = String.Format(Locale.GetForFormat("2fa_sms"), err.PhoneMask);
                            } else {
                                TwoFAInfo.Text = err.ErrorDescription;
                            }
                        } else {
                            ShowError($"Need validation: {err.ErrorDescription}");
                        }
                    }
                    if (err.ExtendFields != null && err.ExtendFields.Count > 0) {
                        string fields = String.Join(", ", err.ExtendFields);
                        ShowError(String.Format(Locale.GetForFormat("need_validation_on_offclient"), fields));
                    }
                    break;
                case "need_captcha":
                    GoToFlow(DirectAuth2Flow.Captcha);
                    BitmapImage img = new BitmapImage();
                    await img.SetUriSourceAsync(new Uri(err.CaptchaImg));
                    CaptchaImg.Source = img;
                    CaptchaImg.Tag = err.CaptchaSid;
                    CaptchaCode.Focus(FocusState.Keyboard);
                    break;
            }
        }
    }
}
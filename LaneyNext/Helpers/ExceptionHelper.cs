using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using System;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.Foundation.Collections;
using Windows.Web;

namespace Elorucov.Laney.Helpers
{
    public class ExceptionHelper
    {
        public static Tuple<string, string> GetDefaultErrorInfo(Exception ex)
        {
            Tuple<string, string> result = new Tuple<string, string>(String.Empty, String.Empty);
            if (ex is APIException apiEx)
            {
                string uem = APIHelper.GetUnderstandableErrorMessage(apiEx);
                result = new Tuple<string, string>(Locale.Get("err_api"), String.IsNullOrEmpty(uem) ? apiEx.Message : uem);
            }
            else if (ex is HttpNonSuccessException hnsex)
            {
                string nerr = Locale.Get("err_network_general");
                if ((int)hnsex.StatusCode >= 500) nerr = Locale.Get("err_network_shinaprovod");
                if ((int)hnsex.StatusCode == 504) nerr = Locale.Get("err_network_timeout");
                nerr += $"\n(code: {(int)hnsex.StatusCode})";
                result = new Tuple<string, string>(Locale.Get("err_network"), nerr);
            }
            else if (ex is System.Net.Http.HttpRequestException httpex)
            {
                WebErrorStatus werror = WebError.GetStatus(httpex.HResult);
                string terr = Locale.Get("err_network");
                string nerr = String.Empty;
                switch (werror)
                {
                    default: nerr = $"{Locale.Get("err_network_general")}\n({werror})"; break;
                    case WebErrorStatus.CertificateCommonNameIsIncorrect: nerr = Locale.Get("err_network_ssl_hostname"); break;
                    case WebErrorStatus.CertificateIsInvalid:
                    case WebErrorStatus.CertificateContainsErrors:
                        terr = Locale.Get("err_network_ssl");
                        nerr = Locale.Get("err_network_ssl_certerror");
                        break;
                    case WebErrorStatus.CertificateExpired:
                        terr = Locale.Get("err_network_ssl");
                        nerr = Locale.Get("err_network_ssl_expired");
                        break;
                    case WebErrorStatus.CertificateRevoked:
                        terr = Locale.Get("err_network_ssl");
                        nerr = Locale.Get("err_network_ssl_revoked");
                        break;
                    case WebErrorStatus.ConnectionAborted: nerr = Locale.Get("err_network_aborted"); break;
                    case WebErrorStatus.RequestTimeout: nerr = Locale.Get("err_network_timeout"); break;
                    case WebErrorStatus.HostNameNotResolved: nerr = Locale.Get("err_network_no_connection"); break;
                }
                result = new Tuple<string, string>(terr, nerr);
            }
            else
            {
                result = new Tuple<string, string>(Locale.Get("error"), $"{ex.Message.Trim()}\n(0x{ex.HResult.ToString("x8")})");
            }
            Log.General.Error(String.Empty, new ValueSet { { "short", result.Item1 }, { "advanced", result.Item2 } });
            return result;
        }

        public static async Task<bool> ShowErrorDialogAsync(Tuple<string, string> errorInfo, bool hideRetry = false)
        {
            Alert alert = new Alert
            {
                Header = errorInfo.Item1,
                Text = errorInfo.Item2,
                PrimaryButtonText = !hideRetry ? Locale.Get("retry") : null,
                SecondaryButtonText = Locale.Get("close"),
            };
            AlertButton result = await alert.ShowAsync();
            return result == AlertButton.Primary;
        }

        public static async Task<bool> ShowErrorDialogAsync(Exception ex, bool hideRetry = false)
        {
            if (ex is AggregateException agex) ex = agex.InnerException;
            Tuple<string, string> err = GetDefaultErrorInfo(ex);
            return await ShowErrorDialogAsync(err, hideRetry);
        }
    }
}

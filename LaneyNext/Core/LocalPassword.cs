using Elorucov.Laney.Helpers.Security;
using System;
using System.Text;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.Security.Credentials.UI;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Core
{
    public enum PasswordSetupDialogMode { Set, Change, Delete }
    public enum PasswordDialogResult { NotSet, Verified, Cancelled }

    public class LocalPassword
    {
        private static string GetCommonStringInStart(string a, string b)
        {
            if (String.IsNullOrEmpty(a) || String.IsNullOrEmpty(b)) return null;
            int min = Math.Min(a.Length, b.Length);
            string result = String.Empty;
            for (int i = 0; i < min; i++)
            {
                if (a[i] != b[i]) break;
                result += a[i];
            }
            return result;
        }

        private static string GetSettingKey()
        {
            string s = (string)ApplicationData.Current.LocalSettings.Values["iv"];
            if (String.IsNullOrEmpty(s)) return null;
            var t = ViewModels.Settings.PrivacyViewModel.Something[3];
            var f = ViewModels.Controls.MessageInputViewModel.Enhancements[2];
            var o = ViewModels.Settings.PrivacyViewModel.Something[1];
            var y = Views.Settings.Debug.Features[2];
            var u = ViewModels.Settings.PrivacyViewModel.Something[0];
            var z = ViewModels.Settings.PrivacyViewModel.Something[2];
            return s.EncryptToBase64(Encoding.UTF8.GetString(new byte[] { t, z, f, y, o, u }));
        }

        public static bool HavePass()
        {
            string key = GetSettingKey();
            if (String.IsNullOrEmpty(key)) return false;
            return ApplicationData.Current.LocalSettings.Values.ContainsKey(key);
        }

        private static bool Set(string newPass, string oldPass)
        {
            string key = GetSettingKey();
            string npass = newPass?.EncryptToBase64(key);
            string opass = oldPass?.EncryptToBase64(key);
            string nc = GetCommonStringInStart(key, npass);
            string oc = GetCommonStringInStart(key, opass);
            string napass = npass?.Replace(nc, String.Empty);
            string oapass = opass?.Replace(oc, String.Empty);

            if (String.IsNullOrEmpty(newPass))
            {
                if (String.IsNullOrEmpty(oldPass)) throw new ArgumentNullException("Required current password to remove this", nameof(oldPass));
                if (Verify(oldPass))
                {
                    ApplicationData.Current.LocalSettings.Values.Remove(key);
                    return true;
                }
                return false;
            }

            if (String.IsNullOrEmpty(oldPass))
            {
                if (!HavePass())
                {
                    ApplicationData.Current.LocalSettings.Values[key] = napass;
                    return true;
                }
                throw new ArgumentNullException(nameof(oldPass));
            }

            if (Verify(oldPass))
            {
                ApplicationData.Current.LocalSettings.Values[key] = napass;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool Verify(string pass)
        {
            if (!HavePass()) return true;
            try
            {
                string key = GetSettingKey();
                string val = (string)ApplicationData.Current.LocalSettings.Values[key];

                string encPass = pass.EncryptToBase64(key);
                string u = GetCommonStringInStart(key, encPass) + val;

                return u == encPass;
            }
            catch
            {
                return false;
            }
        }

        #region UI

        public static async Task<bool> ShowPasswordChangeDialogAsync(PasswordSetupDialogMode mode)
        {
            return await ShowPassChangeDialogInternal(mode);
        }

        private static async Task<bool> ShowPassChangeDialogInternal(PasswordSetupDialogMode mode, string errText = null)
        {
            bool havePass = HavePass();

            if (!havePass && mode == PasswordSetupDialogMode.Delete) throw new ArgumentException("Password is not set", nameof(mode));
            string header = Locale.Get(HavePass() ? "pass_modal_change" : "pass_modal_creation");
            string text = String.Empty;
            if (mode == PasswordSetupDialogMode.Delete) header = Locale.Get("pass_modal_delete");

            StackPanel sp = new StackPanel();
            PasswordBox opb = new PasswordBox()
            {
                Style = (Style)Application.Current.Resources["VKPasswordBox"],
                PlaceholderText = Locale.Get("pass_old"),
                Margin = new Thickness(0, 8, 0, 0)
            };
            PasswordBox npb = new PasswordBox()
            {
                Style = (Style)Application.Current.Resources["VKPasswordBox"],
                PlaceholderText = Locale.Get("pass_new"),
                Margin = new Thickness(0, 8, 0, 0)
            };
            PasswordBox rpb = new PasswordBox()
            {
                Style = (Style)Application.Current.Resources["VKPasswordBox"],
                PlaceholderText = Locale.Get("pass_again"),
                Margin = new Thickness(0, 8, 0, 0)
            };

            if (!String.IsNullOrEmpty(errText)) sp.Children.Add(new TextBlock
            {
                Style = (Style)Application.Current.Resources["VKErrorTextStyle"],
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                Text = errText,
                Margin = new Thickness(0, 5, 0, 0)
            });
            if (mode == PasswordSetupDialogMode.Set) text = Locale.Get("pass_modal_info");
            if (mode != PasswordSetupDialogMode.Set) sp.Children.Add(opb);
            if (mode != PasswordSetupDialogMode.Delete) sp.Children.Add(npb);
            if (mode != PasswordSetupDialogMode.Delete) sp.Children.Add(rpb);

            Alert alert = new Alert
            {
                Header = header,
                Text = text,
                Content = sp,
                PrimaryButtonText = Locale.Get(mode == PasswordSetupDialogMode.Delete ? "delete" : "set"),
                SecondaryButtonText = Locale.Get("cancel")
            };

            var result = await alert.ShowAsync();

            if (mode == PasswordSetupDialogMode.Set)
            {
                npb.Focus(FocusState.Keyboard);
            }
            else
            {
                opb.Focus(FocusState.Keyboard);
            }

            if (result == AlertButton.Primary)
            {
                switch (mode)
                {
                    case PasswordSetupDialogMode.Set:
                        if (npb.Password.Length < 4)
                            return await ShowPassChangeDialogInternal(mode, Locale.Get("pass_modal_short")); // Too short
                        if (npb.Password == rpb.Password)
                        {
                            Set(npb.Password, null);
                            return true;
                        }
                        else
                        {
                            return await ShowPassChangeDialogInternal(mode, Locale.Get("pass_modal_mismatch")); // Wrong confirmation password
                        }
                    case PasswordSetupDialogMode.Change:
                        if (npb.Password.Length < 4)
                            return await ShowPassChangeDialogInternal(mode, Locale.Get("pass_modal_short")); // Too short
                        if (npb.Password == rpb.Password)
                        {
                            if (Set(npb.Password, opb.Password)) return true;
                            return await ShowPassChangeDialogInternal(mode, Locale.Get("pass_modal_wrong")); // Wrong password
                        }
                        return await ShowPassChangeDialogInternal(mode, Locale.Get("pass_modal_mismatch")); // Wrong confirmation password
                    case PasswordSetupDialogMode.Delete:
                        if (Set(null, opb.Password)) return true;
                        return await ShowPassChangeDialogInternal(mode, Locale.Get("pass_modal_wrong")); // Wrong password
                }
            }

            return false;
        }

        public static async Task<PasswordDialogResult> ShowPasswordDialogAsync()
        {
            if (!HavePass()) return PasswordDialogResult.NotSet;
            if (Settings.UseWindowsHello)
            {
                var r = await UserConsentVerifier.RequestVerificationAsync(String.Empty);
                if (r == UserConsentVerificationResult.Verified) return PasswordDialogResult.Verified;
            }
            return await ShowPasswordDialogInternal();
        }

        private static async Task<PasswordDialogResult> ShowPasswordDialogInternal(string errText = null)
        {
            StackPanel sp = new StackPanel();
            PasswordBox opb = new PasswordBox()
            {
                Style = (Style)Application.Current.Resources["VKPasswordBox"],
                Margin = new Thickness(0, 8, 0, 0)
            };

            if (!String.IsNullOrEmpty(errText)) sp.Children.Add(new TextBlock
            {
                Style = (Style)Application.Current.Resources["VKErrorTextStyle"],
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                Text = errText
            });
            sp.Children.Add(opb);

            Alert alert = new Alert
            {
                Header = Locale.Get("enter_password"),
                Content = sp,
                PrimaryButtonText = Locale.Get("ok"),
                SecondaryButtonText = Locale.Get("cancel")
            };
            var result = await alert.ShowAsync();
            opb.Focus(FocusState.Keyboard);

            if (result == AlertButton.Primary)
            {
                if (!Verify(opb.Password)) return await ShowPasswordDialogInternal(Locale.Get("pass_modal_wrong"));
                return PasswordDialogResult.Verified;
            }

            return PasswordDialogResult.Cancelled;
        }

        #endregion
    }
}

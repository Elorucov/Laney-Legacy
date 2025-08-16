using ELOR.VKAPILib;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.Helpers.Groupings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VK.VKUI;
using VK.VKUI.Controls;
using Windows.Security.Credentials.UI;

namespace Elorucov.Laney.ViewModels.Settings
{
    public class PrivacyViewModel : CommonViewModel
    {
        internal static readonly byte[] Something = new byte[] { 0x2a, 0x5a, 0x5f, 0x6e };

        private bool _haveLocalPassword;
        private bool _windowsHelloEnabled;
        private bool _windowsHelloReady;
        private ObservableCollection<Grouping<string, PrivacySetting>> _groupedSettings = new ObservableCollection<Grouping<string, PrivacySetting>>();

        private RelayCommand _setOrChangePasswordCommand;
        private RelayCommand _deletePasswordCommand;
        private RelayCommand _toggleWinHelloCommand;

        public bool HaveLocalPassword { get { return _haveLocalPassword; } private set { _haveLocalPassword = value; OnPropertyChanged(); } }
        public bool WindowsHelloEnabled { get { return _windowsHelloEnabled; } private set { _windowsHelloEnabled = value; OnPropertyChanged(); } }
        public bool WindowsHelloReady { get { return _windowsHelloReady; } private set { _windowsHelloReady = value; OnPropertyChanged(); } }
        public ObservableCollection<Grouping<string, PrivacySetting>> GroupedSettings { get { return _groupedSettings; } private set { _groupedSettings = value; OnPropertyChanged(); } }

        public RelayCommand SetOrChangePasswordCommand { get { return _setOrChangePasswordCommand; } private set { _setOrChangePasswordCommand = value; OnPropertyChanged(); } }
        public RelayCommand DeletePasswordCommand { get { return _deletePasswordCommand; } private set { _deletePasswordCommand = value; OnPropertyChanged(); } }
        public RelayCommand ToggleWinHelloCommand { get { return _toggleWinHelloCommand; } private set { _toggleWinHelloCommand = value; OnPropertyChanged(); } }


        private VKAPI API = ViewManagement.GetVKAPIInstanceForCurrentView();
        public static List<PrivacyCategory> PrivacyCategories { get; set; }

        public PrivacyViewModel()
        {
            CheckPasswordStatus();
            GetSettings();
        }

        #region Local password

        private async void CheckPasswordStatus()
        {
            HaveLocalPassword = LocalPassword.HavePass();
            WindowsHelloEnabled = Core.Settings.UseWindowsHello;

            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            WindowsHelloReady = availability == UserConsentVerifierAvailability.Available;

            SetOrChangePasswordCommand = new RelayCommand(SetOrChangePassword);
            DeletePasswordCommand = new RelayCommand(DeletePassword);
            ToggleWinHelloCommand = new RelayCommand(ToggleWinHello);
        }

        private async void SetOrChangePassword(object obj)
        {
            bool success = await LocalPassword.ShowPasswordChangeDialogAsync(HaveLocalPassword ? PasswordSetupDialogMode.Change : PasswordSetupDialogMode.Set);
            if (success) CheckPasswordStatus();
        }

        private async void DeletePassword(object obj)
        {
            bool success = await LocalPassword.ShowPasswordChangeDialogAsync(PasswordSetupDialogMode.Delete);
            if (success) CheckPasswordStatus();
        }

        private async void ToggleWinHello(object obj)
        {
            WindowsHelloReady = false;
            var result = await UserConsentVerifier.RequestVerificationAsync(String.Empty);

            if (result == UserConsentVerificationResult.Verified)
            {
                Core.Settings.UseWindowsHello = !Core.Settings.UseWindowsHello;
            }
            else
            {
                WindowsHelloEnabled = Core.Settings.UseWindowsHello;
            }
            WindowsHelloReady = result == UserConsentVerificationResult.Verified
                || result == UserConsentVerificationResult.Canceled;
        }

        #endregion

        #region Profile privacy

        private async void GetSettings()
        {
            IsLoading = true;
            try
            {
                var response = await API.Account.GetPrivacySettingsAsync();
                PrivacyCategories = response.SupportedCategories;
                GroupSettings(response);
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => GetSettings());
            }
            IsLoading = false;
        }

        private void GroupSettings(PrivacyResponse response)
        {
            List<string> importantSettingsKeys = new List<string> { "mail_send", "chat_invite_user", "company_messages", "closed_profile" };

            foreach (var section in response.Sections)
            {
                List<PrivacySetting> settings = Core.Settings.ShowAllPrivacySettings ?
                    (from s in response.Settings where s.Section == section.Name select s).ToList() :
                    (from s in response.Settings where s.Section == section.Name && importantSettingsKeys.Contains(s.Key) select s).ToList();
                if (settings.Count() == 0) continue;

                // Fix "some" value
                foreach (var setting in settings)
                {
                    var v = setting.Value;
                    if (setting.Type == PrivacySettingValueType.Binary) continue;
                    if (String.IsNullOrEmpty(v.Category) && v.Owners != null) setting.Value.Category = "some";
                }

                Grouping<string, PrivacySetting> group =
                    new Grouping<string, PrivacySetting>(section.Title, settings, VKUILibrary.GetIconTemplate(GetIconBySection(section.Name)));

                GroupedSettings.Add(group);
            }
        }

        private VKIconName GetIconBySection(string name)
        {
            switch (name)
            {
                case "profile": return VKIconName.Icon28UserCircleFillBlue;
                case "posts": return VKIconName.Icon28EditCircleFillBlue;
                case "photos": return VKIconName.Icon28CameraCircleFillGreen;
                case "contacts": return VKIconName.Icon28MessageCircleFillGreen;
                case "stories": return VKIconName.Icon28StoryFillCircleRed;
                default: return VKIconName.Icon28ListCircleFillGray;
            }
        }

        public async void SetPrivacySetting(string key, string value)
        {
            try
            {
                PrivacySettingValue newValue = await API.Account.SetPrivacyAsync(key, value);
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) SetPrivacySetting(key, value);
            }
        }

        #endregion
    }
}
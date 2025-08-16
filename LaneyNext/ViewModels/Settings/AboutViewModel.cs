using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using Windows.System;

namespace Elorucov.Laney.ViewModels.Settings
{
    public class AboutViewModel : BaseViewModel
    {
        private string _version;
        private DateTime _buildDate;
        private DateTime _installDate;
        private string _additionalInfo;
        private RelayCommand _openLinkCommand;
        private RelayCommand _openConversationCommand;

        public string Version { get { return _version; } private set { _version = value; OnPropertyChanged(); } }
        public DateTime BuildDate { get { return _buildDate; } private set { _buildDate = value; OnPropertyChanged(); } }
        public DateTime InstallDate { get { return _installDate; } private set { _installDate = value; OnPropertyChanged(); } }
        public string AdditionalInfo { get { return _additionalInfo; } private set { _additionalInfo = value; OnPropertyChanged(); } }
        public RelayCommand OpenLinkCommand { get { return _openLinkCommand; } private set { _openLinkCommand = value; OnPropertyChanged(); } }
        public RelayCommand OpenConversationCommand { get { return _openConversationCommand; } private set { _openConversationCommand = value; OnPropertyChanged(); } }

        public AboutViewModel()
        {
            var v = AppInfo.Version;
            Version = $"{v.Major}.{v.Minor} ({v.Build})";
            BuildDate = AppInfo.BuildDateTime.ToLocalTime();
            InstallDate = Windows.ApplicationModel.Package.Current.InstalledDate.DateTime;

            if (AppInfo.ReleaseState == AppReleaseState.Internal)
            {
                AdditionalInfo = $"This is an internal build for trusted testers.\nExpired on {AppInfo.ExpirationDate.ToString(@"yyy\.MM\.dd")}.";
            }

            OpenLinkCommand = new RelayCommand(async l =>
            {
                if (l is string s && Uri.IsWellFormedUriString(s, UriKind.Absolute)) await Launcher.LaunchUriAsync(new Uri(s));
            });
            OpenConversationCommand = new RelayCommand(i => { if (i is int id) return; }); // TODO: Get to conversation with community.
        }
    }
}
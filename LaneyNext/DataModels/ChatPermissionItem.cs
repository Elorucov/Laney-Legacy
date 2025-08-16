using Elorucov.Laney.Core;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.DataModels
{
    public class ChatPermissionItem : INotifyPropertyChanged
    {
        private string _title;
        private string _subtitle;
        private int _settingIndex;
        private Uri _iconPath;
        private SolidColorBrush _iconBackground;

        public string Title { get { return _title; } set { _title = value; OnPropertyChanged(); } }
        public string Subtitle { get { return _subtitle; } private set { _subtitle = value; OnPropertyChanged(); } }
        public int SettingIndex { get { return _settingIndex; } set { _settingIndex = value; OnPropertyChanged(); } }
        public Uri IconPath { get { return _iconPath; } set { _iconPath = value; OnPropertyChanged(); } }
        public SolidColorBrush IconBackground { get { return _iconBackground; } set { _iconBackground = value; OnPropertyChanged(); } }

        public string SettingName { get; private set; }
        public ChatPermissionItem(string settingName)
        {
            SettingName = settingName;
            Title = Locale.Get($"chatperm_{settingName}");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            if (prop == nameof(SettingIndex))
            {
                Subtitle = GetChangersByIndex(SettingIndex);
            }
        }

        public static string GetChangersByIndex(int settingIndex)
        {
            switch (settingIndex)
            {
                case 0: return Locale.Get("chatperm_opt_all");
                case 1: return Locale.Get("chatperm_opt_owner_admins");
                case 2: return Locale.Get("chatperm_opt_owner");
                default: return String.Empty;
            }
        }
    }
}

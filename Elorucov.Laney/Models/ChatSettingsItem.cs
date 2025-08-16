using Elorucov.Laney.Services.Common;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Models {
    class ChatSettingsItem : INotifyPropertyChanged {
        private string _title;
        private string _subtitle;
        private int _settingIndex;
        private DataTemplate _icon;

        public string Title { get { return _title; } set { _title = value; OnPropertyChanged(); } }
        public string Subtitle { get { return _subtitle; } private set { _subtitle = value; OnPropertyChanged(); } }
        public int SettingIndex { get { return _settingIndex; } set { _settingIndex = value; OnPropertyChanged(); } }
        public DataTemplate Icon { get { return _icon; } set { _icon = value; OnPropertyChanged(); } }

        public string SettingName { get; private set; }
        public ChatSettingsItem(string settingName) {
            SettingName = settingName;
            Title = Locale.Get($"chatmgmt_stg_{settingName}");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            if (prop == nameof(SettingIndex)) {
                Subtitle = GetChangersByIndex(SettingIndex);
            }
        }

        public static string GetChangersByIndex(int settingIndex) {
            switch (settingIndex) {
                case 0: return Locale.Get("chatmgmt_opt_all");
                case 1: return Locale.Get("chatmgmt_opt_owner_admins");
                case 2: return Locale.Get("chatmgmt_opt_owner");
                default: return string.Empty;
            }
        }
    }
}
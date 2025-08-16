using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using System;
using System.Linq;
using Windows.Globalization;

namespace Elorucov.Laney.ViewModels.Settings
{
    public class GeneralViewModel : BaseViewModel
    {
        private ThreadSafeObservableCollection<AppLanguage> _languages = new ThreadSafeObservableCollection<AppLanguage>();
        private AppLanguage _currentLanguage;
        private bool _sendViaCtrlEnter;
        private bool _dontParseLinks;
        private bool _disableMentions;
        private bool _suggestStickers;
        private bool _animatedStickers;

        public ThreadSafeObservableCollection<AppLanguage> Languages { get { return _languages; } private set { _languages = value; OnPropertyChanged(); } }
        public AppLanguage CurrentLanguage { get { return _currentLanguage; } set { _currentLanguage = value; OnPropertyChanged(); } }
        public bool SendViaCtrlEnter { get { return _sendViaCtrlEnter; } set { _sendViaCtrlEnter = value; OnPropertyChanged(); } }
        public bool DontParseLinks { get { return _dontParseLinks; } set { _dontParseLinks = value; OnPropertyChanged(); } }
        public bool DisableMentions { get { return _disableMentions; } set { _disableMentions = value; OnPropertyChanged(); } }
        public bool SuggestStickers { get { return _suggestStickers; } set { _suggestStickers = value; OnPropertyChanged(); } }
        public bool AnimatedStickers { get { return _animatedStickers; } set { _animatedStickers = value; OnPropertyChanged(); } }

        public GeneralViewModel()
        {
            // Language
            Languages = new ThreadSafeObservableCollection<AppLanguage>(AppLanguage.SupportedLanguages);
            Languages.Insert(0, new AppLanguage { Code = String.Empty, DisplayName = "System" });

            string overridelang = ApplicationLanguages.PrimaryLanguageOverride;
            if (String.IsNullOrEmpty(overridelang))
            {
                CurrentLanguage = Languages.First();
            }
            else
            {
                AppLanguage lng = Languages.FirstOrDefault(l => l.Code == overridelang);
                CurrentLanguage = lng;
            }

            SendViaCtrlEnter = Core.Settings.SendMessageViaCtrlEnter;
            DontParseLinks = Core.Settings.DontParseLinks;
            DisableMentions = Core.Settings.DisableMentions;
            SuggestStickers = Core.Settings.SuggestStickers;
            AnimatedStickers = Core.Settings.AnimatedStickers;

            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(CurrentLanguage): ApplicationLanguages.PrimaryLanguageOverride = CurrentLanguage.Code; break;
                    case nameof(SendViaCtrlEnter): Core.Settings.SendMessageViaCtrlEnter = SendViaCtrlEnter; break;
                    case nameof(DontParseLinks): Core.Settings.DontParseLinks = DontParseLinks; break;
                    case nameof(DisableMentions): Core.Settings.DisableMentions = DisableMentions; break;
                    case nameof(SuggestStickers): Core.Settings.SuggestStickers = SuggestStickers; LoadStickersSuggestions(); break;
                    case nameof(AnimatedStickers): Core.Settings.AnimatedStickers = AnimatedStickers; break;
                }
            };
        }

        private async void LoadStickersSuggestions()
        {
            if (!SuggestStickers) return;
            try
            {
                var execute = ViewManagement.GetVKAPIExecuteInstanceForCurrentView();
                var suggestions = await execute.GetStickersKeywordsAsync();
                StickersKeywords.InitDictionary(suggestions);
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                {
                    LoadStickersSuggestions();
                }
            }
        }
    }
}

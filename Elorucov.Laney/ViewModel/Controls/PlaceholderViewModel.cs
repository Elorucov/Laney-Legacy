using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using System;

namespace Elorucov.Laney.ViewModel.Controls {
    public class PlaceholderViewModel : BaseViewModel {
        private string _icon;
        private string _header;
        private string _content;
        private string _actionButton;
        private RelayCommand _actionButtonCommand;
        private object _data;

        public string Icon { get { return _icon; } private set { _icon = value; OnPropertyChanged(); } }
        public string Header { get { return _header; } private set { _header = value; OnPropertyChanged(); } }
        public string Content { get { return _content; } private set { _content = value; OnPropertyChanged(); } }
        public string ActionButton { get { return _actionButton; } private set { _actionButton = value; OnPropertyChanged(); } }
        public RelayCommand ActionButtonCommand { get { return _actionButtonCommand; } private set { _actionButtonCommand = value; OnPropertyChanged(); } }
        public object Data { get { return _data; } private set { _data = value; OnPropertyChanged(); } }

        public PlaceholderViewModel(char? icon = null, string header = null, string content = null, string actionButton = null, Action action = null) {
            if (icon != null) Icon = icon?.ToString();
            Header = header;
            Content = content;
            ActionButton = actionButton;
            ActionButtonCommand = action != null ? new RelayCommand(o => { action.Invoke(); }) : null;
        }

        public static PlaceholderViewModel GetForHandledError(object ex, Action action = null) {
            var err = Functions.GetNormalErrorInfo(ex);
            return new PlaceholderViewModel() {
                Data = ex,
                Icon = "",
                Header = err.Item1,
                Content = err.Item2,
                ActionButton = Locale.Get("retry"),
                ActionButtonCommand = action != null ? new RelayCommand(o => { action.Invoke(); }) : null,
            };
        }
    }
}
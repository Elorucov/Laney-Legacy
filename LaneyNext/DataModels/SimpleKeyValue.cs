using Elorucov.Laney.ViewModels;

namespace Elorucov.Laney.DataModels
{
    public class SimpleKeyValue : BaseViewModel
    {
        private string _key;
        private string _value;
        private string _additionalInfo;
        private bool _isEnabled;

        public string Key { get { return _key; } set { _key = value; OnPropertyChanged(); } }
        public string Value { get { return _value; } set { _value = value; OnPropertyChanged(); } }
        public string AdditionalInfo { get { return _additionalInfo; } set { _additionalInfo = value; OnPropertyChanged(); } }
        public bool IsEnabled { get { return _isEnabled; } set { _isEnabled = value; OnPropertyChanged(); } }
    }
}

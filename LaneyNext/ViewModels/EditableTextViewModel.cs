namespace Elorucov.Laney.ViewModels
{
    public class EditableTextViewModel : BaseViewModel
    {
        private string _text;
        public string Text { get { return _text; } set { _text = value; OnPropertyChanged(); } }
    }
}

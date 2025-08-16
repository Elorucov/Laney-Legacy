using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;

namespace Elorucov.Laney.ViewModels.Settings.Interface
{
    public class BackgroundPickerViewModel : BaseViewModel
    {
        private ThreadSafeObservableCollection<BackgroundPickerItem> _items;

        public ThreadSafeObservableCollection<BackgroundPickerItem> Items { get { return _items; } set { _items = value; OnPropertyChanged(); } }

        public BackgroundPickerViewModel()
        {
            Items = new ThreadSafeObservableCollection<BackgroundPickerItem>(BackgroundPickerItem.GetItems());
        }
    }
}

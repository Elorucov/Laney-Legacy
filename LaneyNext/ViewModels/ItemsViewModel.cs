using Elorucov.Laney.Helpers;

namespace Elorucov.Laney.ViewModels
{
    public class ItemsViewModel<T> : CommonViewModel
    {
        private ThreadSafeObservableCollection<T> _items = new ThreadSafeObservableCollection<T>();

        public ThreadSafeObservableCollection<T> Items { get { return _items; } set { _items = value; OnPropertyChanged(); } }
    }
}

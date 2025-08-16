using Elorucov.Laney.ViewModel.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Elorucov.Laney.ViewModel {
    public class ItemsViewModel<T> : BaseViewModel {
        private bool _isLoading;
        private PlaceholderViewModel _placeholder;
        private ObservableCollection<T> _items;

        public bool IsLoading { get { return _isLoading; } set { _isLoading = value; OnPropertyChanged(); } }
        public PlaceholderViewModel Placeholder { get { return _placeholder; } set { _placeholder = value; OnPropertyChanged(); } }
        public ObservableCollection<T> Items => _items;

        public ItemsViewModel() {
            _items = new ObservableCollection<T>();
        }

        // Allowing the class owner to modify items in collection that sent to "items" param.
        public ItemsViewModel(ObservableCollection<T> items) {
            _items = items;
        }

        public ItemsViewModel(IEnumerable<T> items) {
            _items = new ObservableCollection<T>(items);
        }
    }
}

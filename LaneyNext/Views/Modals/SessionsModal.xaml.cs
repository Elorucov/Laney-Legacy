using ELOR.VKAPILib.Objects;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using System.Linq;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class SessionsModal : Modal
    {
        private SessionsModalViewModel ViewModel { get { return DataContext as SessionsModalViewModel; } }

        public SessionsModal()
        {
            this.InitializeComponent();
            DataContext = new SessionsModalViewModel();
            ViewModel.SelectedGroups.CollectionChanged += SelectedGroups_CollectionChanged;
            GroupsList.SelectionChanged += GroupsList_SelectionChanged;
        }

        private void SelectedGroups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            GroupsList.SelectionChanged -= GroupsList_SelectionChanged;

            var list = GroupsList.Items.Cast<Group>();
            foreach (var g in list)
            {
                bool contains = ViewModel.SelectedGroups.Contains(g);
                if (!contains)
                {
                    GroupsList.SelectedItems.Remove(g);
                }
                else
                {
                    GroupsList.SelectedItems.Add(g);
                }
            }

            GroupsList.SelectionChanged += GroupsList_SelectionChanged;
        }

        private void GroupsList_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            var removed = e.RemovedItems.Cast<Group>();
            var added = e.AddedItems.Cast<Group>();
            foreach (var gr in removed)
            {
                if (ViewModel.SelectedGroups.Contains(gr)) ViewModel.SelectedGroups.Remove(gr);
            }
            foreach (var ga in added)
            {
                if (!ViewModel.SelectedGroups.Contains(ga)) ViewModel.SelectedGroups.Add(ga);
            }
        }
    }
}
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class SessionsModalViewModel : CommonViewModel
    {
        private ObservableCollection<Group> _groups = new ObservableCollection<Group>();
        private ObservableCollection<Group> _selectedGroups = new ObservableCollection<Group>();

        private RelayCommand _saveCommand;

        public ObservableCollection<Group> Groups { get { return _groups; } private set { _groups = value; OnPropertyChanged(); } }
        public ObservableCollection<Group> SelectedGroups { get { return _selectedGroups; } set { _selectedGroups = value; OnPropertyChanged(); } }

        public RelayCommand SaveCommand { get { return _saveCommand; } private set { _saveCommand = value; OnPropertyChanged(); } }

        public SessionsModalViewModel()
        {
            SaveCommand = new RelayCommand(e =>
            {
                if (IsLoading) return;
                SaveSessions();
                (e as Modal).Hide();

            });
            GetGroups();
        }

        private async void GetGroups()
        {
            IsLoading = true;
            Placeholder = null;
            SelectedGroups.Clear();
            Groups.Clear();
            try
            {
                int currentUserId = VKSession.CurrentUser.Id;

                List<VKSession> sessions = await VKSession.GetSessionsAsync();
                List<VKSession> groupSessions = (from s in sessions where s.Type == SessionType.VKGroup && s.Id == currentUserId select s).ToList();
                List<int> groupSessionIds = (from g in groupSessions select g.GroupId).ToList();

                VKList<Group> groups = await VKSession.CurrentUser.API.Groups.GetAsync(currentUserId,
                    new List<string> { "can_message" },
                    new List<string> { "admin", "editor" });

                IsLoading = false;
                Groups = new ObservableCollection<Group>(groups.Items.Where(g => g.CanMessage));

                await Task.Delay(50);
                groups.Items.ForEach(g => { if (groupSessionIds.Contains(g.Id)) SelectedGroups.Add(g); });
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Placeholder = PlaceholderViewModel.GetForException(ex, () => GetGroups());
            }
        }

        private async void SaveSessions()
        {
            var result = await AuthorizationHelper.AuthVKGroup(VKSession.CurrentUser, SelectedGroups.ToList());
        }
    }
}

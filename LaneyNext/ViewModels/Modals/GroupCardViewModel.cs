using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Linq;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class GroupCardViewModel : CommonViewModel
    {
        private Group _group;
        private string _type;
        private RelayCommand _messageCommand;

        public Group Group { get { return _group; } set { _group = value; OnPropertyChanged(); } }
        public string Type { get { return _type; } set { _type = value; OnPropertyChanged(); } }
        public RelayCommand MessageCommand { get { return _messageCommand; } set { _messageCommand = value; OnPropertyChanged(); } }

        public GroupCardViewModel(int groupId)
        {
            GetGroupCard(groupId);

            MessageCommand = new RelayCommand(e =>
            {
                (e as Modal).Hide();
                VKSession.Current.SessionBase.SwitchToConversation(-Group.Id);
            });
        }

        private async void GetGroupCard(int groupId)
        {
            try
            {
                Placeholder = null;
                IsLoading = true;
                var response = await VKSession.Current.API.Groups.GetByIdAsync(groupId, APIHelper.GroupFields);
                Group group = response.Groups.FirstOrDefault();
                CacheManager.Add(group);
                Group = group;

                switch (Group.Type)
                {
                    case GroupType.Group: Type = Locale.Get("community"); break;
                    case GroupType.Page: Type = Locale.Get("public"); break;
                    case GroupType.Event: Type = Locale.Get("event"); break;
                }
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => GetGroupCard(groupId));
            }
            IsLoading = false;
        }
    }
}
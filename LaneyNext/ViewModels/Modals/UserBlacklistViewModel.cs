using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using VK.VKUI;
using VK.VKUI.Controls;
using Windows.Foundation.Collections;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class UserBlacklistViewModel : CommonViewModel
    {
        private ObservableCollection<Entity> _bannedUsers = new ObservableCollection<Entity>();

        public ObservableCollection<Entity> BannedUsers { get { return _bannedUsers; } set { _bannedUsers = value; OnPropertyChanged(); } }

        public event EventHandler<Snackbar> ShowSnackbarRequested;

        public UserBlacklistViewModel()
        {
            PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == nameof(IsLoading) && !IsLoading && BannedUsers.Count == 0 && Placeholder == null)
                {
                    Placeholder = PlaceholderViewModel.GetForContent(Locale.Get("user_blacklist_empty"));
                }
            };
            GetBannedUsers();
        }

        private async void GetBannedUsers()
        {
            IsLoading = true;
            Placeholder = null;

            try
            {
                var response = await VKSession.Current.API.Account.GetBannedAsync(APIHelper.Fields);
                foreach (int id in response.Items)
                {
                    User u = response.Profiles.Where(bu => bu.Id == id).FirstOrDefault();
                    if (u == null)
                    {
                        Log.General.Error("User info not found in Profiles list!", new ValueSet() { { "id", id } });
                    }
                    else
                    {
                        Entity e = new Entity()
                        {
                            Id = u.Id,
                            Title = u.FullName,
                            Image = u.Photo,
                            ExtraButtonIcon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon24Cancel),
                            ExtraButtonCommand = new RelayCommand((o) => Unban(u))
                        };
                        BannedUsers.Add(e);
                    }
                }
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => GetBannedUsers());
            }

            IsLoading = false;
        }

        private async void Unban(User u)
        {
            try
            {
                bool response = await VKSession.Current.API.Account.UnbanAsync(u.Id);
                Entity e = BannedUsers.Where(bu => bu.Id == u.Id).FirstOrDefault();
                if (e != null)
                {
                    BannedUsers.Remove(e);
                    if (BannedUsers.Count == 0) Placeholder = PlaceholderViewModel.GetForContent(Locale.Get("user_blacklist_empty"));
                }
                else
                {
                    BannedUsers.Clear();
                    GetBannedUsers();
                }

                Snackbar sb = new Snackbar
                {
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
                    Content = String.Format(Locale.GetForFormat($"user_blacklist_removed_{u.Sex}"), u.FirstName),
                    BeforeIcon = VKIconName.Icon16Done
                };
                ShowSnackbarRequested?.Invoke(this, sb);
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) Unban(u);
            }
        }
    }
}
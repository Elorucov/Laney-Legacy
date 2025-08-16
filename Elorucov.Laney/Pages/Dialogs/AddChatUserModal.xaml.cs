using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Диалоговое окно содержимого" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class AddChatUserModal : Modal {
        long _chatId = 0;
        List<long> _membersId = new List<long>();

        public AddChatUserModal(long chatId, List<long> membersId) {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            _chatId = chatId;
            _membersId = membersId;
        }

        private void LoadFriends(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                Log.Info($"{GetType().Name} > Getting friends...");
                var res = await Friends.Get(AppParameters.UserID, true);

                Log.Info($"{GetType().Name} > Response type: {res.GetType()}");
                if (res is VKList<User>) {
                    VKList<User> resr = res as VKList<User>;
                    Log.Info($"{GetType().Name} > Adding users to cache...");
                    Services.AppSession.AddUsersToCache(resr.Items);
                    Log.Info($"{GetType().Name} > Friends loaded.");

                    ObservableCollection<User> Friends = new ObservableCollection<User>();
                    foreach (var u in resr.Items) {
                        if (!_membersId.Contains(u.Id) && u.Deactivated == DeactivationState.No) Friends.Add(u);
                    }
                    FriendsList.ItemsSource = Friends;
                } else {
                    Functions.ShowHandledErrorDialog(res);
                }
            })();
        }

        private void FriendsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            int max = 20;
            NextBtn.IsEnabled = FriendsList.SelectedItems.Count > 0;
            if (FriendsList.SelectedItems.Count > max) {
                FriendsList.SelectedItems.Remove(FriendsList.SelectedItems.Last());
                Tips.Show(String.Format(Locale.GetForFormat("addchatuser_max"), max));
            }
        }

        private void AddChatBtn(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                var s = FriendsList.SelectedItems.ToList();

                int vmc = 0;
                bool isInt = int.TryParse(VisibleMessagesCount.Text, out vmc);
                if (!isInt) {
                    ShowError(Locale.Get("addchatuser_vmc_err"));
                    return;
                }

                if (vmc < 0 || vmc > 1000) {
                    ShowError(Locale.Get("addchatuser_vmc_err"));
                    return;
                }

                NextBtn.IsEnabled = false;

                List<long> selected = (from u in s select ((User)u).Id).ToList();
                object resp = await Execute.AddChatUser(_chatId, selected, vmc);
                if (resp is AddChatUserResponse) {
                    Hide(true);
                } else {
                    Functions.ShowHandledErrorTip(resp);
                    NextBtn.IsEnabled = true;
                }
            })();
        }

        private void ShowError(string msg = "") {
            ErrText.Visibility = string.IsNullOrEmpty(msg) ? Visibility.Collapsed : Visibility.Visible;
            ErrText.Text = msg;
        }
    }
}
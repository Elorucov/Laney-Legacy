using ELOR.VKAPILib.Objects;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class InviteFriendToChat : Modal
    {
        private InviteFriendToChatViewModel ViewModel { get { return DataContext as InviteFriendToChatViewModel; } }

        public InviteFriendToChat(int chatId, List<int> ignoredIds)
        {
            this.InitializeComponent();
            DataContext = new InviteFriendToChatViewModel(this, chatId, ignoredIds);
        }

        private void FriendsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int max = 20;
            var removed = e.RemovedItems.Cast<User>();
            var added = e.AddedItems.Cast<User>();
            foreach (var ur in removed)
            {
                if (ViewModel.SelectedFriends.Contains(ur)) ViewModel.SelectedFriends.Remove(ur);
            }
            foreach (var ua in added)
            {
                if (!ViewModel.SelectedFriends.Contains(ua)) ViewModel.SelectedFriends.Add(ua);
                if (ViewModel.SelectedFriends.Count > max)
                {
                    ViewModel.SelectedFriends.Remove(ua);
                    FriendsList.SelectedItems.Remove(ua);
                }
            }
        }

        private void VisibleMessagesCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            string t = VisibleMessagesCount.Text;
            ushort vmc = 0;
            if (UInt16.TryParse(t, out vmc) && vmc >= 0 && vmc <= 1000)
            {
                ViewModel.VisibleMessagesCount = vmc;
            }
            else
            {
                if (String.IsNullOrEmpty(t))
                {
                    ViewModel.VisibleMessagesCount = 0;
                    return;
                }
                int s = VisibleMessagesCount.SelectionStart;
                VisibleMessagesCount.Text = ViewModel.VisibleMessagesCount.ToString();
                VisibleMessagesCount.SelectionStart = s - 1;
            }
        }
    }
}
using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.LongPoll;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Elorucov.Laney.ViewModel {

    public class ImViewModel : BaseViewModel {
        private string _currentUserName;
        private Uri _currentUserAvatar;
        private bool _isFoldersLoading;
        private ObservableCollection<ConversationsFolder> _folders = new ObservableCollection<ConversationsFolder>();
        private ConversationsFolder _currentFolder;
        private PlaceholderViewModel _placeholder = null;

        public string CurrentUserName { get { return _currentUserName; } set { _currentUserName = value; OnPropertyChanged(); } }
        public Uri CurrentUserAvatar { get { return _currentUserAvatar; } set { _currentUserAvatar = value; OnPropertyChanged(); } }
        public bool IsFoldersLoading { get { return _isFoldersLoading; } private set { _isFoldersLoading = value; OnPropertyChanged(); } }
        public ObservableCollection<ConversationsFolder> Folders { get { return _folders; } private set { _folders = value; OnPropertyChanged(); } }
        public ConversationsFolder CurrentFolder { get { return _currentFolder; } set { _currentFolder = value; OnPropertyChanged(); } }
        public PlaceholderViewModel Placeholder { get { return _placeholder; } set { _placeholder = value; OnPropertyChanged(); } }

        public ImViewModel() {
            CurrentUserName = AppParameters.UserName;
            if (!string.IsNullOrEmpty(AppParameters.UserAvatar) && Uri.IsWellFormedUriString(AppParameters.UserAvatar, UriKind.Absolute))
                CurrentUserAvatar = new Uri(AppParameters.UserAvatar);

            new System.Action(async () => { await GetFoldersAndAllConvsAsync(); })();

            LongPoll.FolderCreated += LongPoll_FolderCreated;
            LongPoll.FolderDeleted += LongPoll_FolderDeleted;
            LongPoll.FoldersReordered += LongPoll_FoldersReordered;
        }

        public async Task RefreshAsync() {
            if (IsFoldersLoading) return;
            CurrentFolder = null;
            Folders.Clear();
            await GetFoldersAndAllConvsAsync();
        }

        private async Task GetFoldersAndAllConvsAsync() {
            Log.Info($"{GetType().Name} > Starting load conversations.");
            int count = AppParameters.ConversationsLoadCount < 0 ? 40 : AppParameters.ConversationsLoadCount;

            ConversationsResponse convs = null;
            if (IsFoldersLoading) return;
            IsFoldersLoading = true;
            Placeholder = null;

            var res = await Execute.GetConvsFoldersAndCounters(count, 0);
            if (res is ConvsAndCountersResponse resp) {
                convs = resp.Conversations;

                AppSession.AddUsersToCache(convs.Profiles);
                AppSession.AddGroupsToCache(convs.Groups);
                AppSession.AddContactsToCache(convs.Contacts);

                //VKNotificationHelper.SetBadge(resp.ShowOnlyNotMutedMessages
                //    ? (uint)resp.Counters.MessagesUnreadUnmuted
                //    : (uint)resp.Counters.Messages);

                ObservableCollection<ConversationViewModel> cvms = new ObservableCollection<ConversationViewModel>();
                var cached = AppSession.CachedConversations.Select(c => c.ConversationId).ToList();
                foreach (var c in convs.Items) {
                    if (cached.Contains(c.Conversation.Peer.Id)) {
                        var cvm = AppSession.CachedConversations.Where(con => con.ConversationId == c.Conversation.Peer.Id).FirstOrDefault();
                        if (cvm != null) cvms.Add(cvm);
                    } else {
                        ConversationViewModel cvm = new ConversationViewModel(c.Conversation, c.LastMessage, -1);
                        AppSession.CachedConversations.Add(cvm);
                        cvms.Add(cvm);
                    }
                }

                Folders = new ObservableCollection<ConversationsFolder> {
                        ConversationsFolder.GetForAll(cvms, resp.Conversations.Count, resp.Counters.Messages)
                    };
                CurrentFolder = Folders[0];

                foreach (Folder folder in resp.Folders.Items) {
                    var counters = resp.Counters.MessagesFolders.Where(fc => fc.FolderId == folder.Id).FirstOrDefault();
                    if (counters == null) continue;
                    Folders.Add(new ConversationsFolder(folder, counters.TotalCount, counters.UnmutedCount));
                }

                Folders.Add(ConversationsFolder.GetForArchive(resp.Counters.MessagesArchiveUnread));

                // Video message shapes
                AppSession.VideoMessageShapes = resp.VideoMessageShapes;

                PropertyChanged += ImViewModel_PropertyChanged;
            } else {
                Placeholder = PlaceholderViewModel.GetForHandledError(res, async () => await GetFoldersAndAllConvsAsync());
            }
            IsFoldersLoading = false;
        }

        private void ImViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(CurrentFolder)) {
                if (CurrentFolder != null && CurrentFolder.Conversations.Count == 0 && CurrentFolder.Id != 0) CurrentFolder?.LoadConversations();
            }

        }

        #region LP events

        private void LongPoll_FolderCreated(int id, string name, int randomId) {
            Folders.Insert(Folders.Count - 1, new ConversationsFolder(new Folder {
                Id = id,
                Name = name,
                RandomId = randomId
            }, 0, 0));
        }

        private void LongPoll_FolderDeleted(int id) {
            ConversationsFolder folder = Folders.Where(f => f.Id == id).FirstOrDefault();
            if (folder != null) {
                folder.Dispose();
                Folders.Remove(folder);
            }
        }

        private void LongPoll_FoldersReordered(List<int> foldersIds) {
            ConversationsFolder current = CurrentFolder;
            int currentIndex = -1;
            List<Tuple<int, ConversationsFolder>> toMove = new List<Tuple<int, ConversationsFolder>>();

            for (int i = 1; i < Folders.Count; i++) {
                ConversationsFolder folder = Folders[i];
                int index = foldersIds.IndexOf(folder.Id) + 1;
                if (index == i || index <= 0) continue;

                toMove.Add(new Tuple<int, ConversationsFolder>(index, folder));
                if (folder.Id == current.Id) currentIndex = index;
            }

            foreach (var mf in toMove) {
                Folders.Remove(mf.Item2);
                Folders.Insert(mf.Item1, mf.Item2);
            }

            CurrentFolder = Folders.ElementAt(currentIndex);
        }

        #endregion
    }
}
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.LongPoll;
using Elorucov.Laney.ViewModel;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Models {
    public class ConversationsFolder : BaseViewModel {
        const int ALL = 0;
        const int ARCHIVE = -1;

        private int _id;
        private string _type;
        private string _name;
        private string _nameWithoutEmoji;
        private string _icon;
        private string _emoji;
        private int _unreadConversationsCount = 0;
        private ObservableCollection<ConversationViewModel> _conversations;
        private ConversationViewModel _selectedConversation;
        private bool _isLoading = false;
        private PlaceholderViewModel _placeholder = null;

        public int Id { get { return _id; } private set { _id = value; OnPropertyChanged(); } }
        public string Type { get { return _type; } private set { _type = value; OnPropertyChanged(); } }
        public string Name { get { return _name; } private set { _name = value; OnPropertyChanged(); } }
        public string NameWithoutEmoji { get { return _nameWithoutEmoji; } private set { _nameWithoutEmoji = value; OnPropertyChanged(); } }
        public string Icon { get { return _icon; } private set { _icon = value; OnPropertyChanged(); } }
        public string Emoji { get { return _emoji; } private set { _emoji = value; OnPropertyChanged(); } }
        public int UnreadConversationsCount { get { return _unreadConversationsCount; } set { _unreadConversationsCount = value; OnPropertyChanged(); } }
        public ObservableCollection<ConversationViewModel> Conversations { get { return _conversations; } private set { Uninit(_conversations); _conversations = value; Init(_conversations); OnPropertyChanged(); } }
        public ConversationViewModel SelectedConversation { get { return _selectedConversation; } private set { _selectedConversation = value; OnPropertyChanged(); } }
        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; OnPropertyChanged(); } }
        public PlaceholderViewModel Placeholder { get { return _placeholder; } private set { _placeholder = value; OnPropertyChanged(); } }

        private int TotalConversationsCount = 0;

        public static ConversationsFolder GetForAll(ObservableCollection<ConversationViewModel> convs, int totalCount, int unreadCount) {
            return new ConversationsFolder {
                Id = ALL,
                Name = Locale.Get("all"),
                NameWithoutEmoji = Locale.Get("all"),
                Icon = "",
                Conversations = convs,
                TotalConversationsCount = totalCount,
                UnreadConversationsCount = unreadCount
            };
        }

        public static ConversationsFolder GetForArchive(int unreadCount) {
            return new ConversationsFolder {
                Id = ARCHIVE,
                Name = Locale.Get("archive_folder"),
                NameWithoutEmoji = Locale.Get("archive_folder"),
                Icon = "",
                Conversations = new ObservableCollection<ConversationViewModel>(),
                UnreadConversationsCount = unreadCount
            };
        }

        private ConversationsFolder() {
            AppSession.CurrentConversationVMChanged += AppSession_CurrentConversationVMChanged;

            LongPoll.FolderRenamed += LongPoll_FolderRenamed;
            LongPoll.FolderConversationsAdded += LongPoll_FolderConversationsAdded;
            LongPoll.FolderConversationsRemoved += LongPoll_FolderConversationsRemoved;
            LongPoll.FoldersUnreadCountersChanged += LongPoll_FoldersUnreadCountersChanged;
            if (Id == ALL) LongPoll.MessageReceived += LongPoll_MessageReceived;
            if (Id <= 0) LongPoll.CounterUpdated += LongPoll_CounterUpdated;
            LongPoll.Event10Received += LongPoll_Event10Received;
            LongPoll.Event12Received += LongPoll_Event12Received;
        }

        public ConversationsFolder(Folder folder, int totalUnreadCount, int unmutedUnreadConvsCount) {
            Id = folder.Id;
            Type = folder.Type;
            Name = folder.Name;
            Icon = "";
            UnreadConversationsCount = totalUnreadCount;
            Conversations = new ObservableCollection<ConversationViewModel>();
            SetNameAndEmoji();

            switch (folder.Type) {
                case "business":
                    Icon = "";
                    break;
                case "unread":
                    Icon = "";
                    break;
                case "personal":
                    Icon = "";
                    break;
            }

            AppSession.CurrentConversationVMChanged += AppSession_CurrentConversationVMChanged;

            LongPoll.FolderRenamed += LongPoll_FolderRenamed;
            LongPoll.FolderConversationsAdded += LongPoll_FolderConversationsAdded;
            LongPoll.FolderConversationsRemoved += LongPoll_FolderConversationsRemoved;
            LongPoll.FoldersUnreadCountersChanged += LongPoll_FoldersUnreadCountersChanged;
            if (Id == ALL) LongPoll.MessageReceived += LongPoll_MessageReceived;
            if (Id <= 0) LongPoll.CounterUpdated += LongPoll_CounterUpdated;
            LongPoll.Event10Received += LongPoll_Event10Received;
            LongPoll.Event12Received += LongPoll_Event12Received;
        }

        public void Dispose() {
            Conversations.Clear();

            AppSession.CurrentConversationVMChanged -= AppSession_CurrentConversationVMChanged;

            LongPoll.FolderRenamed -= LongPoll_FolderRenamed;
            LongPoll.FolderConversationsAdded -= LongPoll_FolderConversationsAdded;
            LongPoll.FolderConversationsRemoved -= LongPoll_FolderConversationsRemoved;
            LongPoll.FoldersUnreadCountersChanged -= LongPoll_FoldersUnreadCountersChanged;
            if (Id == ALL) LongPoll.MessageReceived -= LongPoll_MessageReceived;
            if (Id <= 0) LongPoll.CounterUpdated -= LongPoll_CounterUpdated;
            LongPoll.Event10Received -= LongPoll_Event10Received;
            LongPoll.Event12Received -= LongPoll_Event12Received;
        }

        private void Init(ObservableCollection<ConversationViewModel> conversations) {
            if (conversations == null) return;
            foreach (var conv in conversations) {
                conv.PropertyChanged += Conv_PropertyChanged;
            }
            conversations.CollectionChanged += Conversations_CollectionChanged;
        }

        private void Uninit(ObservableCollection<ConversationViewModel> conversations) {
            if (conversations == null) return;
            conversations.CollectionChanged -= Conversations_CollectionChanged;
            foreach (var conv in conversations) {
                conv.PropertyChanged -= Conv_PropertyChanged;
            }
        }

        private void Conversations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) foreach (var conv in e.OldItems.Cast<ConversationViewModel>()) {
                    conv.PropertyChanged -= Conv_PropertyChanged;
                }

            if (e.NewItems != null) foreach (var conv in e.NewItems.Cast<ConversationViewModel>()) {
                    conv.PropertyChanged += Conv_PropertyChanged;
                }
        }

        private void Conv_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            ConversationViewModel conv = sender as ConversationViewModel;
            Debug.WriteLine($"Folder: {Id} ({Name}). Conv: {conv.ConversationId}. Property: {e.PropertyName}");
            if (e.PropertyName == nameof(ConversationViewModel.SortId)) {
                Conversations.SortDescending(c => GetSortId(c));
                if (SelectedConversation != null) ChangeSelection(SelectedConversation.ConversationId);
            }
        }

        private object GetSortId(ConversationViewModel c) {
            if (Id > 0) {
                return c.SortId?.MinorId;
            } else {
                return c.SortId?.Id;
            }
        }

        //

        public async Task LoadConversations() {
            if (IsLoading) return;
            if (Conversations.Count > 0 && TotalConversationsCount > 0 && Conversations.Count >= TotalConversationsCount) return;

            IsLoading = true;
            Placeholder = null;
            Log.Info($"{GetType().Name} > Starting load conversations for folder {_id}. Have wt: {!string.IsNullOrEmpty(VkAPI.API.WebToken)}");
            int count = AppParameters.ConversationsLoadCount < 0 ? 200 : AppParameters.ConversationsLoadCount;

            int folderId = _id <= 0 ? 0 : _id;
            string filter = null;
            if (_id == ARCHIVE) filter = "archive";

            object response = await Messages.GetConversations(count, Conversations.Count, filter, folderId, VkAPI.API.WebToken);
            if (response is ConversationsResponse resp) {
                AppSession.AddUsersToCache(resp.Profiles);
                AppSession.AddGroupsToCache(resp.Groups);
                TotalConversationsCount = resp.Count;

                var cached = AppSession.CachedConversations.Select(c => c.ConversationId).ToList();
                foreach (var c in resp.Items) {
                    if (cached.Contains(c.Conversation.Peer.Id)) {
                        var cvm = AppSession.CachedConversations.Where(con => con.ConversationId == c.Conversation.Peer.Id).FirstOrDefault();
                        if (cvm != null) Conversations.Add(cvm);
                    } else {
                        ConversationViewModel cvm = new ConversationViewModel(c.Conversation, c.LastMessage, -1);
                        AppSession.CachedConversations.Add(cvm);
                        Conversations.Add(cvm);
                    }
                }
                ChangeSelection(AppSession.CurrentOpenedConversationId);
            } else {
                if (Conversations.Count > 0) {
                    Functions.ShowHandledErrorDialog(response, async () => await LoadConversations());
                } else {
                    Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await LoadConversations());
                }
            }
            IsLoading = false;
        }

        private void SetNameAndEmoji() {
            string icon = "";
            string emoji = string.Empty;
            string name = Name;

            string[] split = Name.Trim().Split(" ");
            if (split.Length > 0) {
                if (Functions.CheckEmoji(split[0], 1)) {
                    icon = string.Empty;
                    emoji = split[0];
                    if (split.Length == 1) {
                        name = split[0];
                    } else {
                        name = String.Join(" ", split.TakeLast(split.Length - 1));
                    }
                }
            }
            // TODO: случай, если первый символ emoji, а дальше текст без пробела.

            Icon = icon;
            Emoji = emoji;
            NameWithoutEmoji = name;
        }

        #region LP events

        private void LongPoll_MessageReceived(Message message) {
            if (Id != ALL || IsLoading) return;
            new System.Action(async () => {
                try {
                    var existed = Conversations.Where(c => c.ConversationId == message.PeerId).FirstOrDefault();
                    if (existed != null) return;
                    var conv = AppSession.CachedConversations.Where(c => c.ConversationId == message.PeerId).FirstOrDefault();
                    if (conv != null && !conv.IsArchived) {
                        // В кэше есть беседа, отсутствующая в списке, но куда пришло сообщение.
                        Conversations.Add(conv);
                        Conversations.SortDescending(c => GetSortId(c));
                    } else {
                        var response = await Messages.GetConversationById(message.PeerId, VkAPI.API.WebToken);
                        if (response is VKList<Conversation> scr) {
                            var apiconv = scr.Items[0];
                            if (apiconv.IsArchived) return;
                            ConversationViewModel cvm = new ConversationViewModel(apiconv, message);
                            Conversations.Add(cvm);
                            Conversations.SortDescending(c => GetSortId(c));
                        } else {
                            // Не оставим пользователя без новопоявившегося диалога!
                            Conversations.Clear();
                            await LoadConversations();
                        }
                    }
                } catch (Exception ex) {
                    // Даже при ошибке не оставим пользователя без новопоявившегося диалога!
                    Log.Error($"ConversationsFolder: cannot add new message to conversation. Doing full refresh... 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                    await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                        Conversations.Clear();
                        await LoadConversations();
                    });
                }

            })();
        }

        private void LongPoll_Event10Received(long peerId, int flags) {
            new System.Action(async () => {
                // Беседу убрали из архива (в папке "Все")
                if (Functions.CheckFlag(flags, 8388608) && Id == ALL) {
                    // пока переполучаем список бесед, ибо папка с архивами ещё не готова.
                    Conversations.Clear();
                    await LoadConversations();
                }

                // Беседу убрали из архива (в папке "Архив")
                if (Functions.CheckFlag(flags, 8388608) && Id == ARCHIVE) {
                    var conv = Conversations.Where(c => c.ConversationId == peerId).FirstOrDefault();
                    if (conv != null) Conversations.Remove(conv);
                }
            })();
        }

        private void LongPoll_Event12Received(long peerId, int flags) {
            // Беседу добавили в архив (в папке "Все")
            if (Functions.CheckFlag(flags, 8388608) && Id == ALL) {
                var conv = Conversations.Where(c => c.ConversationId == peerId).FirstOrDefault();
                if (conv != null) Conversations.Remove(conv);
            }

            // Беседу добавили в архив (в папке "Архив")
            if (Functions.CheckFlag(flags, 8388608) && Id == ARCHIVE) {
                // пока переполучаем список бесед, ибо папка с архивами ещё не готова.
                Conversations.Clear();
                new System.Action(async () => {
                    await LoadConversations();
                })();
            }
        }

        private void LongPoll_CounterUpdated(int unread, int unreadUnmuted, bool onlyUnmuted, int archiveUnread, int archiveUnreadUnmuted, int archiveMentions) {
            if (Id == ALL) UnreadConversationsCount = unread;
            if (Id == ARCHIVE) UnreadConversationsCount = archiveUnread;
        }

        private void AppSession_CurrentConversationVMChanged(object sender, ConversationViewModel e) {
            if (e == null) {
                SelectedConversation = null;
            } else {
                ChangeSelection(e.ConversationId);
            }
        }

        private void ChangeSelection(long conversationId) {
            var conv = Conversations.Where(c => c.ConversationId == conversationId).FirstOrDefault();
            if (conv != null) {
                SelectedConversation = conv;
            } else {
                SelectedConversation = null;
            }
        }

        private void LongPoll_FolderRenamed(int id, string name) {
            if (Id != id) return;
            Name = name;
            SetNameAndEmoji();
        }

        private void LongPoll_FolderConversationsAdded(int id, List<long> conversationsIds) {
            if (Id != id || Conversations.Count == 0) return;
            new System.Action(async () => {
                List<long> convsNeedLoaded = new List<long>();
                var convs = AppSession.CachedConversations.Where(c => conversationsIds.Contains(c.ConversationId)).ToList();
                if (convs.Count == conversationsIds.Count) {
                    // В кэше есть все те беседы, которые были добавлены в папку
                    foreach (var conv in convs) {
                        Conversations.Add(conv);
                    }
                    Conversations.SortDescending(c => GetSortId(c));
                } else {
                    // Т. к. есть проблемы с получением беседы из API (вместе с последними сообщениями),
                    // то пока что просто переполучаем список бесед в папке.
                    Conversations.Clear();
                    await LoadConversations();
                }
            })();
        }

        private void LongPoll_FolderConversationsRemoved(int id, List<long> conversationsIds) {
            if (Id != id) return;
            var convs = Conversations.Where(c => conversationsIds.Contains(c.ConversationId)).ToList();
            foreach (var conv in convs) {
                Conversations.Remove(conv);
            }
        }

        private void LongPoll_FoldersUnreadCountersChanged(int id, int unread, int unreadUnmuted) {
            if (Id != id) return;
            UnreadConversationsCount = unread;
        }

        #endregion
    }
}
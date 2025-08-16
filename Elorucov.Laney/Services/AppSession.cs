using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Elorucov.Laney.Services {
    public class AppSession {
        private static ConversationViewModel _currentConversationVM;

        public static ImViewModel ImViewModel;
        public static ConversationViewModel CurrentConversationVM { get { return _currentConversationVM; } set { _currentConversationVM = value; CurrentConversationVMChanged?.Invoke(null, value); } }
        public static List<ConversationViewModel> CachedConversations { get; private set; } = new List<ConversationViewModel>();
        public static List<int> SortedReactions { get { return GetSortedReactions(); } }
        public static event EventHandler<ConversationViewModel> CurrentConversationVMChanged;

        //

        public static ConcurrentDictionary<long, User> CachedUsers = new ConcurrentDictionary<long, User>();
        public static ConcurrentDictionary<long, UserLite> CachedUsersLite = new ConcurrentDictionary<long, UserLite>();
        public static ConcurrentDictionary<long, Group> CachedGroups = new ConcurrentDictionary<long, Group>();
        public static ConcurrentDictionary<long, GroupLite> CachedGroupsLite = new ConcurrentDictionary<long, GroupLite>();
        public static ConcurrentDictionary<long, Contact> CachedContacts = new ConcurrentDictionary<long, Contact>();
        public static List<long> ActiveStickerPacks = new List<long>();
        public static List<StoreProduct> CachedStickerPacks = null;
        public static DateTime? CachedStickerPacksTimestamp = null;
        public static List<string> MessagesTranslationLanguagePairs = new List<string>();
        public static NotificationSettings PushSettings = null;
        public static List<ReactionAsset> ReactionsAssets = null;
        public static List<int> AvailableReactions = new List<int>();
        public static Dictionary<long, List<UGCStickerPack>> UGCPacks = null;
        public static VideoMessageShapesResponse VideoMessageShapes = null;
        public static List<FeedViewModel> OpenedWallsAndFeeds = new List<FeedViewModel>();

        private static Stack<long> _chatNavHistory = new Stack<long>();
        public static Stack<long> ChatNavigationHistory { get { return _chatNavHistory; } }

        public static void AddUsersToCache(List<User> users) {
            if (users != null) {
                foreach (User u in users) {
                    if (u == null) continue;
                    if (CachedUsers.ContainsKey(u.Id)) {
                        CachedUsers[u.Id] = u;
                    } else {
                        bool result = CachedUsers.TryAdd(u.Id, u);
                        if (!result) Log.Warn($"Cannot add user to cache! Id: {u.Id}");
                    }
                }
            }
        }

        public static void AddUsersToCache(List<UserLite> users) {
            if (users != null) {
                foreach (UserLite u in users) {
                    if (u == null) continue;
                    if (CachedUsersLite.ContainsKey(u.Id)) {
                        CachedUsersLite[u.Id] = u;
                    } else {
                        bool result = CachedUsersLite.TryAdd(u.Id, u);
                        if (!result) Log.Warn($"Cannot add user to cache! Id: {u.Id}");
                    }
                }
            }
        }

        public static void AddGroupsToCache(List<Group> groups) {
            if (groups != null) {
                foreach (Group g in groups) {
                    if (g == null) continue;
                    if (CachedGroups.ContainsKey(g.Id)) {
                        CachedGroups[g.Id] = g;
                    } else {
                        bool result = CachedGroups.TryAdd(g.Id, g);
                        if (!result) Log.Warn($"Cannot add group to cache! Id: {g.Id}");
                    }
                }
            }
        }

        public static void AddGroupsToCache(List<GroupLite> groups) {
            if (groups != null) {
                foreach (GroupLite g in groups) {
                    if (g == null) continue;
                    if (CachedGroupsLite.ContainsKey(g.Id)) {
                        CachedGroupsLite[g.Id] = g;
                    } else {
                        bool result = CachedGroupsLite.TryAdd(g.Id, g);
                        if (!result) Log.Warn($"Cannot add group to cache! Id: {g.Id}");
                    }
                }
            }
        }

        public static void AddContactsToCache(List<Contact> contacts) {
            if (contacts != null) {
                foreach (Contact c in contacts) {
                    if (c == null) continue;
                    if (CachedContacts.ContainsKey(c.Id)) {
                        CachedContacts[c.Id] = c;
                    } else {
                        bool result = CachedContacts.TryAdd(c.Id, c);
                        if (!result) Log.Warn($"Cannot add contact to cache! Id: {c.Id}");
                    }
                }
            }
        }

        public static User GetCachedUser(long id) {
            if (CachedUsers.ContainsKey(id)) return CachedUsers[id];
            Log.Warn($"User with id {id} not found in cache!");
            return null;
        }

        public static UserLite GetCachedUserLite(long id) {
            if (CachedUsersLite.ContainsKey(id)) return CachedUsersLite[id];
            Log.Warn($"User with id {id} not found in cache!");
            return null;
        }

        public static Group GetCachedGroup(long id) {
            if (id <= -1) id = id * -1;
            if (CachedGroups.ContainsKey(id)) return CachedGroups[id];
            Log.Warn($"Group with id {id} not found in cache!");
            return null;
        }

        public static GroupLite GetCachedGroupLite(long id) {
            if (id <= -1) id = id * -1;
            if (CachedGroupsLite.ContainsKey(id)) return CachedGroupsLite[id];
            Log.Warn($"Group with id {id} not found in cache!");
            return null;
        }

        public static Contact GetCachedContact(long id) {
            if (CachedContacts.ContainsKey(id)) return CachedContacts[id];
            Log.Warn($"Contact with id {id} not found in cache!");
            return null;
        }

        public static long CurrentOpenedConversationId { get { return CurrentConversationVM != null ? CurrentConversationVM.ConversationId : 0; } }

        // First name, last name, avatar
        public static Tuple<string, string, Uri> GetNameAndAvatar(long id, bool oneLetterForLastName = false) {
            if (id.IsUser()) {
                User u = GetCachedUser(id);
                if (u == null) return null;

                string lastName = u.LastName;
                if (oneLetterForLastName && !string.IsNullOrEmpty(lastName) && lastName.Length > 1) {
                    if (lastName.Length > 1) lastName = lastName[0].ToString();
                    lastName = lastName + ".";
                }
                return new Tuple<string, string, Uri>(u.FirstName, lastName, u.Photo);
            } else if (id.IsGroup()) {
                Group g = GetCachedGroup(id);
                if (g == null) return null;
                return new Tuple<string, string, Uri>(g.Name, null, g.Photo);
            }
            return null;
        }

        public static Tuple<string, string, Uri> GetNameAndAvatarFromLiteCache(long id, bool oneLetterForLastName = false) {
            if (id.IsUser()) {
                UserLite u = GetCachedUserLite(id);
                if (u == null) return null;

                string lastName = u.LastName;
                if (oneLetterForLastName && !string.IsNullOrEmpty(lastName) && lastName.Length > 1) {
                    if (lastName.Length > 1) lastName = lastName[0].ToString();
                    lastName = lastName + ".";
                }
                return new Tuple<string, string, Uri>(u.FirstName, lastName, u.Photo);
            } else if (id.IsGroup()) {
                GroupLite g = GetCachedGroupLite(id);
                if (g == null) return null;
                return new Tuple<string, string, Uri>(g.Name, null, g.Photo);
            }
            return null;
        }

        private static List<int> GetSortedReactions() {
            var splitted = AppParameters.QuickReactions.Split(",").ToList();
            if (splitted.Count != 7) return AvailableReactions;

            List<int> sorted = new List<int>();
            foreach (string ids in splitted) {
                int rid = 0;
                if (!int.TryParse(ids, out rid)) return AvailableReactions;
                if (!AvailableReactions.Contains(rid)) continue;
                sorted.Add(rid);
            }

            List<int> allOther = AvailableReactions.Where(r => !sorted.Contains(r)).ToList();
            sorted.AddRange(allOther);

            return sorted;
        }

        public static void Clear() {
            ImViewModel = null;
            CurrentConversationVM = null;
            CachedConversations = new List<ConversationViewModel>();
            CachedUsers.Clear();
            CachedUsersLite.Clear();
            CachedGroups.Clear();
            CachedGroupsLite.Clear();
            CachedContacts.Clear();
            PushSettings = null;
            ReactionsAssets = null;
            AvailableReactions = new List<int>();
            UGCPacks = null;
            VideoMessageShapes = null;
            _chatNavHistory.Clear();
            OpenedWallsAndFeeds.Clear();
        }
    }
}
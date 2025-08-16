using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Elorucov.Laney.ViewModel {
    public class FeedViewModel : BaseViewModel {
        private string _ownerName;
        private bool _searchAvailable = false;
        private bool _searchMode = false;
        private string _searchQuery;
        private PostComposerViewModel _composer;
        private ObservableCollection<WallPost> _posts = new ObservableCollection<WallPost>();
        private bool _isLoading;
        private PlaceholderViewModel _placeholder;

        private RelayCommand _refreshCommand;

        public long Id { get; private set; }
        public string OwnerName { get { return _ownerName; } private set { _ownerName = value; OnPropertyChanged(); } }
        public bool SearchAvailable { get { return _searchAvailable; } set { _searchAvailable = value; OnPropertyChanged(); } }
        public bool SearchMode { get { return _searchMode; } set { _searchMode = value; OnPropertyChanged(); } }
        public string SearchQuery { get { return _searchQuery; } set { _searchQuery = value; OnPropertyChanged(); } }
        public PostComposerViewModel Composer { get { return _composer; } private set { _composer = value; OnPropertyChanged(); } }
        public ObservableCollection<WallPost> Posts { get { return _posts; } private set { _posts = value; OnPropertyChanged(); } }
        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; OnPropertyChanged(); } }
        public PlaceholderViewModel Placeholder { get { return _placeholder; } private set { _placeholder = value; OnPropertyChanged(); } }

        public RelayCommand RefreshCommand { get { return _refreshCommand; } set { _refreshCommand = value; OnPropertyChanged(); } }

        bool IsFeedList => Id > 2000000000;
        bool IsNewsfeed => Id == Constants.FEED_SOURCE_ALL || Id == Constants.FEED_SOURCE_FRIENDS ||
            Id == Constants.FEED_SOURCE_GROUPS || Id == Constants.FEED_SOURCE_PAGES || IsFeedList;
        string startFrom; // need to newsfeed.get

        public FeedViewModel(long ownerId, string name) {
            Id = ownerId;
            OwnerName = name;

            RefreshCommand = new RelayCommand(async o => await RefreshFeedAsync());
            new System.Action(async () => { await LoadPostsAsync(); })();
        }

        public async Task LoadPostsAsync(bool needClear = false) {
            if (IsLoading) return;
            Placeholder = null;
            IsLoading = true;

            if (needClear) {
                startFrom = "";
                Posts.Clear();
            }

            try {
                object response = null;
                if (IsNewsfeed) {
                    Composer = new PostComposerViewModel(AppParameters.UserID, onPostPublished: async () => await RefreshFeedAsync());
                    string sourceIds = null;
                    if (IsFeedList) {
                        sourceIds = $"list{Id - 2000000000}";
                    } else {
                        switch (Id) {
                            case Constants.FEED_SOURCE_FRIENDS:
                                sourceIds = "friends";
                                break;
                            case Constants.FEED_SOURCE_GROUPS:
                                sourceIds = "groups";
                                break;
                            case Constants.FEED_SOURCE_PAGES:
                                sourceIds = "pages";
                                break;
                        }
                    }
                    response = await Newsfeed.Get("post", sourceIds, startFrom, 20);
                } else if (Id.IsUser() || Id.IsGroup()) {
                    response = await Wall.Get(Id, Posts.Count, 20, "all");
                } else {
                    Log.Error($"FeedViewModel > LoadPosts: unknown wall/feed type! Id: {Id}");
                    return;
                }

                if (response is NewsfeedResponse nresp) {
                    AppSession.AddUsersToCache(nresp.Profiles);
                    AppSession.AddGroupsToCache(nresp.Groups);

                    foreach (var post in nresp.Items) {
                        if (!post.MarkedAsAds && post.Type == "post") Posts.Add(post);
                    }

                    startFrom = nresp.NextFrom;
                } else if (response is VKList<WallPost> presp) {
                    AppSession.AddUsersToCache(presp.Profiles);
                    AppSession.AddGroupsToCache(presp.Groups);

                    if (Composer == null) {
                        bool canPost = false;
                        bool canSuggest = false;
                        GroupType? groupType = null;
                        bool donutAvailable = false;
                        bool canPostOnGroupWallAsUser = false;
                        if (Id.IsUser()) {
                            var user = presp.Profiles.Where(u => u.Id == Id).FirstOrDefault();
                            if (user == null) user = AppSession.GetCachedUser(Id);

                            if (user == null) {
                                var uresp = await Users.Get(Id);
                                if (uresp is List<User> users) {
                                    user = users.FirstOrDefault();
                                    AppSession.AddUsersToCache(new List<User> { user });
                                }
                            }

                            canPost = user != null && user.CanPost;
                        } else if (Id.IsGroup()) {
                            var group = presp.Groups.Where(g => g.Id == -Id).FirstOrDefault();
                            if (group == null) group = AppSession.GetCachedGroup(Id);

                            if (group == null) {
                                var gresp = await Groups.GetById(-Id);
                                if (gresp is VKList<object> groups) {
                                    group = groups.Groups[0];
                                    AppSession.AddGroupsToCache(new List<Group> { group });
                                }
                            }

                            if (group != null) {
                                canPost = group.CanPost;
                                canPostOnGroupWallAsUser = group.IsAdmin && group.Wall == 1;
                                canSuggest = group.CanSuggest;
                                donutAvailable = group.Donut != null;
                                groupType = group.Type;
                            }
                        }

                        if (canPost || canSuggest) {
                            Composer = new PostComposerViewModel(Id, groupType, canPostOnGroupWallAsUser, canSuggest, donutAvailable, async () => await RefreshFeedAsync());
                        }
                    }

                    SearchAvailable = !IsNewsfeed;

                    foreach (var post in presp.Items) {
                        if (!post.MarkedAsAds && post.Type == "post") Posts.Add(post);
                    }
                } else {
                    Placeholder = PlaceholderViewModel.GetForHandledError(response);
                }
            } catch (Exception ex) {
                Placeholder = PlaceholderViewModel.GetForHandledError(ex);
            } finally {
                IsLoading = false;
            }
        }

        private async Task RefreshFeedAsync() {
            SearchMode = false;
            SearchQuery = string.Empty;
            await LoadPostsAsync(true);
        }

        public async Task SearchPostsAsync(bool needClear = false) {
            if (IsNewsfeed || IsLoading) return;

            if (string.IsNullOrEmpty(SearchQuery)) {
                await RefreshFeedAsync();
                return;
            }

            SearchMode = true;
            Placeholder = null;
            IsLoading = true;

            if (needClear) {
                startFrom = "";
                Posts.Clear();
            }

            try {
                var response = await Wall.Search(Id, SearchQuery, false, Posts.Count, 20);
                if (response is VKList<WallPost> presp) {
                    AppSession.AddUsersToCache(presp.Profiles);
                    AppSession.AddGroupsToCache(presp.Groups);

                    foreach (var post in presp.Items) {
                        if (!post.MarkedAsAds && post.Type == "post") Posts.Add(post);
                    }
                } else {
                    Placeholder = PlaceholderViewModel.GetForHandledError(response);
                }
            } catch (Exception ex) {
                Placeholder = PlaceholderViewModel.GetForHandledError(ex);
            } finally {
                IsLoading = false;
            }
        }
    }
}
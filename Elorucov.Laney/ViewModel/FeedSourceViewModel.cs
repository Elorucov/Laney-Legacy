using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.ViewModel.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Elorucov.Laney.ViewModel {
    internal class FeedSourceViewModel : BaseViewModel {
        private bool _isLoading;
        private PlaceholderViewModel _placeholder;
        private string _searchQuery;
        private ObservableCollection<Grouping<Entity>> _groupedItems = new ObservableCollection<Grouping<Entity>>();

        public bool IsLoading { get { return _isLoading; } set { _isLoading = value; OnPropertyChanged(); } }
        public PlaceholderViewModel Placeholder { get { return _placeholder; } set { _placeholder = value; OnPropertyChanged(); } }
        public string SearchQuery { get { return _searchQuery; } set { _searchQuery = value; OnPropertyChanged(); } }
        public ObservableCollection<Grouping<Entity>> GroupedItems { get { return _groupedItems; } set { _groupedItems = value; OnPropertyChanged(); } }

        public FeedSourceViewModel() {
            new System.Action(async () => { await GetSourcesAsync(); })();
        }

        private async Task GetSourcesAsync() {
            if (IsLoading) return;
            try {
                Placeholder = null;
                GroupedItems.Clear();
                IsLoading = true;
                var response = await Execute.GetFeedSources();
                if (response is FeedSourcesResponse fsr) {
                    var predefined = new List<Entity> {
                        new Entity(Constants.FEED_SOURCE_ALL, Locale.Get("feed_source_my"), ""),
                        new Entity(AppParameters.UserID, Locale.Get("feed_source_my_wall"), ""),
                        new Entity(Constants.FEED_SOURCE_FRIENDS, Locale.Get("feed_source_friends"), ""),
                        new Entity(Constants.FEED_SOURCE_GROUPS, Locale.Get("feed_source_groups"), ""),
                        // new Entity(Constants.FEED_SOURCE_PAGES, "Pages", "")
                    };

                    if (fsr.Lists.Count > 0) {
                        foreach (var list in fsr.Lists) {
                            predefined.Add(new Entity(list.Id + 2000000000, list.Title, ""));
                        }
                    }

                    GroupedItems.Add(new Grouping<Entity>(predefined));

                    // Friends
                    var friends = new List<Entity>();
                    foreach (var friend in fsr.Friends) {
                        bool available = !friend.IsClosed || (friend.IsClosed && friend.CanAccessClosed);
                        if (available) friends.Add(new Entity(friend.Id, friend.FullName, null, friend.Photo));
                    }
                    // if (fsr.HasMoreFriends) friends.Add(new Entity(Constants.FEED_SOURCE_PICK_FRIENDS, "All friends", ""));
                    if (friends.Count > 0) GroupedItems.Add(new Grouping<Entity>(friends, Locale.Get("friends").ToUpper()));

                    // Communities
                    var communities = new List<Entity>();
                    foreach (var group in fsr.Groups) {
                        bool available = group.Wall != 0;
                        if (available) communities.Add(new Entity(-group.Id, group.Name, null, group.Photo));
                    }
                    // if (fsr.HasMoreGroups) communities.Add(new Entity(Constants.FEED_SOURCE_PICK_COMMUNITIES, "All communities", ""));
                    if (communities.Count > 0) GroupedItems.Add(new Grouping<Entity>(communities, Locale.Get("communities").ToUpper()));

                    // Communities where user is admin
                    var adminComms = new List<Entity>();
                    foreach (var group2 in fsr.AdminedGroups) {
                        adminComms.Add(new Entity(-group2.Id, group2.Name, null, group2.Photo));
                    }
                    // if (fsr.HasMoreAdminedGroups) adminComms.Add(new Entity(Constants.FEED_SOURCE_PICK_ADMINED_COMMUNITIES, "All admined communities", ""));
                    if (adminComms.Count > 0) GroupedItems.Add(new Grouping<Entity>(adminComms, Locale.Get("admined_comms").ToUpper()));
                } else {
                    PlaceholderViewModel.GetForHandledError(response, async () => await GetSourcesAsync());
                }
            } catch (Exception ex) {
                GroupedItems.Clear();
                PlaceholderViewModel.GetForHandledError(ex, async () => await GetSourcesAsync());
            } finally {
                IsLoading = false;
            }
        }

        public async Task SearchAsync() {
            if (IsLoading) return;
            if (string.IsNullOrEmpty(SearchQuery)) {
                await GetSourcesAsync();
            } else {
                try {
                    Placeholder = null;
                    GroupedItems.Clear();
                    IsLoading = true;
                    var response = await Execute.SearchUsersAndGroups(SearchQuery);
                    if (response is UsersAndGroupsList ugl) {
                        // Users
                        var users = new List<Entity>();
                        foreach (var user in ugl.Users) {
                            bool available = !user.IsClosed || (user.IsClosed && user.CanAccessClosed);
                            if (available) users.Add(new Entity(user.Id, user.FullName, null, user.Photo));
                        }
                        if (users.Count > 0) GroupedItems.Add(new Grouping<Entity>(users, Locale.Get("feed_source_found_users")));

                        // Communities
                        var communities = new List<Entity>();
                        foreach (var group in ugl.Groups) {
                            bool available = group.Wall != 0;
                            if (available) communities.Add(new Entity(-group.Id, group.Name, null, group.Photo));
                        }
                        if (communities.Count > 0) GroupedItems.Add(new Grouping<Entity>(communities, Locale.Get("feed_source_found_comms")));

                        if (GroupedItems.Count == 0) {
                            Placeholder = new PlaceholderViewModel('', content: Locale.Get("not_found"));
                        }
                    } else {
                        Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await SearchAsync());
                    }
                } catch (Exception ex) {
                    GroupedItems.Clear();
                    Placeholder = PlaceholderViewModel.GetForHandledError(ex, async () => await SearchAsync());
                } finally {
                    IsLoading = false;
                }
            }
        }
    }
}
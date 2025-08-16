using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Elorucov.VkAPI.Objects.Upload;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Globalization.Collation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.ViewModel {
    internal class ChatCreationViewModel : BaseViewModel {
        private string _chatName;
        private BitmapImage _chatPhotoPreview;
        private ObservableCollection<User> _friends = new ObservableCollection<User>();
        private ObservableCollection<FriendsGroup> _groupedFriends = new ObservableCollection<FriendsGroup>();
        private ObservableCollection<User> _selectedFriends = new ObservableCollection<User>();
        private string _searchQuery;
        private bool _isLoading;
        private PlaceholderViewModel _placeholder;
        private bool _canCreateChat;

        private RelayCommand _chatPhotoSetCommand;
        private RelayCommand _customizeChatSetingsCommand;
        private RelayCommand _createCommand;

        public string ChatName { get { return _chatName; } set { _chatName = value; OnPropertyChanged(); } }
        public BitmapImage ChatPhotoPreview { get { return _chatPhotoPreview; } set { _chatPhotoPreview = value; OnPropertyChanged(); } }
        public ObservableCollection<User> Friends { get { return _friends; } set { _friends = value; OnPropertyChanged(); } }
        public ObservableCollection<FriendsGroup> GroupedFriends { get { return _groupedFriends; } private set { _groupedFriends = value; OnPropertyChanged(); } }
        public ObservableCollection<User> SelectedFriends { get { return _selectedFriends; } set { _selectedFriends = value; OnPropertyChanged(); } }
        public string SearchQuery { get { return _searchQuery; } set { _searchQuery = value; OnPropertyChanged(); } }
        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; OnPropertyChanged(); } }
        public PlaceholderViewModel Placeholder { get { return _placeholder; } private set { _placeholder = value; OnPropertyChanged(); } }
        public bool CanCreateChat { get { return _canCreateChat; } private set { _canCreateChat = value; OnPropertyChanged(); } }

        public RelayCommand ChatPhotoSetCommand { get { return _chatPhotoSetCommand; } private set { _chatPhotoSetCommand = value; OnPropertyChanged(); } }
        public RelayCommand CustomizeChatSetingsCommand { get { return _customizeChatSetingsCommand; } private set { _customizeChatSetingsCommand = value; OnPropertyChanged("CustomizeChatSetingsCommand"); } }
        public RelayCommand CreateCommand { get { return _createCommand; } private set { _createCommand = value; OnPropertyChanged(); } }

        private StorageFile ChatPhoto;
        private ChatPermissions Permissions;
        private System.Action BackRequested;

        public ChatCreationViewModel(System.Action backRequested) {
            BackRequested = backRequested;

            PropertyChanged += (a, b) => {
                switch (b.PropertyName) {
                    case nameof(ChatName):
                    case nameof(SelectedFriends):
                        CheckCanCreateChat();
                        break;
                }
            };

            ChatPhotoSetCommand = new RelayCommand((o) => {
                ShowChatPhotoMenu((o as FrameworkElement));
            });

            CustomizeChatSetingsCommand = new RelayCommand((o) => {
                ChatSettingsModal csm = new ChatSettingsModal();
                csm.Closed += (a, b) => Permissions = (ChatPermissions)b;
                csm.Show();
            });

            CreateCommand = new RelayCommand(async (o) => await TryCreateChatAsync());

            new System.Action(async () => { await LoadFriendsAsync(); })();
        }

        private async Task LoadFriendsAsync() {
            if (IsLoading) return;
            IsLoading = true;
            Placeholder = null;
            Friends.Clear();
            GroupedFriends.Clear();

            Log.Info($"{GetType().Name} > Getting friends...");
            var res = await VkAPI.Methods.Friends.Get(AppParameters.UserID);

            if (res is VKList<User>) {
                VKList<User> resr = res as VKList<User>;
                Log.Info($"{GetType().Name} > Adding users to cache...");
                AppSession.AddUsersToCache(resr.Items);
                Log.Info($"{GetType().Name} > Friends loaded.");
                Friends = new ObservableCollection<User>(resr.Items);
                GroupFriends();
            } else {
                Placeholder = PlaceholderViewModel.GetForHandledError(res, async () => await LoadFriendsAsync());
            }

            IsLoading = false;
        }

        private void GroupFriends() {
            CharacterGroupings slg = new CharacterGroupings();

            if (_friends != null && _friends.Count > 0) {
                //if (!dontAddImportants) {
                //    int importantcount = _friends.Count > 5 ? 5 : _friends.Count;
                //    ObservableCollection<User> important = new ObservableCollection<User>();
                //    for (short i = 0; i < importantcount; i++) {
                //        if (_friends[i].Deactivated == DeactivationState.No && _friends[i].CanWritePrivateMessage == 1) important.Add(_friends[i]);
                //    }
                //    GroupedFriends.Add(new FriendsGroup(Locale.Get("important").ToUpper(), "", Theme.DefaultIconsFont, important));
                //}

                var friends = _friends.ToList();
                foreach (CharacterGrouping key in slg) {
                    if (!string.IsNullOrWhiteSpace(key.Label)) {
                        string k = key.First.ToUpper();
                        var a = from b in _friends where b.FirstName[0].ToString().ToUpper() == k select b;
                        if (a.Count() > 0) {
                            var c = new FriendsGroup(k, k, null, new ObservableCollection<User>(a), true);
                            foreach (User u in a) {
                                friends.Remove(u);
                            }
                            GroupedFriends.Add(c);
                        }
                    }
                }

                if (friends.Count > 0) {
                    var c = new FriendsGroup("~", "", Theme.DefaultIconsFont, new ObservableCollection<User>(friends), true);
                    GroupedFriends.Add(c);
                }
            }
        }

        public void AddFriendToSelected(User user) {
            if (!SelectedFriends.Contains(user)) SelectedFriends.Add(user);
            OnPropertyChanged(nameof(SelectedFriends)); // чтобы триггернулся visibility
        }

        public void RemoveFriendFromSelected(User user) {
            if (SelectedFriends.Contains(user)) SelectedFriends.Remove(user);
            OnPropertyChanged(nameof(SelectedFriends)); // чтобы триггернулся visibility
        }

        private void CheckCanCreateChat() {
            CanCreateChat = !string.IsNullOrEmpty(ChatName) || SelectedFriends.Count > 0;
        }

        private async Task TryCreateChatAsync() {
            if (string.IsNullOrEmpty(ChatName) && SelectedFriends.Count == 0) return;
            List<long> userIds = SelectedFriends.Select(u => u.Id).ToList();
            if (userIds.Count == 0) userIds.Add(AppParameters.UserID);

            VK.VKUI.Popups.ScreenSpinner<object> ssp = new VK.VKUI.Popups.ScreenSpinner<object>();
            object resp = await ssp.ShowAsync(Messages.CreateChat(userIds, ChatName, Permissions));
            if (resp is CreateChatResponse r) {
                BackRequested?.Invoke();
                Main.GetCurrent().ShowConversationPage(2000000000 + r.ChatId);
                if (ChatPhoto != null) await StartUploadChatPhotoAsync(ChatPhoto, r.ChatId);
            } else {
                Functions.ShowHandledErrorDialog(resp, async () => await TryCreateChatAsync());
            }
        }

        // Chat photo upload

        private void ShowChatPhotoMenu(FrameworkElement target) {
            MenuFlyout mf = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };

            MenuFlyoutItem browse = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("chatphoto_browse") };
            MenuFlyoutItem create = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("chatphoto_create") };
            MenuFlyoutItem delete = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("chatphoto_delete") };

            browse.Click += async (a, b) => await ChooseFileForChatPhotoAsync();
            create.Click += (a, b) => {
                AvatarCreator acm = new AvatarCreator(true);
                acm.Closed += async (c, d) => {
                    if (d != null && d is StorageFile file) await SetChatPhotoPreviewAsync(file);
                };
                acm.Show();
            };
            delete.Click += (a, b) => {
                ChatPhoto = null;
                ChatPhotoPreview = null;
            };

            mf.Items.Add(browse);
            mf.Items.Add(create);
            if (ChatPhoto != null) mf.Items.Add(delete);

            mf.ShowAt(target);
        }

        private async Task ChooseFileForChatPhotoAsync() {
            FileOpenPicker fop = new FileOpenPicker();
            fop.FileTypeFilter.Add(".jpg");
            fop.FileTypeFilter.Add(".jpeg");
            fop.FileTypeFilter.Add(".png");
            fop.FileTypeFilter.Add(".bmp");
            fop.FileTypeFilter.Add(".gif");
            fop.FileTypeFilter.Add(".heic");
            fop.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fop.ViewMode = PickerViewMode.Thumbnail;

            var file = await fop.PickSingleFileAsync();
            if (file != null) await SetChatPhotoPreviewAsync(file);
        }

        private async Task SetChatPhotoPreviewAsync(StorageFile file) {
            ChatPhoto = file;
            var stream = await file.OpenAsync(FileAccessMode.Read);

            BitmapImage image = new BitmapImage();
            await image.SetSourceAsync(stream);
            ChatPhotoPreview = image;
        }

        private async Task StartUploadChatPhotoAsync(StorageFile file, long chatId) {
            object resp = await Photos.GetChatUploadServer(chatId);
            if (resp is VkUploadServer server) {
                await UploadChatPhotoAsync(server.Uri, file);
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
        }

        private async Task UploadChatPhotoAsync(Uri uri, StorageFile file) {
            IFileUploader vkfu = APIHelper.GetUploadMethod("file", uri, file);
            vkfu.UploadFailed += UploadFailed;
            string resp = await vkfu.UploadAsync();
            if (resp != null) {
                string result = VKResponseHelper.GetJSONInResponseObject(resp);

                VK.VKUI.Popups.ScreenSpinner<object> ssp2 = new VK.VKUI.Popups.ScreenSpinner<object>();
                object resp2 = await Messages.SetChatPhoto(result);
                if (resp2 is SetChatPhotoResult scpresult) {
                    // ¯\_(ツ)_/¯
                } else {
                    Functions.ShowHandledErrorDialog(resp2);
                }
            }
        }

        private void UploadFailed(Exception e) {
            Log.Error($"{GetType().Name} > Upload failed! 0x{e.HResult.ToString("x8")}.");
            Functions.ShowHandledErrorDialog(e);
        }
    }
}
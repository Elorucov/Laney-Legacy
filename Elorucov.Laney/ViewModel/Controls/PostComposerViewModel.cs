using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.ViewModel.Controls {
    public class PostComposerViewModel : BaseViewModel {
        private string _text;
        private string _placeholderText;
        private string _postButtonText;
        private ObservableCollection<OutboundAttachmentViewModel> _attachments = new ObservableCollection<OutboundAttachmentViewModel>();
        private Geopoint _geopoint;
        private DateTime? _publishTime;
        private int _primaryAttachmentsMode;
        private int _privacy;
        private int _donSetting;
        private int _publishToNonDonsAfterDays;
        private bool _postAsGroup = true; // "true" required even if CanPublishFromUserInGroup is false, because "IsEnabled" prop in checkboxes bound to PostAsGroup prop.
        private bool _disableComments;
        private bool _showSigner;
        private bool _disableNotifications;

        private RelayCommand _attachPhotoCommand;
        private RelayCommand _attachVideoCommand;
        private RelayCommand _attachFileCommand;
        private RelayCommand _attachAudioCommand;
        private RelayCommand _attachPollCommand;
        private RelayCommand _attachLocationCommand;
        private RelayCommand _removeLocationCommand;
        private RelayCommand _removePublishTimeCommand;
        private RelayCommand _publishCommand;

        public string Text { get { return _text; } set { _text = value; OnPropertyChanged(); } }
        public string PlaceholderText { get { return _placeholderText; } private set { _placeholderText = value; OnPropertyChanged(); } }
        public string PostButtonText { get { return _postButtonText; } private set { _postButtonText = value; OnPropertyChanged(); } }
        public ObservableCollection<OutboundAttachmentViewModel> Attachments { get { return _attachments; } private set { _attachments = value; OnPropertyChanged(); } }
        public Geopoint Geopoint { get { return _geopoint; } private set { _geopoint = value; OnPropertyChanged(); } }
        public DateTime? PublishTime { get { return _publishTime; } set { _publishTime = value; OnPropertyChanged(); } }
        public int PrimaryAttachmentsMode { get { return _primaryAttachmentsMode; } set { _primaryAttachmentsMode = value; OnPropertyChanged(); } }
        public int Privacy { get { return _privacy; } set { _privacy = value; OnPropertyChanged(); } }
        public int DonSetting { get { return _donSetting; } set { _donSetting = value; OnPropertyChanged(); } }
        public int PublishToNonDonsAfterDays { get { return _publishToNonDonsAfterDays; } set { _publishToNonDonsAfterDays = value; OnPropertyChanged(); } }
        public bool PostAsGroup { get { return _postAsGroup; } set { _postAsGroup = value; OnPropertyChanged(); } }
        public bool DisableComments { get { return _disableComments; } set { _disableComments = value; OnPropertyChanged(); } }
        public bool ShowSigner { get { return _showSigner; } set { _showSigner = value; OnPropertyChanged(); } }
        public bool DisableNotifications { get { return _disableNotifications; } set { _disableNotifications = value; OnPropertyChanged(); } }

        public RelayCommand AttachPhotoCommand { get { return _attachPhotoCommand; } set { _attachPhotoCommand = value; OnPropertyChanged(); } }
        public RelayCommand AttachVideoCommand { get { return _attachVideoCommand; } set { _attachVideoCommand = value; OnPropertyChanged(); } }
        public RelayCommand AttachFileCommand { get { return _attachFileCommand; } set { _attachFileCommand = value; OnPropertyChanged(); } }
        public RelayCommand AttachAudioCommand { get { return _attachAudioCommand; } set { _attachAudioCommand = value; OnPropertyChanged(); } }
        public RelayCommand AttachPollCommand { get { return _attachPollCommand; } set { _attachPollCommand = value; OnPropertyChanged(); } }
        public RelayCommand AttachLocationCommand { get { return _attachLocationCommand; } set { _attachLocationCommand = value; OnPropertyChanged(); } }
        public RelayCommand RemoveLocationCommand { get { return _removeLocationCommand; } set { _removeLocationCommand = value; OnPropertyChanged(); } }
        public RelayCommand RemovePublishTimeCommand { get { return _removePublishTimeCommand; } set { _removePublishTimeCommand = value; OnPropertyChanged(); } }
        public RelayCommand PublishCommand { get { return _publishCommand; } set { _publishCommand = value; OnPropertyChanged(); } }

        #region Availability etc.

        public long WallOwnerId { get; private set; }
        public bool IsSuggestionMode { get; private set; }
        public bool HasAttachments { get { return _attachments.Count > 0; } }
        public bool HasPreviewableAttachments { get { return _attachments.Any(a => a.Attachment is IPreview); } }
        public bool CanPublishPostponed { get; private set; }
        public bool IsPrivacyAvailable { get; private set; }
        public bool CanPublishFromUserInGroup { get; private set; }
        public bool CanPublishOnlyForDons { get; private set; }
        public bool CanDisableComments { get; private set; }
        public bool CanPostWithSigner { get; private set; }
        public bool CanDisableNotifications { get; private set; }
        public bool SettingsAvailable => CanPublishPostponed || CanPublishOnlyForDons || IsPrivacyAvailable || CanPublishFromUserInGroup || CanDisableComments || CanPostWithSigner || CanDisableNotifications;

        #endregion

        System.Action onPostPublished;
        string guid = "";

        public PostComposerViewModel(long wallOwnerId, GroupType? groupType = null, bool canPostOnGroupWallAsUser = false, bool canSuggest = false, bool donutAvailable = false, System.Action onPostPublished = null) {
            WallOwnerId = wallOwnerId;
            guid = Guid.NewGuid().ToString();
            this.onPostPublished = onPostPublished;
            PostButtonText = Locale.Get("feed_composer_publish_button");

            if (wallOwnerId.IsUser()) {
                if (wallOwnerId == AppParameters.UserID) {
                    CanPublishPostponed = true;
                    IsPrivacyAvailable = true;
                    CanDisableComments = true;
                    CanDisableNotifications = true;
                    PlaceholderText = Locale.Get("feed_composer_placeholder");
                } else {
                    User u = AppSession.GetCachedUser(wallOwnerId);
                    if (u != null && !string.IsNullOrEmpty(u.FirstNameGen)) {
                        PlaceholderText = String.Format(Locale.GetForFormat("feed_composer_placeholder_user_wall"), u.FirstNameGen);
                    } else {
                        PlaceholderText = Locale.Get("feed_composer_placeholder");
                    }
                }
            } else if (wallOwnerId.IsGroup() && groupType.HasValue) {
                if (canSuggest) {
                    IsSuggestionMode = true;
                    CanPostWithSigner = true;
                    PlaceholderText = Locale.Get("feed_composer_placeholder_group_suggest");
                    PostButtonText = Locale.Get("feed_composer_suggest_button");
                } else {
                    CanPublishFromUserInGroup = canPostOnGroupWallAsUser;
                    CanPublishOnlyForDons = donutAvailable;
                    CanPublishPostponed = true;
                    CanDisableComments = true;
                    CanPostWithSigner = true;
                    CanDisableNotifications = true;
                    PlaceholderText = Locale.Get("feed_composer_placeholder");
                }
            }

            Attachments.CollectionChanged += (a, b) => {
                OnPropertyChanged(nameof(HasAttachments));
                OnPropertyChanged(nameof(HasPreviewableAttachments));
            };

            AttachPhotoCommand = new RelayCommand(o => OpenAttachmentPicker(0));
            AttachVideoCommand = new RelayCommand(o => OpenAttachmentPicker(1));
            AttachFileCommand = new RelayCommand(o => OpenAttachmentPicker(2));
            AttachAudioCommand = new RelayCommand(o => OpenAttachmentPicker(3));
            AttachPollCommand = new RelayCommand(o => OpenPollEditor());
            AttachLocationCommand = new RelayCommand(o => OpenLocationPicker());
            RemoveLocationCommand = new RelayCommand(o => Geopoint = null);
            RemovePublishTimeCommand = new RelayCommand(o => PublishTime = null);
            PublishCommand = new RelayCommand(o => Publish());
        }

        private void OpenAttachmentPicker(int tab) {
            if (Attachments.Count == 10) return;
            int limit = 10 - Attachments.Count;
            AttachmentPicker picker = new AttachmentPicker(limit, tab);
            picker.Closed += async (a, b) => {
                if (b is AttachmentPickerResult result) {
                    switch (result.Type) {
                        case AttachmentPickerResultType.Attachments:
                            foreach (var attachment in result.Attachments) {
                                OutboundAttachmentViewModel oavm = new OutboundAttachmentViewModel(attachment);
                                Attachments.Add(oavm);
                            }
                            break;
                        case AttachmentPickerResultType.PhotoFiles:
                            await AttachFilesAndUpload(result.Files, OutboundAttachmentUploadFileType.PhotoForWall);
                            break;
                        case AttachmentPickerResultType.VideoFiles:
                            await AttachFilesAndUpload(result.Files, OutboundAttachmentUploadFileType.VideoForWall);
                            break;
                        case AttachmentPickerResultType.Files:
                            await AttachFilesAndUpload(result.Files, OutboundAttachmentUploadFileType.DocumentForWall);
                            break;
                    }
                }
            };
            picker.Show();
        }

        public async Task AttachFilesAndUpload(IEnumerable<StorageFile> files, OutboundAttachmentUploadFileType type) {
            int start = Attachments.Count;
            foreach (var file in files) {
                OutboundAttachmentViewModel oavm = OutboundAttachmentViewModel.CreateFromFile(WallOwnerId, file, type, true);
                Attachments.Add(oavm);
            }
            for (int pos = start; pos < Attachments.Count; pos++) {
                await Attachments[pos].DoUpload();
                await Task.Delay(500);
            }
        }

        private void OpenPollEditor() {
            if (Attachments.Count == 10) return;
            PollEditor pe = new PollEditor();
            pe.Title = Locale.Get("polleditor_create");
            pe.Closed += (c, d) => {
                if (d != null && d is Poll poll) Attachments.Add(new OutboundAttachmentViewModel(poll));
            };
            pe.Show();
        }

        private void OpenLocationPicker() {
            PlacePicker pp = new PlacePicker();
            pp.Title = Locale.Get("botbtn_position");
            pp.Closed += (c, d) => {
                if (d != null && d is Geopoint point) Geopoint = point;
            };
            pp.Show();
        }

        private int? GetNormalizedDonutPaidParameter(int value) {
            switch (value) {
                case 0:
                    return -1;
                case 1:
                    return 86400;
                case 2:
                    return 172800;
                case 3:
                    return 259200;
                case 4:
                    return 345600;
                case 5:
                    return 432000;
                case 6:
                    return 518400;
                case 7:
                    return 604800;
                default:
                    return null;
            }
        }

        private async Task Publish() {
            bool friendsOnly = IsPrivacyAvailable && Privacy == 1;
            bool bestFriendsOnly = IsPrivacyAvailable && Privacy == 2;
            bool fromGroup = WallOwnerId.IsGroup() && CanPublishFromUserInGroup && PostAsGroup;
            if (WallOwnerId.IsGroup() && !CanPublishFromUserInGroup) fromGroup = true;
            // if (IsSuggestionMode) fromGroup = false;

            if (string.IsNullOrEmpty(Text) && Attachments.Count == 0) {
                return;
            }

            var unsuccessfulUploads = Attachments.Where(a => a.UploadState != OutboundAttachmentUploadState.Success).Select(a => a.DisplayName).ToList();
            if (unsuccessfulUploads.Count > 0) {
                string desc = unsuccessfulUploads.Count == 1 ?
                    String.Format(Locale.GetForFormat("sharetarget_err_upload_desc_single"), unsuccessfulUploads[0]) :
                    String.Format(Locale.GetForFormat("sharetarget_err_upload_desc_multi"), String.Join("\n", unsuccessfulUploads));
                await new ContentDialog {
                    Title = Locale.Get("sharetarget_err_upload_title"),
                    Content = desc,
                    PrimaryButtonText = Locale.Get("close")
                }.ShowAsync();
                return;
            }

            if (PublishTime.HasValue) {
                var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                var pub = new DateTimeOffset(PublishTime.Value).ToUnixTimeSeconds();
                if (pub <= now) {
                    await new ContentDialog {
                        Title = Locale.Get("global_error"),
                        Content = Locale.Get("feed_composer_invalid_time"),
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                    return;
                }
            }

            string message = !string.IsNullOrEmpty(Text) ? Text.Replace("\r\n", "\r").Replace("\r", "\r\n").Trim() : null;
            string attachments = String.Join(",", Attachments);
            bool signed = WallOwnerId.IsGroup() && ShowSigner;
            bool checkSign = IsSuggestionMode && !signed;
            long publishDate = CanPublishPostponed && PublishTime.HasValue ? new DateTimeOffset(PublishTime.Value).ToUnixTimeSeconds() : 0;

            double glat = 0;
            double glong = 0;
            if (Geopoint != null) {
                glat = Geopoint.Position.Latitude;
                glong = Geopoint.Position.Longitude;
            }

            bool closeComments = CanDisableComments && DisableComments;
            bool muteNotifications = CanDisableNotifications && DisableNotifications;
            WallAttachmentsPrimaryMode attachmentsMode = PrimaryAttachmentsMode == 1 ? WallAttachmentsPrimaryMode.Carousel : WallAttachmentsPrimaryMode.Grid;
            int? donutPaidDuration = CanPublishOnlyForDons && DonSetting == 1 ? GetNormalizedDonutPaidParameter(PublishToNonDonsAfterDays) : null;

            if (WallOwnerId.IsGroup() && !fromGroup) {
                closeComments = false;
                muteNotifications = false;
            }

            if (AppParameters.FeedDebug) {
                string test = "";
                test += $"from_group: {fromGroup}\n";
                test += $"friends_only: {friendsOnly}\n";
                test += $"best_friends_only: {bestFriendsOnly}\n";
                test += $"message: {message}\n";
                test += $"attachments: {attachments}\n";
                test += $"signed: {signed}\n";
                test += $"check_sign: {checkSign}\n";
                test += $"publish_date: {publishDate}\n";
                test += $"lat: {glat}\n";
                test += $"long: {glong}\n";
                test += $"guid: {guid}\n";
                test += $"close_comments: {closeComments}\n";
                test += $"mute_notifications: {muteNotifications}\n";
                test += $"attachments_primary_mode: {attachmentsMode}\n";
                if (donutPaidDuration.HasValue) test += $"donut_paid_duration: {donutPaidDuration.Value}\n";

                await new MessageDialog(test, "wall.post").ShowAsync();

                ScreenSpinner<object> sspp = new ScreenSpinner<object>();
                var responsePreview = await sspp.ShowAsync(Wall.GetPostPreview(WallOwnerId, friendsOnly, bestFriendsOnly, fromGroup, message, attachments, signed, checkSign, publishDate, glong, glat, donutPaidDuration));
                if (responsePreview is WallPostPreviewResponse preview) {
                    WallPostModal modal = new WallPostModal(preview.Post);
                    modal.Show();
                } else {
                    Functions.ShowHandledErrorDialog(responsePreview);
                }

                return;
            }

            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            var response = await ssp.ShowAsync(Wall.Post(WallOwnerId, guid, friendsOnly, bestFriendsOnly, fromGroup, message, attachments, signed, checkSign, closeComments, muteNotifications, publishDate, glong, glat, null, donutPaidDuration, attachmentsMode));
            if (response is WallPostResponse resp) {
                guid = Guid.NewGuid().ToString();
                Text = null;
                Attachments.Clear();
                Geopoint = null;
                if (IsSuggestionMode) {
                }

                if (PublishTime != null) {
                    await new ContentDialog {
                        Title = String.Format(Locale.GetForFormat("feed_composer_postponed_created"), PublishTime.Value.ToTimeAndDate()),
                        Content = Locale.Get("feed_composer_postponed_created_desc"),
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                }
                PublishTime = null;

                onPostPublished?.Invoke();
            } else {
                Functions.ShowHandledErrorDialog(response);
            }
        }
    }
}
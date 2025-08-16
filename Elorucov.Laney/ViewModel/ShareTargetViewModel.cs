using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.ListHelpers;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Ported from L2.

namespace Elorucov.Laney.ViewModel {
    public class ShareTargetViewModel : BaseViewModel {
        private bool _isLoading;
        private string _errorInfo;
        private ThreadSafeObservableCollection<LConversation> _conversations = new ThreadSafeObservableCollection<LConversation>();
        private string _messageText;
        private ThreadSafeObservableCollection<OutboundAttachmentViewModel> _attachments;
        private bool _hasAttachments;
        private string _searchQuery;
        private ThreadSafeObservableCollection<Entity> _selectedPeers = new ThreadSafeObservableCollection<Entity>();
        private bool _isSending;
        private bool _isNotEmpty;
        private bool _isSuccess;
        private bool _isSetectedPeersIsEmpty;

        private RelayCommand _sendCommand;
        private RelayCommand _tryAgainCommand;

        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; OnPropertyChanged(); } }
        public string ErrorInfo { get { return _errorInfo; } private set { _errorInfo = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<LConversation> Conversations { get { return _conversations; } private set { _conversations = value; OnPropertyChanged(); } }
        public string MessageText { get { return _messageText; } set { _messageText = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<OutboundAttachmentViewModel> Attachments { get { return _attachments; } private set { _attachments = value; OnPropertyChanged(); } }
        public bool HasAttachments { get { return _hasAttachments; } private set { _hasAttachments = value; OnPropertyChanged("HasAttachments"); } }
        public string SearchQuery { get { return _searchQuery; } set { _searchQuery = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<Entity> SelectedPeers { get { return _selectedPeers; } private set { _selectedPeers = value; OnPropertyChanged(); } }
        public bool IsSending { get { return _isSending; } private set { _isSending = value; OnPropertyChanged(); } }
        public bool IsNotEmpty { get { return _isNotEmpty; } private set { _isNotEmpty = value; OnPropertyChanged(); } }
        public bool IsSuccess { get { return _isSuccess; } private set { _isSuccess = value; OnPropertyChanged(); } }
        public bool IsSelectedPeersIsEmpty { get { return _isSetectedPeersIsEmpty; } private set { _isSetectedPeersIsEmpty = value; OnPropertyChanged(); } }

        public RelayCommand SendCommand { get { return _sendCommand; } private set { _sendCommand = value; OnPropertyChanged(); } }
        public RelayCommand TryAgainCommand { get { return _tryAgainCommand; } private set { _tryAgainCommand = value; OnPropertyChanged(); } }

        private ShareOperation shareOperation;
        FrameworkElement root = Window.Current.Content as FrameworkElement;

        public ShareTargetViewModel(ShareOperation shareOp) {
            PropertyChanged += async (a, b) => {
                switch (b.PropertyName) {
                    case nameof(Attachments):
                        HasAttachments = Attachments.Count > 0;
                        CheckIsNotEmpty();
                        break;
                    case nameof(SearchQuery):
                        if (SearchQuery.Length == 0) await LoadConversationsAsync();
                        break;
                    case nameof(MessageText):
                        CheckIsNotEmpty();
                        break;
                }
            };
            SelectedPeers.CollectionChanged += (a, b) => {
                UpdateSelectedPeersInfo();
            };
            UpdateSelectedPeersInfo();

            shareOperation = shareOp;
            new System.Action(async () => { await DoMagicWithSharedDataAsync(shareOp.Data); })();

            SendCommand = new RelayCommand(async o => {
                if (!IsNotEmpty) return;
                IsSending = true;
                root.IsHitTestVisible = false;
                await PrepareToSendingMessageAsync();
            });

            new System.Action(async () => { await LoadConversationsAsync(); })();
        }

        private async Task DoMagicWithSharedDataAsync(DataPackageView data) {
            if (data.Contains(StandardDataFormats.Text)) {
                MessageText = await data.GetTextAsync();
            }
            if (data.Contains(StandardDataFormats.WebLink)) {
                if (!string.IsNullOrEmpty(MessageText)) MessageText += "\n";
                MessageText += await data.GetWebLinkAsync();
            }

            var attachments = await OutboundAttachmentViewModel.CreateFromDataPackageView(0, data, dontUploadImmediately: true);
            Attachments = new ThreadSafeObservableCollection<OutboundAttachmentViewModel>(attachments);
            Attachments.CollectionChanged += (a, b) => {
                HasAttachments = Attachments.Count > 0;
            };
        }

        private void UpdateSelectedPeersInfo() {
            IsSelectedPeersIsEmpty = SelectedPeers.Count == 0;
            CheckIsNotEmpty();
        }

        private void CheckIsNotEmpty() {
            IsNotEmpty = Attachments?.Count > 0 || !string.IsNullOrEmpty(MessageText);
            if (SelectedPeers.Count == 0) IsNotEmpty = false;
        }

        private async Task LoadConversationsAsync() {
            Conversations.Clear();
            IsLoading = true;
            ErrorInfo = null;

            var response = await Messages.GetConversations(100);
            if (response is ConversationsResponse cr) {
                AppSession.AddUsersToCache(cr.Profiles);
                AppSession.AddGroupsToCache(cr.Groups);

                foreach (var convitem in cr.Items) {
                    if (convitem.Conversation.CanWrite.Allowed) Conversations.Add(new LConversation(convitem.Conversation));
                }
            } else {
                var err = Functions.GetNormalErrorInfo(response);
                ErrorInfo = $"{err.Item1}\n{err.Item2}";
                TryAgainCommand = new RelayCommand(async o => await LoadConversationsAsync());
            }

            IsLoading = false;
        }

        public async Task SearchConversationsAsync() {
            Conversations.Clear();
            IsLoading = true;

            var response = await Messages.SearchConversations(SearchQuery);
            if (response is VKList<Conversation> cr) {
                AppSession.AddUsersToCache(cr.Profiles);
                AppSession.AddGroupsToCache(cr.Groups);

                foreach (var convitem in cr.Items) {
                    if (convitem.CanWrite.Allowed) Conversations.Add(new LConversation(convitem));
                }
            } else {
                var err = Functions.GetNormalErrorInfo(response);
                ErrorInfo = $"{err.Item1}\n{err.Item2}";
                TryAgainCommand = new RelayCommand(async o => await SearchConversationsAsync());
            }

            IsLoading = false;
        }

        private async Task PrepareToSendingMessageAsync() {
            List<AttachmentBase> uploadedAttachments = new List<AttachmentBase>();
            List<string> unsuccessfulUploadFileNames = new List<string>();
            List<long> peers = SelectedPeers.Select(p => p.Id).ToList();

            foreach (var attachment in Attachments) {
                if (attachment.UploadState == OutboundAttachmentUploadState.Success) {
                    uploadedAttachments.Add(attachment.Attachment);
                } else if (attachment.UploadState == OutboundAttachmentUploadState.Failed) {
                    unsuccessfulUploadFileNames.Add(attachment.DisplayName);
                } else {
                    bool result = await attachment.DoUpload();
                    if (!result) {
                        unsuccessfulUploadFileNames.Add(attachment.DisplayName);
                    } else {
                        uploadedAttachments.Add(attachment.Attachment);
                    }
                }
            }
            int unsuccessfulUploads = unsuccessfulUploadFileNames.Count;
            if (unsuccessfulUploads == Attachments.Count && string.IsNullOrEmpty(MessageText)) {
                IsSending = false;
                root.IsHitTestVisible = true;
                await new ContentDialog {
                    Title = Locale.Get("sharetarget_err_upload_nomsg_title"),
                    Content = Locale.Get("sharetarget_err_upload_nomsg_desc"),
                    PrimaryButtonText = Locale.Get("close")
                }.ShowAsync();
            } else if (unsuccessfulUploads > 0) {
                string desc = unsuccessfulUploads == 1 ?
                    String.Format(Locale.GetForFormat("sharetarget_err_upload_desc_single"), unsuccessfulUploadFileNames[0]) :
                    String.Format(Locale.GetForFormat("sharetarget_err_upload_desc_multi"), String.Join("\n", unsuccessfulUploadFileNames));
                ContentDialog dlg = new ContentDialog {
                    Title = Locale.Get("sharetarget_err_upload_title"),
                    Content = desc,
                    PrimaryButtonText = Locale.Get("yes"),
                    SecondaryButtonText = Locale.Get("no")
                };
                var result = await dlg.ShowAsync();
                if (result == ContentDialogResult.Primary) {
                    await FinalizeSendingAsync(peers, MessageText, uploadedAttachments);
                } else {
                    IsSending = false;
                    root.IsHitTestVisible = true;
                }
            } else {
                await FinalizeSendingAsync(peers, MessageText, uploadedAttachments);
            }
        }

        private async Task FinalizeSendingAsync(List<long> peers, string message, List<AttachmentBase> attachments) {
            var attachmentsList = attachments.Select(a => a.ToString()).ToList();
            object response = await Execute.MultiSend(peers, message, attachmentsList);
            if (response is List<MultiSendResult> result) {
                List<int> successPeers = result.Select(r => r.PeerId).ToList();
                int failedPeersCount = 0;
                foreach (int peer in peers) {
                    if (!successPeers.Contains(peer)) failedPeersCount++;
                }

                IsSending = false;
                IsSuccess = true;

                if (failedPeersCount > 0) {
                    string desc = string.Empty;
                    if (peers.Count == 1 && failedPeersCount == 1) {
                        long peer = peers[0];
                        string name = string.Empty;
                        if (peer > 0 && peer <= 1000000000) {
                            name = AppSession.GetCachedUser(peer).FirstNameDat;
                            desc = String.Format(Locale.GetForFormat("sharetarget_err_send_user"), name);
                        } else {
                            name = SelectedPeers.Where(p => p.Id == peer).FirstOrDefault().Title;
                            desc = String.Format(Locale.GetForFormat("sharetarget_err_send"), name);
                        }
                    } else {
                        desc = String.Format(Locale.GetForFormat("sharetarget_sent_but_not_all"), String.Join("\n", SelectedPeers.Select(p => p.Title)));
                    }

                    await new ContentDialog {
                        Title = Locale.Get("sharetarget_sent_but_not_all_title"),
                        Content = desc,
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                } else {
                    await Task.Delay(1000);
                }
                shareOperation.ReportCompleted();
            } else {
                IsSending = false;
                root.IsHitTestVisible = true;
                Functions.ShowHandledErrorDialog(response, async () => await FinalizeSendingAsync(peers, message, attachments));
            }
        }
    }
}
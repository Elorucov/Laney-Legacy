using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels.Controls;
using Elorucov.Laney.VKAPIExecute;
using Elorucov.Laney.VKAPIExecute.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;

namespace Elorucov.Laney.ViewModels
{
    public class ShareTargetViewModel : CommonViewModel
    {
        private ThreadSafeObservableCollection<VKSession> _sessions;
        private VKSession _selectedSession;
        private ThreadSafeObservableCollection<ConversationViewModel> _conversations = new ThreadSafeObservableCollection<ConversationViewModel>();
        private string _messageText;
        private ThreadSafeObservableCollection<OutboundAttachmentViewModel> _attachments;
        private bool _hasAttachments;
        private string _searchQuery;
        private ThreadSafeObservableCollection<Entity> _selectedPeers = new ThreadSafeObservableCollection<Entity>();
        private string _selectedPeersInfo;
        private bool _isSending;
        private bool _isNotEmpty;
        private bool _isSuccess;

        private RelayCommand _sendCommand;

        public ThreadSafeObservableCollection<VKSession> Sessions { get { return _sessions; } private set { _sessions = value; OnPropertyChanged(); } }
        public VKSession SelectedSession { get { return _selectedSession; } set { _selectedSession = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<ConversationViewModel> Conversations { get { return _conversations; } private set { _conversations = value; OnPropertyChanged(); } }
        public string MessageText { get { return _messageText; } set { _messageText = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<OutboundAttachmentViewModel> Attachments { get { return _attachments; } private set { _attachments = value; OnPropertyChanged(); } }
        public bool HasAttachments { get { return _hasAttachments; } private set { _hasAttachments = value; OnPropertyChanged("HasAttachments"); } }
        public string SearchQuery { get { return _searchQuery; } set { _searchQuery = value; OnPropertyChanged(); } }
        public ThreadSafeObservableCollection<Entity> SelectedPeers { get { return _selectedPeers; } private set { _selectedPeers = value; OnPropertyChanged(); } }
        public string SelectedPeersInfo { get { return _selectedPeersInfo; } private set { _selectedPeersInfo = value; OnPropertyChanged(); } }
        public bool IsSending { get { return _isSending; } private set { _isSending = value; OnPropertyChanged(); } }
        public bool IsNotEmpty { get { return _isNotEmpty; } private set { _isNotEmpty = value; OnPropertyChanged(); } }
        public bool IsSuccess { get { return _isSuccess; } private set { _isSuccess = value; OnPropertyChanged(); } }

        public RelayCommand SendCommand { get { return _sendCommand; } private set { _sendCommand = value; OnPropertyChanged(); } }

        private ShareOperation shareOperation;
        public ShareTargetViewModel(ShareOperation shareOp, List<VKSession> sessions)
        {
            PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(SelectedSession):
                        SelectedPeers.Clear();
                        SelectedSession.StartSession();
                        LoadConversations();
                        break;
                    case nameof(Attachments):
                        HasAttachments = Attachments.Count > 0;
                        CheckIsNotEmpty();
                        break;
                    case nameof(SearchQuery):
                        if (SearchQuery.Length == 0) LoadConversations();
                        break;
                    case nameof(MessageText):
                        CheckIsNotEmpty();
                        break;
                }
            };
            SelectedPeers.CollectionChanged += (a, b) =>
            {
                UpdateSelectedPeersInfo();
            };
            UpdateSelectedPeersInfo();

            shareOperation = shareOp;
            Sessions = new ThreadSafeObservableCollection<VKSession>(sessions);
            SelectedSession = Sessions.FirstOrDefault();
            DoMagicWithSharedData(shareOp.Data);

            SendCommand = new RelayCommand(o =>
            {
                if (!IsNotEmpty) return;
                IsSending = true;
                ViewManagement.DisableFocusForCurrentWindow();
                PrepareToSendingMessage();
            });
        }

        private async void DoMagicWithSharedData(DataPackageView data)
        {
            // Set text
            if (data.Contains(StandardDataFormats.Text))
            {
                MessageText = await data.GetTextAsync();
            }
            if (data.Contains(StandardDataFormats.WebLink))
            {
                if (!String.IsNullOrEmpty(MessageText)) MessageText += "\n";
                MessageText += await data.GetWebLinkAsync();
            }

            var attachments = await OutboundAttachmentViewModel.CreateFromDataPackageView(0, data, dontUploadImmediately: true);
            Attachments = new ThreadSafeObservableCollection<OutboundAttachmentViewModel>(attachments);
            Attachments.CollectionChanged += (a, b) =>
            {
                HasAttachments = Attachments.Count > 0;
            };
        }

        private void UpdateSelectedPeersInfo()
        {
            SelectedPeersInfo = $"{Locale.Get("sharetarget_selected")} ({SelectedPeers.Count}/10)";
            CheckIsNotEmpty();
        }

        private void CheckIsNotEmpty()
        {
            IsNotEmpty = Attachments?.Count > 0 || !String.IsNullOrEmpty(MessageText);
            if (SelectedPeers.Count == 0) IsNotEmpty = false;
        }

        private async void LoadConversations()
        {
            Conversations.Clear();
            IsLoading = true;
            try
            {
                var response = await SelectedSession.API.Messages.GetConversationsAsync(SelectedSession.GroupId, APIHelper.Fields, ELOR.VKAPILib.Methods.ConversationsFilter.All, true, 200);
                CacheManager.Add(response.Profiles);
                CacheManager.Add(response.Groups);

                foreach (var convitem in response.Items)
                {
                    if (convitem.Conversation.CanWrite.Allowed) Conversations.Add(new ConversationViewModel(convitem.Conversation, true));
                }
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => LoadConversations());
            }
            IsLoading = false;
        }

        public async void SearchConversations()
        {
            Conversations.Clear();
            IsLoading = true;
            try
            {
                var response = await SelectedSession.API.Messages.SearchConversationsAsync(SelectedSession.GroupId, SearchQuery, 100, true, APIHelper.Fields);
                CacheManager.Add(response.Profiles);
                CacheManager.Add(response.Groups);

                foreach (var conv in response.Items)
                {
                    if (conv.CanWrite.Allowed) Conversations.Add(new ConversationViewModel(conv, true));
                }
            }
            catch (Exception ex)
            {
                Placeholder = PlaceholderViewModel.GetForException(ex, () => LoadConversations());
            }
            IsLoading = false;
        }

        private async void PrepareToSendingMessage()
        {
            List<AttachmentBase> uploadedAttachments = new List<AttachmentBase>();
            List<string> unsuccessfulUploadFileNames = new List<string>();
            List<int> peers = SelectedPeers.Select(p => p.Id).ToList();

            foreach (var attachment in Attachments)
            {
                if (attachment.UploadState == OutboundAttachmentUploadState.Success)
                {
                    uploadedAttachments.Add(attachment.Attachment);
                }
                else if (attachment.UploadState == OutboundAttachmentUploadState.Failed)
                {
                    unsuccessfulUploadFileNames.Add(attachment.DisplayName);
                }
                else
                {
                    bool result = await attachment.DoUpload(SelectedSession.API, SelectedSession.GroupId);
                    if (!result)
                    {
                        unsuccessfulUploadFileNames.Add(attachment.DisplayName);
                    }
                    else
                    {
                        uploadedAttachments.Add(attachment.Attachment);
                    }
                }
            }
            int unsuccessfulUploads = unsuccessfulUploadFileNames.Count;
            if (unsuccessfulUploads == Attachments.Count && String.IsNullOrEmpty(MessageText))
            {
                IsSending = false;
                ViewManagement.EnableFocusForCurrentWindow();
                await new Alert
                {
                    Header = Locale.Get("sharetarget_err_upload_nomsg_title"),
                    Text = Locale.Get("sharetarget_err_upload_nomsg_desc"),
                    PrimaryButtonText = Locale.Get("close")
                }.ShowAsync();
            }
            else if (unsuccessfulUploads > 0)
            {
                string desc = unsuccessfulUploads == 1 ?
                    String.Format(Locale.GetForFormat("sharetarget_err_upload_desc_single"), unsuccessfulUploadFileNames[0]) :
                    String.Format(Locale.GetForFormat("sharetarget_err_upload_desc_multi"), String.Join("\n", unsuccessfulUploadFileNames));
                Alert alert = new Alert
                {
                    Header = Locale.Get("sharetarget_err_upload_title"),
                    Text = desc,
                    PrimaryButtonText = Locale.Get("yes"),
                    SecondaryButtonText = Locale.Get("no")
                };
                AlertButton result = await alert.ShowAsync();
                if (result == AlertButton.Primary)
                {
                    FinalizeSending(peers, MessageText, uploadedAttachments);
                }
                else
                {
                    IsSending = false;
                    ViewManagement.EnableFocusForCurrentWindow();
                }
            }
            else
            {
                FinalizeSending(peers, MessageText, uploadedAttachments);
            }
        }

        private async void FinalizeSending(List<int> peers, string message, List<AttachmentBase> attachments)
        {
            try
            {
                Execute execute = SelectedSession.API.Execute as Execute;
                List<MultiSendResult> result = await execute.MultiSendAsync(SelectedSession.API, peers, message, attachments.Select(a => a.ToString()).ToList());

                List<int> successPeers = result.Select(r => r.PeerId).ToList();
                int failedPeersCount = 0;
                foreach (int peer in peers)
                {
                    if (!successPeers.Contains(peer)) failedPeersCount++;
                }

                IsSending = false;
                IsSuccess = true;

                if (failedPeersCount > 0)
                {
                    string desc = String.Empty;
                    if (peers.Count == 1 && failedPeersCount == 1)
                    {
                        int peer = peers[0];
                        string name = String.Empty;
                        if (peer > 0 && peer <= 1000000000)
                        {
                            name = CacheManager.GetUser(peer).FirstNameDat;
                            desc = String.Format(Locale.GetForFormat("sharetarget_err_send_user"), name);
                        }
                        else
                        {
                            name = SelectedPeers.Where(p => p.Id == peer).FirstOrDefault().Title;
                            desc = String.Format(Locale.GetForFormat("sharetarget_err_send"), name);
                        }
                    }
                    else
                    {
                        desc = String.Format(Locale.GetForFormat("sharetarget_sent_but_not_all"), String.Join("\n", SelectedPeers.Select(p => p.Title)));
                    }

                    await new Alert
                    {
                        Header = Locale.Get("sharetarget_sent_but_not_all_title"),
                        Text = desc,
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                }
                else
                {
                    await Task.Delay(1000);
                }
                shareOperation.ReportCompleted();
            }
            catch (Exception ex)
            {
                IsSending = false;
                if (await ExceptionHelper.ShowErrorDialogAsync(ex))
                {
                    FinalizeSending(peers, message, attachments);
                }
                else
                {
                    ViewManagement.EnableFocusForCurrentWindow();
                }
            }
        }
    }
}
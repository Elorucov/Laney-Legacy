using ELOR.VKAPILib.Methods;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class ConversationAttachmentsViewModel : BaseViewModel
    {
        public class ConversationAttachmentsTabViewModel : ItemsViewModel<ConversationAttachment>
        {
            public int StartFrom { get; set; } = 0;
            public bool End { get; set; } = false;
        }

        private ConversationAttachmentsTabViewModel _photos = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _videos = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _audios = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _documents = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _share = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _graffities = new ConversationAttachmentsTabViewModel();
        private ConversationAttachmentsTabViewModel _audioMessages = new ConversationAttachmentsTabViewModel();

        public ConversationAttachmentsTabViewModel Photos { get { return _photos; } set { _photos = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel Videos { get { return _videos; } set { _videos = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel Audios { get { return _audios; } set { _audios = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel Documents { get { return _documents; } set { _documents = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel Share { get { return _share; } set { _share = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel Graffities { get { return _graffities; } set { _graffities = value; OnPropertyChanged(); } }
        public ConversationAttachmentsTabViewModel AudioMessages { get { return _audioMessages; } set { _audioMessages = value; OnPropertyChanged(); } }

        private int PeerId;

        public ConversationAttachmentsViewModel(int peerId)
        {
            PeerId = peerId;
        }

        public void LoadPhotos()
        {
            LoadVM(Photos, HistoryAttachmentMediaType.Photo);
        }

        public void LoadVideos()
        {
            LoadVM(Videos, HistoryAttachmentMediaType.Video);
        }

        public void LoadAudios()
        {
            LoadVM(Audios, HistoryAttachmentMediaType.Audio);
        }

        public void LoadDocs()
        {
            LoadVM(Documents, HistoryAttachmentMediaType.Doc);
        }

        public void LoadLinks()
        {
            LoadVM(Share, HistoryAttachmentMediaType.Share);
        }

        public void LoadGraffities()
        {
            LoadVM(Graffities, HistoryAttachmentMediaType.Graffiti);
        }

        public void LoadAudioMessages()
        {
            LoadVM(AudioMessages, HistoryAttachmentMediaType.AudioMessage);
        }

        #region Private methods

        private async void LoadVM(ConversationAttachmentsTabViewModel ivm, HistoryAttachmentMediaType type)
        {
            if (ivm.IsLoading || ivm.End) return;
            ivm.Placeholder = null;
            ivm.IsLoading = true;
            try
            {
                ConversationAttachmentsResponse resp = await VKSession.Current.API.Messages.GetHistoryAttachmentsAsync(VKSession.Current.GroupId, PeerId, type, ivm.StartFrom, 60, true);
                resp.Items.ForEach(i => ivm.Items.Add(i));
                if (!String.IsNullOrEmpty(resp.NextFrom))
                {
                    ivm.StartFrom = Int32.Parse(resp.NextFrom.Split('/')[0]) - 1;
                }
                else
                {
                    if (ivm.Items.Count > 0) ivm.End = true;
                }
            }
            catch (Exception ex)
            {
                if (ivm.Items.Count == 0)
                {
                    ivm.Placeholder = PlaceholderViewModel.GetForException(ex, () => LoadVM(ivm, type));
                }
                else
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex)) LoadVM(ivm, type);
                }
            }
            ivm.IsLoading = false;
        }

        #endregion

    }
}

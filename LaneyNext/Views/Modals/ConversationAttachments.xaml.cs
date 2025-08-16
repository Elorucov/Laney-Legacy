using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ConversationAttachments : Modal
    {
        public ConversationAttachments(int peerId, string conversationName)
        {
            this.InitializeComponent();
            Title = $"{Locale.Get("attachments")} — {conversationName}";
            DataContext = new ConversationAttachmentsViewModel(peerId);
        }
        private ConversationAttachmentsViewModel ViewModel { get { return DataContext as ConversationAttachmentsViewModel; } }

        private void LoadPivotContents(object sender, SelectionChangedEventArgs e)
        {
            Pivot p = sender as Pivot;
            switch (p.SelectedIndex)
            {
                case 0:
                    if (ViewModel.Photos.Items.Count == 0) ViewModel.LoadPhotos();
                    RegisterScrollViewChangedEvent(PhotosGrid, () => ViewModel.LoadPhotos());
                    break;
                case 1:
                    if (ViewModel.Videos.Items.Count == 0) ViewModel.LoadVideos();
                    RegisterScrollViewChangedEvent(VideosGrid, () => ViewModel.LoadVideos());
                    break;
                case 2:
                    if (ViewModel.Audios.Items.Count == 0) ViewModel.LoadAudios();
                    RegisterScrollViewChangedEvent(AudiosList, () => ViewModel.LoadAudios());
                    break;
                case 3:
                    if (ViewModel.Documents.Items.Count == 0) ViewModel.LoadDocs();
                    RegisterScrollViewChangedEvent(DocsList, () => ViewModel.LoadDocs());
                    break;
                case 4:
                    if (ViewModel.Share.Items.Count == 0) ViewModel.LoadLinks();
                    RegisterScrollViewChangedEvent(ShareList, () => ViewModel.LoadLinks());
                    break;
                case 5:
                    if (ViewModel.Graffities.Items.Count == 0) ViewModel.LoadGraffities();
                    RegisterScrollViewChangedEvent(GraffitiesGrid, () => ViewModel.LoadGraffities());
                    break;
                    //case 6: 
                    //    if (ViewModel.AudioMessages.Items.Count == 0) ViewModel.LoadAudioMessages();
                    //    RegisterScrollViewChangedEvent(AudioMessagesList, () => ViewModel.LoadAudioMessages());
                    //    break;
            }
        }
        private void RegisterScrollViewChangedEvent(ListViewBase listViewBase, System.Action p)
        {
            if (listViewBase.Tag is bool q && q) return;
            listViewBase.Loaded += (a, b) =>
            {
                ScrollViewer sv = listViewBase.GetScrollViewer();
                sv.RegisterIncrementalLoadingEvent(p, 16);
            };
            listViewBase.Tag = true;
        }

        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            Hide(e.ClickedItem as ConversationAttachment);
        }
    }
}
using ELOR.VKAPILib.Methods;
using ELOR.VKAPILib.Objects;
using ELOR.VKAPILib.Objects.Messages;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.Helpers.UI;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.ViewModels.Controls;
using Elorucov.Laney.Views.Modals;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls
{
    public sealed partial class MessageInputControl : UserControl
    {
        private ConversationViewModel ViewModel { get { return DataContext as ConversationViewModel; } }

        public MessageInputControl()
        {
            this.InitializeComponent();

            // TextBox keydown event handler
            KeyEventHandler keh = new KeyEventHandler(MessageInputKeyboardEvent);
            MessageText.AddHandler(TextBox.PreviewKeyDownEvent, keh, true);

            // Mentions picker event and show/hide some items for group session
            Loaded += async (a, b) =>
            {
                GraffitiPickerMenuItem.Visibility = VKSession.Current.Type == SessionType.VKGroup ? Visibility.Collapsed : Visibility.Visible;
                TemplatesButton.Visibility = VKSession.Current.Type == SessionType.VKUser ? Visibility.Collapsed : Visibility.Visible;

                await Task.Delay(10);
                MentionsHelper.MentionsPicker.MentionPicked += MentionsPicker_MentionPicked;
            };
        }

        private void ShowAttachmentPicker(object sender, RoutedEventArgs e)
        {
            AttachmentPicker ap = new AttachmentPicker(Int32.Parse((sender as FrameworkElement).Tag.ToString()));
            ap.Show();
        }

        private void ShowPollCreatorModal(object sender, RoutedEventArgs e)
        {
            PollEditor pe = new PollEditor();
            pe.Closed += (a, b) =>
            {
                if (b != null && b is Poll p) ViewModel.MessageInput.Attach(p);
            };
            pe.Show();
        }

        private void ShowPlacePickerModal(object sender, RoutedEventArgs e)
        {
            PlacePicker pp = new PlacePicker();
            pp.Closed += (a, b) =>
            {
                if (b != null && b is OutboundAttachmentViewModel oavm) ViewModel.MessageInput.Attach(oavm);
            };
            pp.Show();
        }

        private void ShowGraffitiPickerModal(object sender, RoutedEventArgs e)
        {
            GraffitiPicker gp = new GraffitiPicker();
            gp.Show();
        }

        private void RemoveOutboundAttachment(object sender, RoutedEventArgs e)
        {
            OutboundAttachmentViewModel oavm = (sender as FrameworkElement).DataContext as OutboundAttachmentViewModel;
            if (oavm.UploadState == OutboundAttachmentUploadState.InProgress) oavm.CancelUpload();
            ViewModel.MessageInput.OutboundAttachments.Remove(oavm);
        }

        private async void BotKeyboardButtonClicked(object sender, BotButtonAction e)
        {
            await MessageKeyboardHelper.DoAction(e, 0, ViewModel.CurrentKeyboard.AuthorId);
        }

        private void DetachReplyMessage(object sender, RoutedEventArgs e)
        {
            ViewModel.MessageInput.ReplyMessage = null;
        }

        private void CancelEditing(object sender, RoutedEventArgs e)
        {
            ViewModel.MessageInput.Clear();
        }

        private void MessageInputKeyboardEvent(object sender, KeyRoutedEventArgs e)
        {
            bool ctrl = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shift = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (Core.Settings.SendMessageViaCtrlEnter)
                {
                    if (ctrl)
                    {
                        e.Handled = true;
                        ViewModel.MessageInput.SendMessage();
                    }
                }
                else
                {
                    if (!shift)
                    {
                        e.Handled = true;
                        ViewModel.MessageInput.SendMessage();
                    }
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Down && String.IsNullOrEmpty(ViewModel.MessageInput.Text) && ViewModel.Messages.Count > 0)
            {
                ViewModel.MessageInput.ReplyMessage = ViewModel.Messages.Last();
            }
            else
            {
                APIHelper.SendActivity(ActivityType.Typing, ViewModel.Id);
            }

            if (ctrl && e.Key == Windows.System.VirtualKey.V)
            {
                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Bitmap) || dataPackageView.Contains(StandardDataFormats.StorageItems))
                {
                    e.Handled = true;
                    ViewModel.MessageInput.AttachFromDataPackageView(dataPackageView);
                }
            }

            if (MentionsHelper.MentionsPicker != null && ViewModel.PeerType == PeerType.Chat &&
                MentionsHelper.MentionsPicker.Visibility == Visibility.Visible &&
                (e.Key == Windows.System.VirtualKey.Up || e.Key == Windows.System.VirtualKey.Down))
            {
                MentionsHelper.MentionsPicker.FocusToList();
            }
        }

        private void ShowAudioRecorder(object sender, RoutedEventArgs e)
        {
            AudioRecorderControl.OnRecordTimeChanged += AudioRecorderControl_OnRecordTimeChanged;
            AudioRecorderControl.Visibility = Visibility.Visible;
            AudioRecorderControl.Show();
            AudioRecorderControl.Focus(FocusState.Programmatic);
        }

        private void AudioRecorded(object sender, StorageFile e)
        {
            AudioRecorderControl.OnRecordTimeChanged -= AudioRecorderControl_OnRecordTimeChanged;
            AudioRecorderControl.Visibility = Visibility.Collapsed;
            if (e != null)
            {
                ViewModel.MessageInput.Attach(OutboundAttachmentViewModel.CreateFromFile(ViewModel.Id, e, OutboundAttachmentUploadFileType.AudioMessage));
            }
        }

        private void AudioRecorderControl_OnRecordTimeChanged(object sender, TimeSpan e)
        {
            APIHelper.SendActivity(ActivityType.Audiomessage, ViewModel.Id);
        }

        // Mentions

        private void CheckMentions(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            if (ViewModel != null && ViewModel.PeerType != PeerType.Chat) return;
            bool k = MentionsHelper.CheckMentions(sender, ViewModel.MembersUsers, ViewModel.MembersGroups);
            if (!k) MentionsHelper.MentionsPicker.Visibility = Visibility.Collapsed;
        }

        private void MentionsPicker_MentionPicked(object sender, Entity e)
        {
            string text = MessageText.Text;
            string mentiontemp = (sender as MentionsPicker).SearchDomain;
            int startIndex = text.LastIndexOf(mentiontemp);
            string mention = $"@{e.Subtitle}";
            text = text.Remove(startIndex, Math.Max(mentiontemp.Length, 1)).Insert(startIndex, mention);
            MessageText.Text = text;
            MessageText.Focus(FocusState.Keyboard);
            MessageText.SelectionStart = MessageText.Text.Length;
        }

        private void TemplatesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            MessageTemplate template = e.ClickedItem as MessageTemplate;
            ViewModel.MessageInput.Text = template.Text;
            TemplatesFlyout.Hide();
        }

        private void ChangeTemplate(object sender, RoutedEventArgs e)
        {
            MessageTemplate template = (sender as FrameworkElement).DataContext as MessageTemplate;
            ViewModel.MessageInput.AddTemplateCommand.Execute(template);
        }

        private void DeleteTemplate(object sender, RoutedEventArgs e)
        {
            MessageTemplate template = (sender as FrameworkElement).DataContext as MessageTemplate;
            ViewModel.MessageInput.DeleteTemplateCommand.Execute(template);
        }
    }
}
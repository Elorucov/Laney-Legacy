using Elorucov.Laney.Controls.MessageAttachments;
using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Elorucov.Laney.Controls {
    public static class MessageUIConstants {
        public const string SenderNameCC = "SenderName";
        public const string ReplyMessageButton = "ReplyMessage";
        public const string MsgTextBlockControlName = "Text";
        public const string StickerOrGraffitiControlName = "StickerOrGraffiti";
        public const string GiftControlName = "Gift";
        public const string PhotosContainerControlName = "Container";
        public const string LocationControlName = "Location";
        public const string ForwardedMessageControlName = "ForwardedMessage";
        public const string WallPostControlName = "Wallpost";
        public const string WallReplyControlName = "Wallreply";
        public const string LinkControlName = "Link";
        public const string PollControlName = "Poll";
        public const string CallControlName = "Call";
        public const string StoryControlName = "Story";
        public const string AudioMsgControlName = "AudioMessage";
        public const string VideoMsgControlName = "VideoMessage";
        public const string DocumentAttachmentControlName = "Document";
        public const string AudioAttachmentControlName = "Audio";
        public const string StandartAttachmentControlName = "StandartAttachment";
    }

    public class BubbleMessageUI : Grid {
        LMessage msg;

        private double BubbleFixedWidth { get { return GetBubbleFixedWidth(); } }

        private double GetBubbleFixedWidth() {
            return Window.Current.Bounds.Width >= 420 ? 356 : 296;
        }

        bool isChat;
        bool isOutgoing;
        bool containsOnlyImage;
        bool containsOnlyEmojis;
        bool hasGift;
        bool hasSticker;
        bool hasAttachments;
        double bubbleWidth = 0;

        // UI elements with events.
        // При Unloaded надо отписываться от событий
        Avatar ava;
        StoryUC sc;
        HyperlinkButton rb;
        ContentPresenter stsb;
        ContentPresenter strb;

        public BubbleMessageUI(LMessage msg, ScrollViewer parentScroll, bool hideAvatar = false) {
            this.msg = msg;
            Margin = new Thickness(0, 4, 0, 4);
            MaxWidth = 1280;
            Unloaded += BubbleMessageUI_Unloaded;

            isChat = msg.PeerId.IsChat();
            isOutgoing = msg.SenderId == AppParameters.UserID;
            containsOnlyImage = msg.ContainsOnlyImage();
            containsOnlyEmojis = msg.ContainsOnlyEmojis();
            hasGift = msg.HasGift();
            hasSticker = msg.HasSticker();
            hasAttachments = msg.HasAttachments(true);

            if (hasAttachments) bubbleWidth = BubbleFixedWidth;
            if (hasGift) bubbleWidth = 184;
            if (hasSticker) bubbleWidth = 168;
            bool useFixedWidth = bubbleWidth > 0;

            if (!isOutgoing) {
                ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MaxWidth = 40 });
                ColumnDefinitions.Add(new ColumnDefinition());
            } else {
                ColumnDefinitions.Add(new ColumnDefinition());
                ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
            }
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Show avatar in chats
            if (isChat && !isOutgoing) {
                ava = new Avatar {
                    Margin = new Thickness(12, 0, 0, 0), Width = 32, Height = 32,
                    VerticalAlignment = VerticalAlignment.Top, DisplayName = msg.SenderName
                };

                new System.Action(async () => { await ava.SetUriSourceAsync(msg.SenderAvatar); })();
                ava.Opacity = hideAvatar ? 0 : 1;
                ToolTipService.SetToolTip(ava, msg.SenderName);
                if (msg.PeerId != long.MaxValue) ava.Click += OnAvatarClicked;
                Children.Add(ava);
            }

            // Bubble container
            Grid bubblecontainer = new Grid {
                Margin = new Thickness(ava != null ? 8 : 12, 0, 12, 0),
                MinHeight = 32, MinWidth = 64,
                HorizontalAlignment = !isOutgoing ? HorizontalAlignment.Left : HorizontalAlignment.Right,
            };

            if (msg.IsPossibleToShowStoryControl()) { // Story
                Story s = msg.Attachments.Where(q => q.Type == AttachmentType.Story).First().Story;
                sc = new StoryUC {
                    Story = s,
                    HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left
                };
                sc.Click += StoryClicked;

                bubblecontainer.Children.Add(sc);

                // Sticker
                Sticker ssticker = msg.Attachments.Count == 2 ?
                    msg.Attachments.Where(q => q.Type == AttachmentType.Sticker).First().Sticker : null;
                if (ssticker != null) {
                    bubblecontainer.Margin = new Thickness(12, 0, 12, bubbleWidth / 2);

                    StickerPresenter img = new StickerPresenter();
                    img.IsDarkThemeForced = true;
                    img.Width = img.Height = 128;
                    img.Sticker = ssticker;
                    img.VerticalAlignment = VerticalAlignment.Bottom;
                    img.HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                    img.RenderTransform = new CompositeTransform { TranslateY = 64 };
                    bubblecontainer.Children.Add(img);
                }

                ContentControl sstime = isOutgoing ?
                GetTimeAndStatusElement(msg, "TimeInBubbleImageTemplate") :
                GetSentTimeControl(msg.Date, "TimeInBubbleImageTemplate", msg.UpdateTime);

                if (ssticker != null) sstime.RenderTransform = new CompositeTransform { TranslateY = 64 };
                sstime.Margin = new Thickness(4, 0, 4, 4);
                sstime.HorizontalAlignment = !isOutgoing ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                bubblecontainer.Children.Add(sstime);

                // Reactions
                if (msg.Reactions.Count > 0) {
                    bubblecontainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    bubblecontainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    double rightInlineMargin = msg.UpdateTime != null ? 96 : 64;
                    double rightMargin = isOutgoing ? 12 : rightInlineMargin;
                    ReactionsChips rc = new ReactionsChips(msg.PeerId, msg.ConversationMessageId) {
                        IsOutgoing = isOutgoing,
                        IsDarkAppearance = true,
                        HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                        Margin = new Thickness(0, 4, 0, 0),
                        Reactions = msg.Reactions,
                        SelectedReactionId = msg.SelectedReactionId,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        RenderTransform = new CompositeTransform { TranslateY = 64 }
                    };

                    Binding binding = new Binding() {
                        Path = new PropertyPath(nameof(LMessage.SelectedReactionId)),
                        Mode = BindingMode.OneWay
                    };
                    rc.SetBinding(ReactionsChips.SelectedReactionIdProperty, binding);

                    SetRow(rc, 1);
                    bubblecontainer.Children.Add(rc);
                }

                // Star and unread/delete icon
                AddStateIcons(msg, bubblecontainer, isOutgoing);

                // Return formed UI for story
                if (!isOutgoing) SetColumn(bubblecontainer, 1);
                Children.Add(bubblecontainer);
                return;
            } else if (msg.ContainsWidgets()) { // Widgets
                Rectangle background = (Rectangle)(Application.Current.Resources["DefaultIncomingBubbleTemplate"] as DataTemplate).LoadContent();
                bubblecontainer.Children.Insert(0, background);

                StackPanel widgetsContainer = new StackPanel();
                var widgets = msg.Attachments.Where(a => a.Type == AttachmentType.Widget).Select(a => a.Widget);
                foreach (var widget in widgets) {
                    FrameworkElement widgetUI = WidgetRenderer.Render(widget, async (uri) => await VKLinks.LaunchLinkAsync(uri));
                    widgetsContainer.Children.Add(widgetUI);
                }
                bubblecontainer.Children.Add(widgetsContainer);

                // Reactions
                if (msg.Reactions.Count > 0) {
                    bubblecontainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    bubblecontainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    double rightInlineMargin = msg.UpdateTime != null ? 96 : 64;
                    double rightMargin = isOutgoing ? 12 : rightInlineMargin;
                    ReactionsChips rc = new ReactionsChips(msg.PeerId, msg.ConversationMessageId) {
                        IsOutgoing = isOutgoing,
                        IsDarkAppearance = true,
                        HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                        Margin = new Thickness(0, 4, 0, 0),
                        Reactions = msg.Reactions,
                        SelectedReactionId = msg.SelectedReactionId,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };

                    Binding binding = new Binding() {
                        Path = new PropertyPath(nameof(LMessage.SelectedReactionId)),
                        Mode = BindingMode.OneWay
                    };
                    rc.SetBinding(ReactionsChips.SelectedReactionIdProperty, binding);

                    SetRow(rc, 1);
                    bubblecontainer.Children.Add(rc);
                }

                // Return formed UI for widgets
                bubblecontainer.MaxWidth = 404;
                if (!isOutgoing) SetColumn(bubblecontainer, 1);
                Children.Add(bubblecontainer);
                return;
            }

            bubblecontainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            if (useFixedWidth) {
                if (bubbleWidth == BubbleFixedWidth) {
                    bubblecontainer.MaxWidth = bubbleWidth;
                } else {
                    bubblecontainer.Width = bubbleWidth;
                }
            }

            StackPanel sp = new StackPanel {
                CornerRadius = new CornerRadius(16),
            };

            bool transparentBubble = (msg.HasSticker() || msg.HasOnlyGraffiti()) && msg.ReplyMessage != null;

            if (!containsOnlyImage && !containsOnlyEmojis) {
                string template = isOutgoing ? "DefaultOutgoingBubbleTemplate" : "DefaultIncomingBubbleTemplate";
                if (msg.HasGift()) template = "GiftBubbleTemplate";
                if (transparentBubble) template = "TransparentWithBorderBubbleTemplate";
                Rectangle background = (Rectangle)(Application.Current.Resources[template] as DataTemplate).LoadContent();
                bubblecontainer.Children.Insert(0, background);

                // CTE
                if (isOutgoing) {
                    bubblecontainer.DataContext = msg;
                    ChatThemeService.RegisterOutgoingBubbleUI(bubblecontainer);
                    if (parentScroll != null) ChatThemeService.RegisterForGradientBackground(background, parentScroll);
                }
            }

            // Show sender name in chats
            bool senderNameDisplayed = false;
            if (isChat && !isOutgoing && !hideAvatar && !containsOnlyImage && !containsOnlyEmojis) {
                ContentControl ncc = new ContentControl {
                    Name = MessageUIConstants.SenderNameCC,
                    Template = (ControlTemplate)Application.Current.Resources["SenderNameInBubbleTemplate"],
                    Margin = new Thickness(12, 7, 12, 0),
                    Content = msg.SenderName
                };
                sp.Children.Insert(0, ncc);
                senderNameDisplayed = true;
            }

            // Reply message
            if (msg.ReplyMessage != null) {
                CompactMessageControl cmc = new CompactMessageControl {
                    Message = msg.ReplyMessage,
                    BorderThickness = new Thickness(2, 0, 0, 0),
                    Padding = new Thickness(10, 0, 0, 0),
                };

                rb = new HyperlinkButton();
                rb.Name = MessageUIConstants.ReplyMessageButton;
                rb.HorizontalAlignment = HorizontalAlignment.Left;
                rb.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                rb.Padding = new Thickness(0);
                rb.Margin = new Thickness(12, 12, 12, 0);
                rb.MaxWidth = 512;
                rb.Content = cmc;
                rb.Click += OnReplyMessageClick;

                sp.Children.Add(rb);
            }

            // Images (if ThumbsPosition == true and contains another content)
            bool dontAddPreviews = false;
            if (AppParameters.ThumbsPosition && !containsOnlyImage) {
                dontAddPreviews = true;
                List<IPreview> previews = new List<IPreview>();

                foreach (Attachment a in msg.Attachments) {
                    switch (a.Type) {
                        case AttachmentType.Photo: if (a.Photo.Sizes != null && a.Photo.Sizes.Count > 0) previews.Add(a.Photo); break;
                        case AttachmentType.Video: previews.Add(a.Video); break;
                        case AttachmentType.Document: if (a.Document.Preview != null) { previews.Add(a.Document); } break;
                    }
                }

                if (previews.Count > 0) {
                    Grid tg = MessageUIHelper.AddThumbnailsGrid(previews, BubbleFixedWidth - 4, msg.ConversationMessageId);
                    tg.HorizontalAlignment = HorizontalAlignment.Left;
                    tg.Margin = new Thickness(2, senderNameDisplayed && msg.ReplyMessage == null ? 6 : 2, 2, 2);
                    sp.Children.Add(tg);

                    // Fix bubble width
                    bubblecontainer.MaxWidth = double.IsNaN(tg.Width) ? BubbleFixedWidth : tg.Width + 4;
                }
            }

            // Text
            if (!string.IsNullOrEmpty(msg.Text)) {
                if (containsOnlyEmojis) {
                    TextBlock tb = new TextBlock {
                        Name = MessageUIConstants.MsgTextBlockControlName,
                        Text = msg.Text,
                        FontSize = 42,
                        MaxWidth = 420,
                        Margin = new Thickness(0, 0, 48, 8)
                    };
                    sp.Children.Add(tb);
                } else {
                    RichTextBlock rtb = MessageUIHelper.BuildTextBlock(msg.ParsedTextInfo, !msg.HasAttachments(true) && msg.Reactions.Count == 0, msg.UpdateTime != null, isOutgoing);
                    rtb.Margin = new Thickness(12, 7, 12, 9);
                    rtb.MaxWidth = 576;
                    sp.Children.Add(rtb);
                }
            }

            // Images (in other cases)
            if (!dontAddPreviews) {
                dontAddPreviews = true;
                List<IPreview> previews = new List<IPreview>();

                foreach (Attachment a in msg.Attachments) {
                    switch (a.Type) {
                        case AttachmentType.Photo: if (a.Photo.Sizes != null && a.Photo.Sizes.Count > 0) previews.Add(a.Photo); break;
                        case AttachmentType.Video: previews.Add(a.Video); break;
                        case AttachmentType.Document: if (a.Document.Preview != null) { previews.Add(a.Document); } break;
                    }
                }

                if (previews.Count > 0) {
                    Grid tg = MessageUIHelper.AddThumbnailsGrid(previews, containsOnlyImage ? BubbleFixedWidth : BubbleFixedWidth - 4, msg.ConversationMessageId);
                    tg.HorizontalAlignment = HorizontalAlignment.Left;
                    tg.Margin = new Thickness(2);
                    sp.Children.Add(tg);

                    // Fix bubble width
                    bubblecontainer.MaxWidth = double.IsNaN(tg.Width) ? BubbleFixedWidth : tg.Width + 4;
                }
            }

            // Attachments
            double smargin = containsOnlyImage ? 0 : 16;
            MessageUIHelper.AddAttachmentsToPanel(msg.ConversationMessageId, sp, msg.Attachments,
                    containsOnlyImage ? BubbleFixedWidth : BubbleFixedWidth - 4,
                    containsOnlyImage ? new Thickness(0) : new Thickness(2),
                    bubbleWidth - 8, new Thickness(4),
                    bubbleWidth - smargin, new Thickness(smargin / 2),
                    BubbleFixedWidth - 16, new Thickness(8),
                    BubbleFixedWidth - 8, new Thickness(4),
                    msg.Geo, true, msg.SenderName, true);

            // Forwarded messages 
            if (msg.ForwardedMessages.Count > 0) {
                if (msg.ForwardedMessages.Count <= 3) {
                    for (int i = 0; i < Math.Min(msg.ForwardedMessages.Count, 3); i++) {
                        LMessage fmsg = msg.ForwardedMessages[i];
                        ContentControl fwdc = new ContentControl {
                            Name = MessageUIConstants.ForwardedMessageControlName,
                            Template = Application.Current.Resources["ForwardedMessagesInBubbleTemplate"] as ControlTemplate,
                            Content = new LegacyMessageUI(fmsg, true, 1, isOutgoing),
                        };
                        sp.Children.Add(fwdc);
                    }
                } else {
                    HyperlinkButton fwdlink = new HyperlinkButton {
                        Style = Application.Current.Resources["AccentHyperlinkStyle"] as Style,
                        Margin = new Thickness(12, 7, 12, 5),
                        Padding = new Thickness(0),
                        Content = msg.ForwardedMessages.Count > 1 ?
                        $"{msg.ForwardedMessages.Count} {Locale.GetDeclension(msg.ForwardedMessages.Count, "forwarded_msgs_link").ToLower()}" :
                        Locale.Get("forwarded_msgs_link_nom"),
                    };
                    fwdlink.Click += (a, b) => MessageUIHelper.ShowForwardedMessages(msg.ForwardedMessages);
                    sp.Children.Add(fwdlink);
                }
            }

            // Fix elements
            FixElements(sp.Children, !containsOnlyImage);

            // Remove corner radius for sticker; graffiti and videomessage
            if (sp.Children.Count > 0 && ((sp.Children.Last() is Grid pcc && pcc.Name == MessageUIConstants.PhotosContainerControlName)
                || sp.Children.Last() is VideoMessageControl
                || (sp.Children.Last() is FrameworkElement gss && gss.Name == MessageUIConstants.StickerOrGraffitiControlName))) sp.CornerRadius = new CornerRadius(0);

            // And add stackpanel with content in bubble
            bubblecontainer.Children.Add(sp);

            // Check if message is empty
            if (sp.Children.Count == 0 ||
                (sp.Children.Count == 1 && sp.Children[0] is ContentControl sncc && sncc.Name == MessageUIConstants.SenderNameCC) ||
                (sp.Children.Count == 2 && sp.Children[0] is ContentControl snccc && snccc.Name == MessageUIConstants.SenderNameCC &&
                sp.Children[1] is Button rbc && rbc.Name == MessageUIConstants.ReplyMessageButton)) {
                TextBlock tb = new TextBlock { Text = Locale.Get(msg.UISentMessageState == SentMessageState.Loading ? "loading" : "empty_message"), FontStyle = Windows.UI.Text.FontStyle.Italic, Opacity = 0.7 };
                tb.Margin = new Thickness(12, 7, 48, 9);
                sp.Children.Add(tb);
            }

            // Add sent time
            bool forImages = sp.Children.Last() is Grid pc && pc.Name == MessageUIConstants.PhotosContainerControlName ||
                (sp.Children.Last() is TextBlock stb && stb.Name == MessageUIConstants.MsgTextBlockControlName) ||
                (sp.Children.Last() is Border geo && geo.Name == MessageUIConstants.LocationControlName) ||
                (sp.Children.Last() is ArticlePreview ap) ||
                (sp.Children.Last() is VideoMessageControl vms) ||
                (sp.Children.Last() is FrameworkElement gs && gs.Name == MessageUIConstants.StickerOrGraffitiControlName && msg.ReplyMessage == null);

            string stemp = "TimeInBubbleImageTemplate";
            if (!forImages && !transparentBubble) {
                stemp = isOutgoing ? "TimeInBubbleOutgoingTemplate" : "TimeInBubbleIncomingTemplate";
                if (msg.HasGift()) stemp = "TimeInBubbleGiftTemplate";
            }

            ContentControl stime = isOutgoing ?
                GetTimeAndStatusElement(msg, stemp) :
                GetSentTimeControl(msg.Date, stemp, msg.UpdateTime);

            double tmargin = 0;
            if (forImages) tmargin = containsOnlyImage ? 4 : 8;
            if (transparentBubble) tmargin = 8;
            stime.Margin = new Thickness(0, 0, tmargin, tmargin);
            bubblecontainer.Children.Add(stime);

            // Star and unread/delete icon
            AddStateIcons(msg, bubblecontainer, isOutgoing);

            // Inline keyboard
            if (msg.Keyboard != null && msg.Keyboard.Inline) {
                bubblecontainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                sp.CornerRadius = new CornerRadius(16, 16, 0, 0);
                Rectangle background = bubblecontainer.Children[0] as Rectangle;
                if (background != null) {
                    SetRowSpan(background, bubblecontainer.RowDefinitions.Count);
                } else {
                    background = (Rectangle)(Application.Current.Resources["DefaultIncomingBubbleTemplate"] as DataTemplate).LoadContent();
                    SetRowSpan(background, bubblecontainer.RowDefinitions.Count);
                    bubblecontainer.Children.Insert(0, background);
                }

                ContentControl cc = new ContentControl {
                    Content = MessageUIHelper.BuildInlineKeyboard(msg.ConversationMessageId, msg.Keyboard),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                SetRow(cc, 1);
                bubblecontainer.Children.Add(cc);
            }

            // Reactions
            if (msg.Reactions.Count > 0) {
                bubblecontainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                int rowForReaction = bubblecontainer.RowDefinitions.Count - 1;
                double rightInlineMargin = msg.UpdateTime != null ? 96 : 64;
                bool isLastElementIsPreviews = sp.Children.LastOrDefault() is Grid pcg && pcg?.Name == MessageUIConstants.PhotosContainerControlName;

                ReactionsChips rc = new ReactionsChips(msg.PeerId, msg.ConversationMessageId) {
                    IsDarkAppearance = containsOnlyEmojis || containsOnlyImage,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = containsOnlyEmojis || containsOnlyImage ? new Thickness(0, 4, 0, 0) : new Thickness(12, isLastElementIsPreviews ? 6 : 0, rightInlineMargin, 8),
                    Reactions = msg.Reactions,
                    SelectedReactionId = msg.SelectedReactionId
                };

                Binding binding = new Binding() {
                    Path = new PropertyPath(nameof(LMessage.SelectedReactionId)),
                    Mode = BindingMode.OneWay
                };
                rc.SetBinding(ReactionsChips.SelectedReactionIdProperty, binding);

                SetRow(rc, rowForReaction);
                if (!containsOnlyEmojis && !containsOnlyImage) { // Fix time
                    string nstemp = isOutgoing ? "TimeInBubbleOutgoingTemplate" : "TimeInBubbleIncomingTemplate";
                    stime.Template = Application.Current.Resources[nstemp] as ControlTemplate;
                    stime.Margin = new Thickness(0);
                    SetRow(stime, rowForReaction);
                }
                bubblecontainer.Children.Add(rc);

                Rectangle background = bubblecontainer.Children[0] as Rectangle;
                if (background != null) {
                    SetRowSpan(background, bubblecontainer.RowDefinitions.Count);
                }

                // Fix sent time row.
                if (!forImages) SetRow(stime, rowForReaction);
            }

            // Carousel
            if (msg.Template != null && msg.Template.Type == BotTemplateType.Carousel) {
                RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Border b = new Border {
                    Margin = new Thickness(0, 4, 0, 0),
                    Child = MessageUIHelper.BuildCarousel(msg.Template.Elements, msg.ConversationMessageId)
                };
                SetRow(b, 1);
                SetColumnSpan(b, 2);
                Children.Add(b);
            }

            // Self-destruction timer
            if (msg.TTL > 0) {
                ContentControl sdc = new ContentControl {
                    Template = (ControlTemplate)Application.Current.Resources["SelfDestructionTimerTemplate"],
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = isOutgoing ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                    RenderTransform = new TranslateTransform {
                        Y = -2,
                        X = isOutgoing ? -18 : 18
                    }
                };
                bubblecontainer.Children.Add(sdc);

                DispatcherTimer timer = new DispatcherTimer {
                    Interval = TimeSpan.FromSeconds(0.5)
                };
                timer.Tick += (a, z) => {
                    TimeSpan expiration = DateTime.Now - msg.Date;
                    int remaining = msg.TTL - Convert.ToInt32(expiration.TotalSeconds);
                    if (remaining > 0) {
                        sdc.Content = TimeSpan.FromSeconds(remaining).ToNormalString();
                    } else {
                        timer.Stop();
                    }
                };
                timer.Start();
            }

            // Finish
            if (!isOutgoing) SetColumn(bubblecontainer, 1);
            Children.Add(bubblecontainer);
        }

        private void OnReplyMessageClick(object sender, RoutedEventArgs e) {
            AppSession.CurrentConversationVM.GoToMessage(msg.ReplyMessage);
        }

        #region Events

        private void OnAvatarClicked(object sender, RoutedEventArgs e) {
            VKLinks.ShowPeerInfoModal(msg.SenderId);
        }

        private void BubbleMessageUI_Unloaded(object sender, RoutedEventArgs e) {
            if (ava != null && msg.PeerId != long.MaxValue) ava.Click -= OnAvatarClicked;
            if (sc != null) sc.Click -= StoryClicked;
            if (rb != null) rb.Click -= OnReplyMessageClick;
            msg.PropertyChanged -= OnMessagePropertyChanged;
        }

        #endregion

        private DataTemplate GetIconByMessageState(SentMessageState state) {
            switch (state) {
                case SentMessageState.Loading: return (DataTemplate)Application.Current.Resources["LoadingMessageIconTemplate"];
                case SentMessageState.Unread: return (DataTemplate)Application.Current.Resources["UnreadMessageIconTemplate"];
                case SentMessageState.Deleted: return (DataTemplate)Application.Current.Resources["DeletedMessageIconTemplate"];
            }
            return null;
        }

        private DataTemplate GetIconByMessageStateNew(SentMessageState state) {
            switch (state) {
                case SentMessageState.Loading: return (DataTemplate)Application.Current.Resources["LoadingMsgIcon"];
                case SentMessageState.Unread: return (DataTemplate)Application.Current.Resources["DeliveredCheckInMsgIcon"];
                case SentMessageState.Read: return (DataTemplate)Application.Current.Resources["ReadCheckInMsgIcon"];
                case SentMessageState.Deleted: return (DataTemplate)Application.Current.Resources["DeletedMsgIcon"];
            }
            return null;
        }

        private ContentControl GetTimeAndStatusElement(LMessage msg, string template) {
            DateTime dateTime = msg.Date;
            DateTime? updateTime = msg.UpdateTime;

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock stx = new TextBlock { FontSize = 12 };
            if (updateTime != null) stx.Inlines.Add(new Run { Text = $"{Locale.Get("edited")} · " });
            stx.Inlines.Add(new Run { Text = dateTime.ToShortTimeString() });
            sp.Children.Add(stx);

            if (msg.SenderId == AppParameters.UserID) {
                ContentPresenter statusIcon = new ContentPresenter {
                    Margin = new Thickness(2, 0, 0, 0),
                    RenderTransform = new CompositeTransform { TranslateY = 1 }
                };
                statusIcon.ContentTemplate = GetIconByMessageStateNew(msg.UISentMessageState);
                sp.Children.Add(statusIcon);
                msg.PropertyChanged += (a, b) => {
                    switch (b.PropertyName) {
                        case nameof(msg.UISentMessageState):
                            statusIcon.ContentTemplate = GetIconByMessageStateNew(msg.UISentMessageState);
                            break;
                    }
                };
            }

            ContentControl stime = new ContentControl {
                Name = "SentTime",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            stime.Template = Application.Current.Resources[template] as ControlTemplate;
            stime.Content = sp;

            if (updateTime != null) ToolTipService.SetToolTip(stime, updateTime.Value.ToTimeAndDate());

            return stime;
        }

        private ContentControl GetSentTimeControl(DateTime dateTime, string template, DateTime? updateTime) {
            TextBlock stx = new TextBlock { FontSize = 12 };
            if (updateTime != null) stx.Inlines.Add(new Run { Text = $"{Locale.Get("edited")} · " });
            stx.Inlines.Add(new Run { Text = dateTime.ToShortTimeString() });

            ContentControl stime = new ContentControl {
                Name = "SentTime",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            stime.Template = Application.Current.Resources[template] as ControlTemplate;
            stime.Content = stx;

            if (updateTime != null) ToolTipService.SetToolTip(stime, updateTime.Value.ToTimeAndDate());

            return stime;
        }

        private void AddStateIcons(LMessage msg, Grid bubblecontainer, bool isOutgoing) {
            // Star and unread/delete icon
            strb = new ContentPresenter {
                Width = 12,
                Height = 12,
                HorizontalAlignment = !isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransform = new CompositeTransform { TranslateX = !isOutgoing ? 12 : -12 },
                ContentTemplate = (DataTemplate)Application.Current.Resources["ImportantMessageIconTemplate"],
                Visibility = msg.IsImportant ? Visibility.Visible : Visibility.Collapsed
            };
            bubblecontainer.Children.Add(strb);

            bool legacyReadIndicator = !isOutgoing;

            if (legacyReadIndicator) {
                stsb = new ContentPresenter {
                    Width = 12,
                    Height = 12,
                    HorizontalAlignment = !isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    RenderTransform = new CompositeTransform { TranslateX = !isOutgoing ? 12 : -12 },
                    ContentTemplate = GetIconByMessageState(msg.UISentMessageState)
                };
                SetRow(stsb, 3);
                bubblecontainer.Children.Add(stsb);
            }

            // Update some UI when changed property in message
            msg.PropertyChanged += OnMessagePropertyChanged;
        }

        private void OnMessagePropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(msg.UISentMessageState):
                    if (isOutgoing) return;
                    stsb.ContentTemplate = GetIconByMessageState(msg.UISentMessageState);
                    break;
                case nameof(msg.IsImportant):
                    strb.Visibility = msg.IsImportant ? Visibility.Visible : Visibility.Collapsed;
                    break;
            }
        }

        private void StoryClicked(object sender, RoutedEventArgs e) {
            Story s = null;
            FrameworkElement senderElement = null;
            if (sender is StoryUC suc) {
                senderElement = suc;
                s = suc.Story;
            } else if (sender is DefaultAttachmentControl dac) {
                s = dac.Tag as Story;
            }
            if (s == null) return;
            if (ViewManagement.GetWindowType() == WindowType.Main && !s.IsDeleted && !s.IsExpired && s.CanSee)
                Pages.StoryViewer.Show(s, senderElement);
        }

        private void FixElements(UIElementCollection elements, bool isInBubble) {
            int i = 0;
            foreach (UIElement el in elements) {
                bool isFirst = i == 0;
                bool isLast = i == elements.Count - 1;

                if (el is RichTextBlock tb && tb.Name == MessageUIConstants.MsgTextBlockControlName) {
                    tb.Margin = isLast ? new Thickness(12, 7, 12, 9) : new Thickness(12, 7, 12, 5);
                    tb.Margin = new Thickness(12, isFirst ? 7 : 3, 12, isLast ? 9 : 5);
                }
                if (el is Grid pc && pc.Name == MessageUIConstants.PhotosContainerControlName) {
                    double mb = isInBubble ? 2 : 0;
                    pc.Margin = new Thickness(pc.Margin.Left, pc.Margin.Top, pc.Margin.Right, isLast ? mb : 6);
                    pc.CornerRadius = new CornerRadius(14);
                }
                if (el is Border map && map.Name == MessageUIConstants.LocationControlName) {
                    map.CornerRadius = new CornerRadius(isFirst ? 14 : 4, isFirst ? 14 : 4, isLast ? 14 : 4, isLast ? 14 : 4);
                }
                if (el is DefaultAttachmentControl uc) {
                    uc.Margin = new Thickness(12, isFirst ? 12 : 6, 12, isLast ? 12 : 6);
                }
                if (el is ExtendedAttachmentControl eac) {
                    eac.Margin = new Thickness(12, isFirst ? 12 : 6, 12, isLast ? 12 : 6);
                }
                if (el is ArticlePreview ap) {
                    ap.Margin = new Thickness(4, isFirst ? 4 : 6, 4, isLast ? 4 : 6);
                }
                if (el is VideoMessageControl vms) {
                    vms.Margin = new Thickness(isFirst && isLast ? 0 : 4, isFirst ? 0 : 6, isFirst && isLast ? 0 : 4, isLast ? 4 : 6);
                }
                if (el is VoiceMessageControl vmc) {
                    vmc.Margin = new Thickness(12, isFirst ? 12 : 6, 12, isLast ? 12 : 6);
                }
                if (el is AudioUC auc) {
                    auc.Margin = new Thickness(12, isFirst ? 12 : 6, 12, isLast ? 12 : 6);
                }
                if (el is ContentControl cc && cc.Name == MessageUIConstants.ForwardedMessageControlName) {
                    cc.Margin = new Thickness(12, isFirst ? 12 : 6, 12, isLast ? 28 : 6);
                }
                if (el is HyperlinkButton rb && rb.Name == MessageUIConstants.ReplyMessageButton) {
                    rb.Margin = new Thickness(rb.Margin.Left, isFirst ? 12 : 4, rb.Margin.Right, rb.Margin.Bottom);
                }
                if (el is HyperlinkButton hbtn && hbtn.Name != MessageUIConstants.ReplyMessageButton) {
                    hbtn.Margin = new Thickness(12, isFirst ? 7 : 0, 12, isLast ? 9 : 5);
                }
                i++;
            }
        }
    }

    public class LegacyMessageUI : Grid {
        public LegacyMessageUI(LMessage msg, bool showSentDay, int recursive = 0, bool isInOutgoingBubble = false) {
            MaxWidth = 1280;
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MaxWidth = 40 });
            ColumnDefinitions.Add(new ColumnDefinition());

            // Avatar
            Avatar ava = new Avatar {
                Margin = new Thickness(0, 0, 8, 0), Width = 32, Height = 32,
                VerticalAlignment = VerticalAlignment.Top, DisplayName = msg.SenderName
            };

            new System.Action(async () => { await ava.SetUriSourceAsync(msg.SenderAvatar); })();
            ava.Visibility = Visibility.Visible;
            ToolTipService.SetToolTip(ava, msg.SenderName);
            ava.Click += (a, b) => VKLinks.ShowPeerInfoModal(msg.SenderId);
            Children.Add(ava);

            // Sender info and sent time
            StackPanel sinfo = new StackPanel { Visibility = Visibility.Visible };
            HyperlinkButton hbtn = new HyperlinkButton {
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Padding = new Thickness(0),
                FontSize = 13,
                ContentTemplate = (DataTemplate)Application.Current.Resources["TextLikeHyperlinkBtnTemplate"],
                Content = msg.SenderName
            };
            hbtn.RightTapped += (a, b) => b.Handled = false;
            hbtn.Click += (a, b) => VKLinks.ShowPeerInfoModal(msg.SenderId);
            sinfo.Children.Add(hbtn);
            sinfo.Children.Add(new TextBlock {
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 300 },
                FontSize = 13,
                Opacity = 0.8,
                Text = !showSentDay ? msg.Date.ToShortTimeString() : msg.Date.ToTimeAndDate(),
            });
            sinfo.Margin = new Thickness(0, -3, 0, 0);
            SetColumn(sinfo, 1);
            Children.Add(sinfo);

            // Message content
            StackPanel sp = new StackPanel();
            SetRow(sp, 1);
            SetColumnSpan(sp, 2);

            if (msg.ContainsWidgets()) { // Widgets
                var widgets = msg.Attachments.Where(a => a.Type == AttachmentType.Widget).Select(a => a.Widget);
                foreach (var widget in widgets) {
                    FrameworkElement widgetUI = WidgetRenderer.Render(widget, async (uri) => await VKLinks.LaunchLinkAsync(uri));
                    sp.Children.Add(widgetUI);
                }

                // Return formed UI for widgets
                sp.Margin = new Thickness(-12, 0, -12, 0);
                Children.Add(sp);
                return;
            }

            // Images (if ThumbsPosition == true)
            bool dontAddPreviews = false;
            if (AppParameters.ThumbsPosition) {
                dontAddPreviews = true;
                List<IPreview> previews = new List<IPreview>();

                foreach (Attachment a in msg.Attachments) {
                    switch (a.Type) {
                        case AttachmentType.Photo: if (a.Photo.Sizes != null && a.Photo.Sizes.Count > 0) previews.Add(a.Photo); break;
                        case AttachmentType.Video: previews.Add(a.Video); break;
                        case AttachmentType.Document: if (a.Document.Preview != null) { previews.Add(a.Document); } break;
                    }
                }

                if (previews.Count > 0) {
                    Grid tg = MessageUIHelper.AddThumbnailsGrid(previews, 296, msg.ConversationMessageId);
                    tg.HorizontalAlignment = HorizontalAlignment.Left;
                    tg.Margin = new Thickness(0, 6, 0, 6);
                    sp.Children.Add(tg);
                }
            }

            // Text
            if (!string.IsNullOrEmpty(msg.Text)) {
                RichTextBlock rtb = MessageUIHelper.BuildTextBlock(msg.ParsedTextInfo, isForOutgoingBubble: isInOutgoingBubble);
                rtb.Name = MessageUIConstants.MsgTextBlockControlName;
                rtb.Margin = new Thickness(0, 6, 0, 6);
                sp.Children.Add(rtb);
            }

            // Attachments
            MessageUIHelper.AddAttachmentsToPanel(msg.ConversationMessageId, sp, msg.Attachments,
                    296, new Thickness(0, 6, 0, 6),
                    176, new Thickness(0, 6, 0, 6),
                    168, new Thickness(0, 6, 0, 6),
                    168, new Thickness(0, 6, 0, 6),
                    296, new Thickness(0, 6, 0, 6),
                    msg.Geo, true, msg.SenderName, dontAddPreviews);

            // Forwarded messages 
            if (msg.ForwardedMessages.Count > 0) {
                System.Diagnostics.Debug.WriteLine($"BuildNonBubbleMessage > Recursive: {recursive}");
                if (msg.ForwardedMessages.Count <= 1 && recursive < 3) {
                    for (int i = 0; i < Math.Min(msg.ForwardedMessages.Count, 1); i++) {
                        LMessage fmsg = msg.ForwardedMessages[i];
                        ContentControl fwdc = new ContentControl {
                            Name = MessageUIConstants.ForwardedMessageControlName,
                            Template = Application.Current.Resources["ForwardedMessagesInBubbleTemplate"] as ControlTemplate,
                            Content = new LegacyMessageUI(fmsg, true, recursive + 1),
                            Margin = new Thickness(0, 6, 0, 6),
                        };
                        sp.Children.Add(fwdc);
                    }
                } else {
                    HyperlinkButton fwdlink = new HyperlinkButton {
                        Style = Application.Current.Resources["AccentHyperlinkStyle"] as Style,
                        Margin = new Thickness(0, 7, 0, 5),
                        Padding = new Thickness(0),
                        Content = msg.ForwardedMessages.Count > 1 ?
                        $"{msg.ForwardedMessages.Count} {Locale.GetDeclension(msg.ForwardedMessages.Count, "forwarded_msgs_link").ToLower()}" :
                        Locale.Get("forwarded_msgs_link_nom"),
                    };
                    fwdlink.Click += (a, b) => MessageUIHelper.ShowForwardedMessages(msg.ForwardedMessages);
                    sp.Children.Add(fwdlink);
                }
            }

            // Reply message 
            if (msg.ReplyMessage != null) {
                HyperlinkButton replink = new HyperlinkButton {
                    Style = Application.Current.Resources["AccentHyperlinkStyle"] as Style,
                    Margin = new Thickness(0, 0, 0, 6),
                    Padding = new Thickness(0),
                    Content = Locale.Get("reply_msg_link"),
                };
                replink.Click += (a, b) => MessageUIHelper.ShowForwardedMessages(new List<LMessage> { msg.ReplyMessage });
                sp.Children.Add(replink);
            }

            if (sp.Children.Count > 0) {
                FrameworkElement lfe = sp.Children.Last() as FrameworkElement;
                lfe.Margin = new Thickness(lfe.Margin.Left, lfe.Margin.Top, lfe.Margin.Right, 0);
            } else {
                TextBlock tb = new TextBlock { Text = Locale.Get("empty_message"), FontStyle = Windows.UI.Text.FontStyle.Italic, Opacity = 0.7 };
                tb.Margin = new Thickness(0, 6, 0, 0);
                sp.Children.Add(tb);
            }

            Children.Add(sp);
        }
    }
}

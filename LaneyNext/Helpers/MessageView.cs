using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Controls;
using Elorucov.Laney.Controls.Attachments;
using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers.UI;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.Views.Modals;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VK.VKUI.Controls;
using VK.VKUI.Popups;
using VKStylePhotoLayout;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Action = ELOR.VKAPILib.Objects.Action;
using Group = ELOR.VKAPILib.Objects.Group;
using MenuFlyout = VK.VKUI.Popups.MenuFlyout;
using Size = ELOR.VKAPILib.Objects.Common.Size;
using User = ELOR.VKAPILib.Objects.User;

namespace Elorucov.Laney.Helpers
{
    public class MessageView : IDisposable
    {
        private Border Parent { get; set; }

        private bool isOutgoing;
        private bool containsOnlyImage;
        private bool hasAvatar;
        private bool hasAttachments;
        private bool hasGift;
        private bool hasSticker;
        private bool hasSenderName;
        private double bubbleWidth;
        private bool isFixedWide;
        private readonly double fixedWideWidth = 420;
        private readonly double fixedWidth = 360;

        public long RenderMilliseconds { get; }

        public FrameworkElement BuiltMessageUI { get; private set; }

        public MessageView(MessageViewModel msg, MessageViewModel prev, bool noBubble, Border parent, double parentWidth)
        {
            Parent = parent ?? throw new ArgumentException("parent is null!");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (msg.Action != null)
            {
                BuiltMessageUI = BuildActionMessage(msg.Action, msg.SenderId, msg.Attachments);
            }
            else
            {
                if (!msg.IsExpired)
                {
                    if (noBubble)
                    {
                        BuiltMessageUI = BuildNonBubbleMessage(msg, true);
                    }
                    else
                    {
                        if (Parent.ActualWidth == 0) isFixedWide = parentWidth >= fixedWideWidth;
                        bool hideSenderName = prev != null && prev.SenderId == msg.SenderId && prev.SentDateTime.Date == msg.SentDateTime.Date && prev.Action == null && !prev.IsExpired;
                        bool hideAvatar = hideSenderName;
                        BuiltMessageUI = BuildBubbleMessage(msg, hideAvatar, hideSenderName);
                    }
                }
                else
                {
                    BuiltMessageUI = BuildExpiredMessage();
                }
            }

            Parent.Child = BuiltMessageUI;

            sw.Stop();
            RenderMilliseconds = sw.ElapsedMilliseconds;
            if (RenderMilliseconds > 75)
                Log.General.Warn("Message UI built too long!", new ValueSet {
                    { "id", msg.Id },
                    { "ms", RenderMilliseconds }
                });
        }

        private Grid BuildBubbleMessage(MessageViewModel msg, bool hideAvatar = false, bool hideSenderName = false)
        {
            bool isChat = msg.PeerId > 2000000000;
            int senderId = VKSession.Current == null ? 0 : VKSession.Current.GroupId > 0 ? -VKSession.Current.GroupId : VKSession.Current.Id;
            isOutgoing = msg.ForceDisplayAsOutgoing || msg.SenderId == senderId;
            containsOnlyImage = msg.ContainsOnlyImage();
            hasGift = msg.HasGift();
            hasSticker = msg.HasSticker();
            hasAttachments = msg.HasAttachments(true);
            hasAvatar = hasSenderName = isChat && !isOutgoing;
            hasSenderName = !hasGift && !hasSticker;
            hasSenderName = hasAvatar && !hideSenderName;
            if (Parent.ActualWidth > 0) isFixedWide = Parent.ActualWidth >= fixedWideWidth;

            Grid g = new Grid
            {
                Margin = new Thickness(0, 4, 0, 4),
                Name = Constants.MessageUIName
            };
            if (!isOutgoing)
            {
                g.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = GridLength.Auto,
                    MaxWidth = 40
                });
                g.ColumnDefinitions.Add(new ColumnDefinition());
            }
            else
            {
                g.ColumnDefinitions.Add(new ColumnDefinition());
                g.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(0)
                });
            }

            g.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            // Show avatar in chats
            if (isChat && !isOutgoing)
            {
                Avatar ava = new Avatar
                {
                    Margin = new Thickness(8, 0, 0, 0),
                    Width = 32,
                    Height = 32,
                    VerticalAlignment = VerticalAlignment.Top,
                    ImageUri = msg.SenderAvatar,
                    DisplayName = msg.SenderName,
                    Opacity = hideAvatar ? 0 : 1
                };
                ava.Click += (a, b) => Router.ShowCard(msg.SenderId);
                ToolTipService.SetToolTip(ava, msg.SenderName);
                g.Children.Add(ava);
            }

            // Bubble container
            Grid bubblecontainer = new Grid
            {
                Name = Constants.MessageBubbleContainerName,
                Margin = new Thickness(12, 0, 12, 0),
                MinHeight = 32,
                MinWidth = 64,
                HorizontalAlignment = !isOutgoing ? HorizontalAlignment.Left : HorizontalAlignment.Right
            };

            // Story
            if (msg.IsPossibleToShowStoryControl())
            {
                Story s = msg.Attachments.First(q => q.Type == AttachmentType.Story).Story;
                StoryControl sc = new StoryControl
                {
                    Name = Constants.StoryControlName,
                    Story = s
                };

                sc.Click += (a, b) => Views.StoryViewer.Show(s, sc);
                bubblecontainer.Children.Add(sc);

                // Sticker
                Sticker ssticker = msg.Attachments.Count == 2 ? msg.Attachments.First(q => q.Type == AttachmentType.Sticker).Sticker : null;
                if (ssticker != null)
                {
                    bubblecontainer.Margin = new Thickness(12, 0, 12, 48);

                    StickerPresenter img = new StickerPresenter
                    {
                        Name = Constants.StickerOrGraffitiControlName,
                        Width = 96,
                        Height = 96,
                        Sticker = ssticker,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                        RenderTransform = new CompositeTransform
                        {
                            TranslateY = 48
                        }
                    };
                    bubblecontainer.Children.Add(img);
                }

                ContentControl sstime = GetSentTimeControl(Application.Current.Resources, msg.SentDateTime, "TimeInBubbleImageTemplate", msg.EditDateTime);
                if (ssticker != null)
                    sstime.RenderTransform = new CompositeTransform
                    {
                        TranslateY = 48
                    };
                sstime.Margin = new Thickness(4, 0, 4, 4);
                sstime.HorizontalAlignment = isOutgoing ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                bubblecontainer.Children.Add(sstime);

                // Star and unread/delete icon
                AddStateIcons(msg, bubblecontainer, isOutgoing);

                // Return formed UI for story
                if (!isOutgoing) Grid.SetColumn(bubblecontainer, 1);
                g.Children.Add(bubblecontainer);
                return g;
            }

            bubblecontainer.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            SetWidth(bubblecontainer);

            StackPanel sp = new StackPanel
            {
                CornerRadius = new CornerRadius(16)
            };

            if (Settings.MessageBubbleTemplateLoadMethod)
            {
                LoadColorResources();

                ThemeManager.ChatBackgroundChanged += async (a, b) =>
                {
                    await g.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        LoadColorResources();
                        ThemeManager.UpdateResourceColors(g, Window.Current.Content as FrameworkElement);
                    });
                };

                if (!containsOnlyImage)
                {
                    string template = isOutgoing ? "DefaultOutgoingBubbleTemplate" : "DefaultIncomingBubbleTemplate";
                    if (msg.HasGift()) template = "GiftBubbleTemplate";
                    sp = (StackPanel)(Application.Current.Resources[template] as DataTemplate).LoadContent();
                }
            }
            else
            {
                if (!containsOnlyImage)
                { // TODO: 3 emojis
                    string template = isOutgoing ? "DefaultOutgoingBubbleTemplateLegacy" : "DefaultIncomingBubbleTemplateLegacy";
                    if (msg.HasGift()) template = "GiftBubbleTemplateLegacy";
                    sp = (StackPanel)(Application.Current.Resources[template] as DataTemplate).LoadContent();
                }
            }

            // Sender name
            if (hasSenderName && !containsOnlyImage)
            {
                ContentControl snc = new ContentControl
                {
                    Template = Application.Current.Resources["SenderNameInBubbleTemplate"] as ControlTemplate,
                    Margin = new Thickness(12, 7, 12, 0),
                    Name = Constants.MsgSenderNameControlName,
                    Content = msg.SenderName
                };
                sp.Children.Add(snc);
            }

            // Reply message
            if (msg.ReplyMessage != null)
            {
                CompactMessageControl cmc = new CompactMessageControl
                {
                    Message = msg.ReplyMessage
                };

                Button rb = new Button
                {
                    Name = Constants.ReplyMessageButtonName,
                    Style = (Style)Application.Current.Resources["TransparentButtonStyle"],
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 73, 134, 204)),
                    BorderThickness = new Thickness(2, 0, 0, 0),
                    Margin = new Thickness(12, hasSenderName ? 4 : 12, 12, 0),
                    Padding = new Thickness(12, 0, 0, 0),
                    Content = cmc,
                    MaxWidth = 420,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                rb.Click += (a, b) => { ConversationViewModel.CurrentFocused.GetToMessage(msg.ReplyMessage); };

                sp.Children.Add(rb);
            }

            // Text
            if (!string.IsNullOrEmpty(msg.Text))
            {
                RichTextBlock rtb = BuildTextBlock(msg.Text, !msg.HasAttachments(true));
                rtb.Name = Constants.MsgTextBlockControlName;
                rtb.Margin = new Thickness(12, sp.Children.Count == 1 && hasSenderName ? 1 : 7, 12, 5);
                rtb.MaxWidth = 420;
                sp.Children.Add(rtb);
            }

            // Attachments
            Sticker sticker = null;
            Gift gift = null;
            List<Graffiti> graffities = new List<Graffiti>();
            List<IPreview> previews = new List<IPreview>();
            WallPost wp = null;
            WallReply wr = null;
            List<Link> links = new List<Link>();
            Market market = null;
            Poll poll = null;
            Call call = null;
            GroupCallInProgress gcall = null;
            Event evt = null;
            Story st = null;
            Curator curator = null;
            List<Document> docs = new List<Document>();
            List<Audio> audios = new List<Audio>();
            List<AudioMessage> ams = new List<AudioMessage>();
            List<Podcast> podcasts = new List<Podcast>();
            List<Attachment> unknown = new List<Attachment>();
            foreach (Attachment a in msg.Attachments)
                switch (a.Type)
                {
                    case AttachmentType.Sticker: sticker = a.Sticker; break;
                    case AttachmentType.Graffiti: graffities.Add(a.Graffiti); break;
                    case AttachmentType.Gift: gift = a.Gift; break;
                    case AttachmentType.Photo: previews.Add(a.Photo); break;
                    case AttachmentType.Video: previews.Add(a.Video); break;
                    case AttachmentType.Audio: audios.Add(a.Audio); break;
                    case AttachmentType.Podcast: podcasts.Add(a.Podcast); break;
                    case AttachmentType.Curator: curator = a.Curator; break;
                    case AttachmentType.Wall: wp = a.Wall; break;
                    case AttachmentType.WallReply: wr = a.WallReply; break;
                    case AttachmentType.Link: links.Add(a.Link); break;
                    case AttachmentType.Market: market = a.Market; break;
                    case AttachmentType.Poll: poll = a.Poll; break;
                    case AttachmentType.AudioMessage: ams.Add(a.AudioMessage); break;
                    case AttachmentType.Call: call = a.Call; break;
                    case AttachmentType.GroupCallInProgress: gcall = a.GroupCallInProgress; break;
                    case AttachmentType.Event: evt = a.Event; break;
                    case AttachmentType.Story: st = a.Story; break;
                    case AttachmentType.Document:
                        if (a.Document.Preview != null)
                            previews.Add(a.Document);
                        else
                            docs.Add(a.Document);
                        break;
                    default:
                        unknown.Add(a);
                        break;
                }

            // Gift
            if (gift != null)
            {
                GiftPresenter gp = new GiftPresenter
                {
                    Name = Constants.GiftControlName,
                    Width = bubbleWidth - 8,
                    Margin = new Thickness(4),
                    Gift = gift
                };
                sp.Children.Insert(0, gp);
            }

            // Sticker
            if (sticker != null)
            {
                double margin = containsOnlyImage ? 0 : 16;

                StickerPresenter img = new StickerPresenter
                {
                    Name = Constants.StickerOrGraffitiControlName,
                    Width = bubbleWidth - margin,
                    Height = bubbleWidth - margin,
                    Margin = new Thickness(margin / 2),
                    Sticker = sticker
                };

                sp.Children.Add(img);
            }

            // Graffiti
            if (graffities.Count > 0)
                foreach (Graffiti gr in graffities)
                {
                    Image img = new Image
                    {
                        Name = Constants.StickerOrGraffitiControlName,
                        Width = bubbleWidth - 16,
                        Margin = new Thickness(8),
                        Source = new BitmapImage(gr.Uri)
                    };
                    img.Height = img.Width / gr.Width * gr.Height;
                    sp.Children.Add(img);
                }

            // Images
            Grid ImgsGrid = null;
            if (previews.Count > 0)
            {
                ImgsGrid = AddThumbnailsGrid(previews, containsOnlyImage ? bubbleWidth : bubbleWidth - 8);
                ImgsGrid.Margin = containsOnlyImage ? new Thickness(0) : new Thickness(4);
                ImgsGrid.CornerRadius = new CornerRadius(14);
                sp.Children.Add(ImgsGrid);
                Window.Current.CoreWindow.CharacterReceived += (e, f) =>
                {
                    if (f.KeyCode == 82)
                        // R
                        ResizeThumbnailsGrid(ImgsGrid, containsOnlyImage ? bubbleWidth : bubbleWidth - 8);
                };
            }

            // Geo
            Border GeoUI = null;
            if (msg.Location != null)
            {
                GeoUI = new Border
                {
                    Width = bubbleWidth - 8,
                    Height = 240,
                    Margin = new Thickness(4, 4, 4, 24),
                    Child = BuildGeoControl(msg.Location, bubbleWidth - 8),
                    Name = Constants.LocationControlName
                };
                sp.Children.Add(GeoUI);
            }

            // Wall post
            if (wp != null)
            {
                string def = GetNameOrDefaultString(wp.FromId, wp.Text);
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Newsfeed"],
                    Title = Locale.Get("msg_attachment_wall").Capitalize(),
                    Description = def,
                    Name = Constants.WallPostControlName
                };
                dac.Click += async (a, b) => await Router.LaunchLinkAsync(new Uri($"https://m.vk.com/wall{wp.FromId}_{wp.Id}"));
                sp.Children.Add(dac);
            }

            // Wall reply
            if (wr != null)
            {
                string def = GetNameOrDefaultString(wr.OwnerId, " ");
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Newsfeed"],
                    Title = Locale.Get("msg_attachment_wallreply").Capitalize(),
                    Description = def,
                    Name = Constants.WallReplyControlName
                };
                dac.Click += async (a, b) => await Router.LaunchLinkAsync(new Uri($"https://m.vk.com/wall{wr.OwnerId}_{wr.PostId}?reply={wr.Id}"));
                sp.Children.Add(dac);
            }

            // Link
            foreach (Link lnk in links)
                if (lnk.Photo != null && lnk.Photo.Id > 0)
                {
                    sp.Children.Add(new ExtendedAttachmentControl
                    {
                        Name = Constants.LinkControlName,
                        Link = lnk.Uri,
                        Title = lnk.Title,
                        Caption = lnk.Caption,
                        Description = lnk.Description,
                        Image = lnk.Photo.Sizes[1],
                        Button = lnk.Button
                    });
                }
                else
                {
                    DefaultAttachmentControl dac = new DefaultAttachmentControl
                    {
                        IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Link"],
                        Title = lnk.Title,
                        Description = lnk.Caption,
                        Name = Constants.LinkControlName
                    };
                    dac.Click += async (a, b) => await Router.LaunchLinkAsync(lnk.Uri);
                    sp.Children.Add(dac);
                }

            // Market
            if (market != null)
            {
                Uri link = new Uri($"https://m.vk.com/product{market.OwnerId}_{market.Id}");
                sp.Children.Add(new ExtendedAttachmentControl
                {
                    Name = Constants.LinkControlName,
                    Link = link,
                    Title = market.Title,
                    Caption = $"{market.Price.Text} · {market.Category.Name}",
                    Description = market.Description,
                    Image = new PhotoSizes
                    {
                        Width = 80,
                        Height = 80,
                        Url = market.ThumbPhoto
                    },
                    Button = new LinkButton
                    {
                        Title = Locale.Get("open"),
                        Action = new LinkButtonAction
                        {
                            Url = link.AbsoluteUri
                        }
                    }
                });
            }

            // Poll
            if (poll != null)
            {
                string def = GetNameOrDefaultString(poll.AuthorId);
                string from = $"{Locale.Get("msg_attachment_poll").Capitalize()} {def}";
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Poll"],
                    Title = poll.Question,
                    Description = from,
                    Name = Constants.PollControlName
                };
                dac.Click += async (a, b) => await Router.LaunchLinkAsync(new Uri($"https://m.vk.com/poll{poll.OwnerId}_{poll.Id}"));
                sp.Children.Add(dac);
            }

            // Call
            if (call != null) AddCallInfoControl(sp, call);

            // Group call in progress
            if (gcall != null) AddCallInfoControl(sp, gcall);

            // Event
            if (evt != null)
            {
                Group eg = CacheManager.GetGroup(evt.Id);
                Uri link = new Uri($"https://vk.com/club{evt.Id}");

                sp.Children.Add(new ExtendedAttachmentControl
                {
                    Name = Constants.LinkControlName,
                    Link = link,
                    Title = eg.Name,
                    Caption = evt.Address,
                    Description = evt.Text,
                    Image = new PhotoSizes
                    {
                        Width = 80,
                        Height = 80,
                        Url = eg.Photo.AbsoluteUri
                    },
                    Button = new LinkButton
                    {
                        Title = Locale.Get("open"),
                        Action = new LinkButtonAction
                        {
                            Url = link.AbsoluteUri
                        }
                    }
                });
            }

            // Story
            if (st != null)
            {
                string def = GetNameOrDefaultString(st.OwnerId);
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Story"],
                    Title = Locale.Get("msg_attachment_story").Capitalize(),
                    Description = def,
                    Name = Constants.StoryControlName,
                    Tag = st
                };
                dac.Click += (a, b) => Views.StoryViewer.Show(st);
                sp.Children.Add(dac);
            }

            // Audios
            foreach (Audio a in audios)
            {
                AudioControl auc = new AudioControl
                {
                    Audio = a,
                    Name = Constants.AudioAttachmentControlName
                };
                auc.IsPlayButtonClicked += (b, c) => AudioPlayerViewModel.PlaySong(audios, c, msg.SenderName);
                sp.Children.Add(auc);
            }

            // Audio message
            foreach (AudioMessage am in ams)
            {
                VoiceMessageControl vmc = new VoiceMessageControl(am)
                {
                    Name = Constants.AudioMsgControlName
                };
                vmc.IsPlayButtonClicked += (a, b) => AudioPlayerViewModel.PlayVoiceMessage(ams, am, msg.SenderName);
                sp.Children.Add(vmc);
            }

            // Podcasts
            foreach (Podcast p in podcasts)
            {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl
                {
                    Title = p.Title,
                    Caption = Locale.Get("msg_attachment_podcast").Capitalize(),
                    ButtonText = Locale.Get("play"),
                    Image = p.Info.Cover.Sizes[0],
                    Name = Constants.LinkControlName
                };
                eac.ButtonClick += (a, b) => AudioPlayerViewModel.PlayPodcast(podcasts, p, msg.SenderName);
                sp.Children.Add(eac);
            }

            // Curator
            if (curator != null)
            {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl
                {
                    Title = curator.Name,
                    Caption = Locale.Get("msg_attachment_curator"),
                    Description = curator.Description,
                    ButtonText = Locale.Get("play"),
                    Image = curator.Photo.Last(),
                    Name = Constants.LinkControlName
                };
                eac.ButtonClick += async (a, b) => await Router.LaunchLinkAsync(curator.Uri);
                sp.Children.Add(eac);
            }

            // Documents
            foreach (Document d in docs)
            {
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Document"],
                    Title = d.Title,
                    Description = $"{d.Extension} · {((decimal)d.Size).ToFileSize()}",
                    Name = Constants.DocumentAttachmentControlName
                };
                dac.Click += async (a, b) => await Launcher.LaunchUriAsync(d.Uri);
                sp.Children.Add(dac);
            }

            // Unknown attachments
            foreach (Attachment a in unknown) AddUnknownAttachment(sp, a, new Thickness(12, 12, 12, 0));

            // Forwarded messages 
            if (msg.ForwardedMessages.Count > 0)
            {
                HyperlinkButton fwdlink = new HyperlinkButton
                {
                    Margin = new Thickness(12, 7, 12, 5),
                    Content = msg.ForwardedMessages.Count > 1 ? $"{msg.ForwardedMessages.Count} {Locale.GetDeclension(msg.ForwardedMessages.Count, "forwarded_msgs").ToLower()}" : Locale.Get("forwarded_msgs_nom")
                };
                fwdlink.Click += (a, b) => ShowForwardedMessages(msg.ForwardedMessages, fwdlink.Content.ToString());
                sp.Children.Add(fwdlink);
            }

            if (sp.Children.Count == 1 && sp.Children.First() is Grid grd && grd.Name == Constants.PhotosContainerControlName)
                if (grd.Width < bubblecontainer.Width - 8)
                    bubblecontainer.Width = grd.Width + 8;

            // Fix elements
            FixElements(sp.Children, !containsOnlyImage);

            // And add stackpanel with content in bubble
            bubblecontainer.Children.Add(sp);

            // Check if message is empty
            if (sp.Children.Count == 0 || sp.Children.Count == 1 && ((FrameworkElement)sp.Children[0]).Name == Constants.MsgSenderNameControlName)
            {
                TextBlock tb = new TextBlock
                {
                    Text = "(empty message)",
                    FontStyle = FontStyle.Italic,
                    Opacity = 0.7,
                    Margin = new Thickness(12, 7, 48, 9)
                };
                sp.Children.Add(tb);
            }

            // Add sent time
            bool forImages = sp.Children.Last() is Grid pc && pc.Name == Constants.PhotosContainerControlName || sp.Children.Last() is FrameworkElement gs && gs.Name == Constants.StickerOrGraffitiControlName && msg.ReplyMessage == null;

            string stemp = "TimeInBubbleImageTemplate";
            if (!forImages)
            {
                stemp = isOutgoing ? "TimeInBubbleOutgoingTemplate" : "TimeInBubbleIncomingTemplate";
                if (msg.HasGift()) stemp = "TimeInBubbleGiftTemplate";
            }
            if (!Settings.MessageBubbleTemplateLoadMethod) stemp += "Legacy";

            ContentControl stime = GetSentTimeControl(Application.Current.Resources, msg.SentDateTime, stemp, msg.EditDateTime);

            if (forImages)
            {
                double margin = containsOnlyImage ? 4 : 8;
                stime.Margin = new Thickness(0, 0, margin, margin);
            }

            bubblecontainer.Children.Add(stime);

            // Star and unread/delete icon
            AddStateIcons(msg, bubblecontainer, isOutgoing);

            // Inline keyboard
            if (msg.Keyboard != null && msg.Keyboard.Inline)
            {
                bubblecontainer.RowDefinitions.Add(new RowDefinition
                {
                    Height = GridLength.Auto
                });
                sp.CornerRadius = new CornerRadius(16, 16, 0, 0);

                ContentControl cc = new ContentControl
                {
                    Template = (ControlTemplate)Application.Current.Resources[Settings.MessageBubbleTemplateLoadMethod ? "InlineKeyboardContainerTemplate" : "InlineKeyboardContainerTemplateLegacy"],
                    Content = BuildInlineKeyboard(msg.Keyboard, msg.Id)
                };
                Grid.SetRow(cc, 1);
                bubblecontainer.Children.Add(cc);
            }

            // Carousel
            if (msg.Template != null && msg.Template.Type == BotTemplateType.Carousel)
            {
                g.RowDefinitions.Add(new RowDefinition
                {
                    Height = GridLength.Auto
                });
                Border b = new Border
                {
                    Margin = new Thickness(0, 4, 0, 0),
                    Child = BuildCarousel(msg.Template.Elements, msg.Id)
                };
                Grid.SetRow(b, 1);
                Grid.SetColumnSpan(b, 2);
                g.Children.Add(b);
            }

            // Self-destruction timer
            if (msg.TTL > 0)
            {
                ContentControl sdc = new ContentControl
                {
                    Template = (ControlTemplate)Application.Current.Resources["SelfDestructionTimerTemplate"],
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = isOutgoing ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                    RenderTransform = new TranslateTransform
                    {
                        Y = -2,
                        X = isOutgoing ? -18 : 18
                    }
                };
                bubblecontainer.Children.Add(sdc);

                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.5)
                };
                timer.Tick += (a, z) =>
                {
                    TimeSpan expiration = DateTime.Now - msg.SentDateTime;
                    int remaining = msg.TTL - Convert.ToInt32(expiration.TotalSeconds);
                    if (remaining > 0)
                    {
                        sdc.Content = TimeSpan.FromSeconds(remaining).ToNormalString();
                    }
                    else
                    {
                        timer.Stop();
                        BuiltMessageUI = BuildExpiredMessage();
                        Parent.Child = BuiltMessageUI;
                    }
                };
                timer.Start();
            }

            // Size changed
            Parent.SizeChanged += (a, b) =>
            {
                double w = b.NewSize.Width;
                bool isFixedWideNew = b.NewSize.Width >= fixedWideWidth;
                if (isFixedWide == isFixedWideNew) return;
                isFixedWide = isFixedWideNew;
                SetWidth(bubblecontainer);
                if (ImgsGrid != null) ResizeThumbnailsGrid(ImgsGrid, containsOnlyImage ? bubbleWidth : bubbleWidth - 8);
                if (GeoUI != null) GeoUI.Width = bubbleWidth - 8;
            };

            // LP events
            if (VKSession.Current != null)
            {
                if (isOutgoing)
                    VKSession.Current.LongPoll.OutgoingMessagesRead += (a, b) =>
                    {
                        if (msg.PeerId != b.Item1 || msg.Id > b.Item2) return;
                        msg.State = MessageVMState.Read;
                    };
                else
                    VKSession.Current.LongPoll.IncomingMessagesRead += (a, b) =>
                    {
                        if (msg.PeerId != b.Item1 || msg.Id > b.Item2) return;
                        msg.State = MessageVMState.Read;
                    };
            }

            // Return formed UI
            if (!isOutgoing) Grid.SetColumn(bubblecontainer, 1);
            g.Children.Add(bubblecontainer);
            return g;
        }

        private Grid BuildNonBubbleMessage(MessageViewModel msg, bool showSentDay, bool hideSenderInfo = false)
        {
            Grid g = new Grid
            {
                Margin = new Thickness(12, 6, 12, 6),
                MaxWidth = 960
            };
            g.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            g.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            g.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto,
                MaxWidth = 40
            });
            g.ColumnDefinitions.Add(new ColumnDefinition());

            // Avatar
            Avatar ava = new Avatar
            {
                Margin = new Thickness(0, 0, 8, 0),
                Width = 32,
                Height = 32,
                VerticalAlignment = VerticalAlignment.Top,
                ImageUri = msg.SenderAvatar,
                DisplayName = msg.SenderName,
                Visibility = hideSenderInfo ? Visibility.Collapsed : Visibility.Visible
            };
            ToolTipService.SetToolTip(ava, msg.SenderName);
            g.Children.Add(ava);

            // Sender info and sent time
            StackPanel sinfo = new StackPanel
            {
                Visibility = hideSenderInfo ? Visibility.Collapsed : Visibility.Visible
            };
            sinfo.Children.Add(new HyperlinkButton
            {
                FontWeight = new FontWeight
                {
                    Weight = 600
                },
                Padding = new Thickness(0),
                FontSize = 13,
                ContentTemplate = (DataTemplate)Application.Current.Resources["TextLikeHyperlinkBtnTemplate"],
                Content = msg.SenderName
            });
            sinfo.Children.Add(new TextBlock
            {
                FontWeight = new FontWeight
                {
                    Weight = 400
                },
                FontSize = 13,
                Opacity = 0.8,
                Text = !showSentDay ? msg.SentDateTime.ToShortTimeString() : msg.SentDateTime.ToTimeAndDate()
            });
            sinfo.Margin = new Thickness(0, -3, 0, 0);
            Grid.SetColumn(sinfo, 1);
            g.Children.Add(sinfo);

            // Message content
            StackPanel sp = new StackPanel();
            Grid.SetRow(sp, 1);
            Grid.SetColumnSpan(sp, 2);

            // Text
            if (!string.IsNullOrEmpty(msg.Text))
            {
                RichTextBlock rtb = BuildTextBlock(msg.Text, !msg.HasAttachments(true));
                rtb.Name = Constants.MsgTextBlockControlName;
                rtb.Margin = new Thickness(0, 6, 0, 6);
                rtb.FontSize = 16;
                sp.Children.Add(rtb);
            }

            // Attachments
            Sticker sticker = null;
            Gift gift = null;
            List<Graffiti> graffities = new List<Graffiti>();
            List<IPreview> previews = new List<IPreview>();
            WallPost wp = null;
            WallReply wr = null;
            List<Link> links = new List<Link>();
            Market market = null;
            Poll poll = null;
            Call call = null;
            GroupCallInProgress gcall = null;
            Event evt = null;
            Story st = null;
            Curator curator = null;
            List<Document> docs = new List<Document>();
            List<Audio> audios = new List<Audio>();
            List<AudioMessage> ams = new List<AudioMessage>();
            List<Podcast> podcasts = new List<Podcast>();
            List<Attachment> unknown = new List<Attachment>();
            foreach (Attachment a in msg.Attachments)
                switch (a.Type)
                {
                    case AttachmentType.Sticker: sticker = a.Sticker; break;
                    case AttachmentType.Graffiti: graffities.Add(a.Graffiti); break;
                    case AttachmentType.Gift: gift = a.Gift; break;
                    case AttachmentType.Photo: previews.Add(a.Photo); break;
                    case AttachmentType.Video: previews.Add(a.Video); break;
                    case AttachmentType.Audio: audios.Add(a.Audio); break;
                    case AttachmentType.Podcast: podcasts.Add(a.Podcast); break;
                    case AttachmentType.Curator: curator = a.Curator; break;
                    case AttachmentType.Wall: wp = a.Wall; break;
                    case AttachmentType.WallReply: wr = a.WallReply; break;
                    case AttachmentType.Link: links.Add(a.Link); break;
                    case AttachmentType.Market: market = a.Market; break;
                    case AttachmentType.Poll: poll = a.Poll; break;
                    case AttachmentType.AudioMessage: ams.Add(a.AudioMessage); break;
                    case AttachmentType.Call: call = a.Call; break;
                    case AttachmentType.GroupCallInProgress: gcall = a.GroupCallInProgress; break;
                    case AttachmentType.Event: evt = a.Event; break;
                    case AttachmentType.Story: st = a.Story; break;
                    case AttachmentType.Document:
                        if (a.Document.Preview != null)
                            previews.Add(a.Document);
                        else
                            docs.Add(a.Document);
                        break;
                    default:
                        unknown.Add(a);
                        break;
                }

            // Gift
            if (gift != null)
            {
                GiftPresenter gp = new GiftPresenter
                {
                    Name = Constants.GiftControlName,
                    Width = 176,
                    Margin = new Thickness(0, 6, 0, 6),
                    Gift = gift
                };
                sp.Children.Insert(0, gp);
            }

            // Sticker
            if (sticker != null)
            {
                StickerPresenter img = new StickerPresenter
                {
                    Name = Constants.StickerOrGraffitiControlName,
                    Width = 168,
                    Height = 168,
                    Margin = new Thickness(0, 6, 0, 6),
                    Sticker = sticker
                };
                sp.Children.Add(img);
            }

            // Graffiti
            if (graffities.Count > 0)
                foreach (Graffiti gr in graffities)
                {
                    Image img = new Image
                    {
                        Name = Constants.StickerOrGraffitiControlName,
                        Width = 168,
                        Margin = new Thickness(0, 6, 0, 6),
                        Source = new BitmapImage(gr.Uri)
                    };
                    img.Height = img.Width / gr.Width * gr.Height;
                    sp.Children.Add(img);
                }

            // Images
            if (previews.Count > 0)
            {
                Grid tg = AddThumbnailsGrid(previews, 336);
                tg.Margin = new Thickness(0, 6, 0, 6);
                sp.Children.Add(tg);
            }

            // Geo
            if (msg.Location != null)
                sp.Children.Add(new Border
                {
                    Width = 336,
                    Margin = new Thickness(0, 6, 0, 6),
                    Child = BuildGeoControl(msg.Location, 336),
                    Name = Constants.LocationControlName
                });

            // Wall post
            if (wp != null)
            {
                string def = GetNameOrDefaultString(wp.FromId, wp.Text);
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Newsfeed"],
                    Title = Locale.Get("msg_attachment_wall").Capitalize(),
                    Description = def,
                    Name = Constants.WallPostControlName
                };
                dac.Click += async (a, b) => await Router.LaunchLinkAsync(new Uri($"https://m.vk.com/wall{wp.FromId}_{wp.Id}"));
                sp.Children.Add(dac);
            }

            // Wall reply
            if (wr != null)
            {
                string def = GetNameOrDefaultString(wr.OwnerId, " ");
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Newsfeed"],
                    Title = Locale.Get("msg_attachment_wallreply").Capitalize(),
                    Description = def,
                    Name = Constants.WallReplyControlName
                };
                dac.Click += async (a, b) => await Router.LaunchLinkAsync(new Uri($"https://m.vk.com/wall{wr.OwnerId}_{wr.PostId}?reply={wr.Id}"));
                sp.Children.Add(dac);
            }

            // Link
            foreach (Link lnk in links)
                if (lnk.Photo != null && lnk.Photo.Id > 0)
                {
                    sp.Children.Add(new ExtendedAttachmentControl
                    {
                        Name = Constants.LinkControlName,
                        Link = lnk.Uri,
                        Title = lnk.Title,
                        Caption = lnk.Caption,
                        Description = lnk.Description,
                        Image = lnk.Photo.Sizes[1],
                        Button = lnk.Button,
                        Margin = new Thickness(0, 6, 0, 6)
                    });
                }
                else
                {
                    DefaultAttachmentControl dac = new DefaultAttachmentControl
                    {
                        Margin = new Thickness(0, 6, 0, 6),
                        IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Link"],
                        Title = lnk.Title,
                        Description = lnk.Caption,
                        Name = Constants.LinkControlName
                    };
                    dac.Click += async (a, b) => await Launcher.LaunchUriAsync(lnk.Uri);
                    sp.Children.Add(BuildDefaultAttachmentControl("Icon24Link", lnk.Title, lnk.Caption, Constants.LinkControlName));
                }

            // Market
            if (market != null)
            {
                Uri link = new Uri($"https://m.vk.com/product{market.OwnerId}_{market.Id}");
                sp.Children.Add(new ExtendedAttachmentControl
                {
                    Name = Constants.LinkControlName,
                    Link = link,
                    Title = market.Title,
                    Caption = $"{market.Price.Text} · {market.Category.Name}",
                    Description = market.Description,
                    Image = new PhotoSizes
                    {
                        Width = 80,
                        Height = 80,
                        Url = market.ThumbPhoto
                    },
                    Button = new LinkButton
                    {
                        Title = Locale.Get("open"),
                        Action = new LinkButtonAction
                        {
                            Url = link.AbsoluteUri
                        }
                    },
                    Margin = new Thickness(0, 6, 0, 6)
                });
            }

            // Poll
            if (poll != null)
            {
                string def = GetNameOrDefaultString(poll.AuthorId);
                string from = $"{Locale.Get("msg_attachment_poll").Capitalize()} {def}";
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Poll"],
                    Title = poll.Question,
                    Description = from,
                    Name = Constants.PollControlName
                };
                dac.Click += async (a, b) => await Router.LaunchLinkAsync(new Uri($"https://m.vk.com/poll{poll.OwnerId}_{poll.Id}"));
                sp.Children.Add(dac);
            }

            // Call
            if (call != null) AddCallInfoControl(sp, call);

            // Group call in progress
            if (gcall != null) AddCallInfoControl(sp, gcall);

            // Event
            if (evt != null)
            {
                Group eg = CacheManager.GetGroup(evt.Id);
                Uri link = new Uri($"https://vk.com/club{evt.Id}");

                sp.Children.Add(new ExtendedAttachmentControl
                {
                    Name = Constants.LinkControlName,
                    Link = link,
                    Title = eg.Name,
                    Caption = evt.Address,
                    Description = evt.Text,
                    Image = new PhotoSizes
                    {
                        Width = 80,
                        Height = 80,
                        Url = eg.Photo.AbsoluteUri
                    },
                    Button = new LinkButton
                    {
                        Title = Locale.Get("open"),
                        Action = new LinkButtonAction
                        {
                            Url = link.AbsoluteUri
                        }
                    },
                    Margin = new Thickness(0, 6, 0, 6)
                });
            }

            // Story
            if (st != null)
            {
                string def = GetNameOrDefaultString(st.OwnerId);
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Story"],
                    Title = Locale.Get("msg_attachment_story").Capitalize(),
                    Description = def,
                    Name = Constants.StoryControlName,
                    Tag = st
                };
                dac.Click += (a, b) => Views.StoryViewer.Show(st);
                sp.Children.Add(dac);
            }

            // Audios
            foreach (Audio a in audios)
            {
                AudioControl auc = new AudioControl
                {
                    Audio = a,
                    Name = Constants.AudioAttachmentControlName,
                    Margin = new Thickness(0, 6, 0, 6)
                };
                auc.IsPlayButtonClicked += (b, c) => AudioPlayerViewModel.PlaySong(audios, c, msg.SenderName);
                sp.Children.Add(auc);
            }

            // Audio message
            foreach (AudioMessage am in ams)
            {
                VoiceMessageControl vmc = new VoiceMessageControl(am)
                {
                    Name = Constants.AudioMsgControlName,
                    Margin = new Thickness(0, 6, 0, 6)
                };
                vmc.IsPlayButtonClicked += (a, b) => AudioPlayerViewModel.PlayVoiceMessage(ams, am, msg.SenderName);
                sp.Children.Add(vmc);
            }

            // Podcasts
            foreach (Podcast p in podcasts)
            {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl
                {
                    Title = p.Title,
                    Caption = Locale.Get("msg_attachment_podcast").Capitalize(),
                    ButtonText = Locale.Get("play"),
                    Image = p.Info.Cover.Sizes[0],
                    Name = Constants.LinkControlName,
                    Margin = new Thickness(0, 6, 0, 6)
                };
                eac.ButtonClick += (a, b) => AudioPlayerViewModel.PlayPodcast(podcasts, p, msg.SenderName);
                sp.Children.Add(eac);
            }

            // Curator
            if (curator != null)
            {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl
                {
                    Title = curator.Name,
                    Caption = Locale.Get("msg_attachment_curator"),
                    Description = curator.Description,
                    ButtonText = Locale.Get("play"),
                    Image = curator.Photo.Last(),
                    Name = Constants.LinkControlName,
                    Margin = new Thickness(0, 6, 0, 6)
                };
                eac.ButtonClick += async (a, b) => await Router.LaunchLinkAsync(curator.Uri);
                sp.Children.Add(eac);
            }

            // Documents
            foreach (Document d in docs)
            {
                sp.Children.Add(BuildDefaultAttachmentControl("Icon24Document", d.Title, d.Extension, Constants.DocumentAttachmentControlName, async (a, b) => await Launcher.LaunchUriAsync(d.Uri)));
                DefaultAttachmentControl dac = new DefaultAttachmentControl
                {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Document"],
                    Title = d.Title,
                    Description = $"{d.Extension} · {((decimal)d.Size).ToFileSize()}",
                    Name = Constants.DocumentAttachmentControlName
                };
                dac.Click += async (a, b) => await Launcher.LaunchUriAsync(d.Uri);
                sp.Children.Add(dac);
            }

            // Unknown attachments
            foreach (Attachment a in unknown) AddUnknownAttachment(sp, a, new Thickness(0, 6, 0, 6));

            // Forwarded messages 
            if (msg.ForwardedMessages.Count > 0)
            {
                HyperlinkButton fwdlink = new HyperlinkButton
                {
                    Margin = new Thickness(0, 7, 0, 5),
                    Content = msg.ForwardedMessages.Count > 1 ? $"{msg.ForwardedMessages.Count} {Locale.GetDeclension(msg.ForwardedMessages.Count, "forwarded_msgs").ToLower()}" : Locale.Get("forwarded_msgs_nom")
                };
                fwdlink.Click += (a, b) => ShowForwardedMessages(msg.ForwardedMessages, fwdlink.Content.ToString());
                sp.Children.Add(fwdlink);
            }

            // Reply message 
            if (msg.ReplyMessage != null)
            {
                HyperlinkButton replink = new HyperlinkButton
                {
                    Margin = new Thickness(0, 0, 0, 6),
                    Content = Locale.Get("reply_msg_link")
                };
                replink.Click += (a, b) => ShowForwardedMessages(new List<MessageViewModel> {
                    msg.ReplyMessage
                }, replink.Content.ToString());
                sp.Children.Add(replink);
            }

            g.Children.Add(sp);
            return g;
        }

        private FrameworkElement BuildActionMessage(Action action, int fromId, ThreadSafeObservableCollection<Attachment> attachments)
        {
            ActionMessage msg = new ActionMessage(action, fromId);

            string xaml = "<TextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" TextWrapping=\"Wrap\" TextAlignment=\"Center\" Foreground=\"{ThemeResource VKTextPrimaryBrush}\" FontSize=\"12\">" + $"<Hyperlink FontWeight=\"SemiBold\" Foreground=\"{{ThemeResource VKAccentBrush}}\" UnderlineStyle=\"None\">{msg.InitiatorDisplayName}</Hyperlink>\n" + $"<Run>{msg.ActionText}</Run>";

            if (!string.IsNullOrEmpty(msg.ObjectDisplayName) && msg.ObjectId != 0) xaml += $"\n<Hyperlink FontWeight=\"SemiBold\" Foreground=\"{{ThemeResource VKAccentBrush}}\" UnderlineStyle=\"None\">{msg.ObjectDisplayName}</Hyperlink>";
            if (!string.IsNullOrEmpty(msg.Suffix)) xaml += $"\n<Run>{msg.Suffix}</Run>";
            xaml += "</TextBlock>";

            TextBlock tb = (TextBlock)XamlReader.Load(xaml);
            Debug.WriteLine($"tb inlines: {tb.Inlines.Count}; smtype: {action.Type}");
            (tb.Inlines[0] as Hyperlink).Click += (a, b) => { Router.ShowCard(msg.InitiatorId); };
            if (tb.Inlines.Count > 4 && tb.Inlines[4] is Hyperlink ol)
                ol.Click += (a, b) =>
                {
                    switch (msg.ObjectType)
                    {
                        case ActionObjectType.Member: Router.ShowCard(msg.ObjectId); break;
                        case ActionObjectType.ConversationMessage: ConversationViewModel.CurrentFocused.GetToMessageByConvMsgId(msg.ObjectId, msg.MessageText, tb); break;
                    }
                };

            ContentControl cc = new ContentControl
            {
                Template = (ControlTemplate)Application.Current.Resources["ServiceMessageTemplate"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = tb
            };

            StackPanel sp = new StackPanel();
            sp.Children.Add(cc);

            if (attachments.Count == 1 && attachments[0].Photo != null)
            {
                ClickableImage ci = GetConversationAvatarThumbnail(attachments[0].Photo);
                sp.Children.Add(ci);
            }

            return sp;
        }

        private FrameworkElement BuildExpiredMessage()
        {
            Button b = (Button)(Application.Current.Resources["DisappearedMessageTemplate"] as DataTemplate).LoadContent();
            b.Click += async (a, c) =>
            {
                await new Alert
                {
                    Header = Locale.Get("msg_disappeared"),
                    Text = Locale.Get("msg_disappeared_desc"),
                    PrimaryButtonText = Locale.Get("close"),
                }.ShowAsync();
            };
            return b;
        }

        #region Attachment controls

        public static ClickableImage GetConversationAvatarThumbnail(Photo p)
        {
            Size size = p.PreviewImageSize;
            double w = 180;
            double h = 180;
            if (size.Width > size.Height)
            {
                w = 180;
                h = w / size.Width * size.Height;
            }
            else if (size.Width < size.Height)
            {
                h = 320;
                w = h / size.Height * size.Width;
            }

            ClickableImage t = new ClickableImage
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
                Width = w,
                Height = h,
                Image = p
            };
            t.Click += (a, b) =>
            {
                List<AttachmentBase> images = new List<AttachmentBase> {
                    p
                };
                ViewManagement.OpenPhotoViewer(images, images.First());
            };
            return t;
        }

        private DefaultAttachmentControl BuildDefaultAttachmentControl(string iconTemplateKey, string title, string description, string controlName, RoutedEventHandler click = null)
        {
            DefaultAttachmentControl dac = new DefaultAttachmentControl
            {
                IconTemplate = (DataTemplate)Application.Current.Resources[iconTemplateKey],
                Title = title,
                Description = description,
                Name = controlName,
                Margin = new Thickness(0, 6, 0, 6)
            };
            if (click != null) dac.Click += click;
            return dac;
        }

        private static void AddCallInfoControl(StackPanel sp, Call call)
        {
            string title = Locale.Get(call.InitiatorId == VKSession.Current.SessionId ? "outgoing_call" : "incoming_call");
            string subtitle = string.Empty;

            if (call.Participants != null)
            {
                int c = call.Participants.Count;
                subtitle = $"{string.Format(Locale.GetDeclensionForFormat(c, "chatinfo_subtitle"), c)}. ";
            }

            switch (call.State)
            {
                case "reached": subtitle += call.Duration.ToString(call.Duration.Hours > 0 ? @"h\:mm\:ss" : @"m\:ss"); break;
                case "canceled_by_receiver": subtitle += Locale.Get(call.InitiatorId == VKSession.Current.SessionId ? "call_declined" : "call_canceled"); break;
                case "canceled_by_initiator": subtitle += Locale.Get(call.InitiatorId == VKSession.Current.SessionId ? "call_canceled" : "call_missed"); break;
                default: subtitle += call.State; break;
            }

            sp.Children.Add(new DefaultAttachmentControl
            {
                IconTemplate = (DataTemplate)Application.Current.Resources[call.Video ? "Icon24Videocam" : "Icon24Phone"],
                Title = call.ReceiverId > 2000000000 ? Locale.Get("msg_attachment_group_call_in_progress").Capitalize() : title,
                Description = subtitle,
                Name = Constants.CallControlName
            });
        }

        private static void AddCallInfoControl(StackPanel sp, GroupCallInProgress call)
        {
            string subtitle = string.Empty;

            if (call.Participants != null)
            {
                int c = call.Participants.Count;
                subtitle = $"{string.Format(Locale.GetDeclensionForFormat(c, "chatinfo_subtitle"), c)}";
            }

            sp.Children.Add(new ExtendedAttachmentControl
            {
                Title = Locale.Get("msg_attachment_group_call_in_progress").Capitalize(),
                Caption = subtitle,
                Name = Constants.CallControlName
            });
        }

        private static Control BuildGeoControl(Geo geo, double width)
        {
            string lat = geo.Coordinates.Latitude.ToString().Replace(',', '.');
            string lon = geo.Coordinates.Longitude.ToString().Replace(',', '.');

            if (Settings.UseYandexMaps)
            {
                BitmapImage bimg = new BitmapImage();
                bimg.UriSource = new Uri($"https://static-maps.yandex.ru/1.x/?ll={lon},{lat}&size={width},240&z=15&l=pmap&pt={lon},{lat}");

                Button b = new Button
                {
                    Style = (Style)Application.Current.Resources["TransparentButtonStyle"],
                    Padding = new Thickness(0),
                    Content = new Image
                    {
                        Stretch = Stretch.UniformToFill,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Source = bimg
                    }
                };
                b.Click += async (c, d) => await Launcher.LaunchUriAsync(new Uri($"bingmaps:?cp={lat}~{lon}"));
                return b;
            }

            MapControl m = new MapControl();
            Geopoint g = new Geopoint(new BasicGeoposition
            {
                Latitude = geo.Coordinates.Latitude,
                Longitude = geo.Coordinates.Longitude
            });
            m.ActualCamera.Location = g;
            m.ActualCamera.Pitch = 0;
            m.ActualCamera.Heading = 0;
            m.Center = g;
            m.MapServiceToken = "UiW7WOb2oHEe9yQSEVpg~53Z_RFeFeK1NCE1sIE_-Kw~Apg8QKsKCmrLWTa6BttNQVkUvaDQJjwCmgv8814SFKD2SmHWNIOJEUKpp8MDO-GP";
            m.PanInteractionMode = MapPanInteractionMode.Disabled;
            m.RotateInteractionMode = MapInteractionMode.Disabled;
            m.TiltInteractionMode = MapInteractionMode.Disabled;
            m.ZoomInteractionMode = MapInteractionMode.Disabled;
            m.ZoomLevel = 15;
            m.MapElementClick += async (c, d) => await Launcher.LaunchUriAsync(new Uri($"bingmaps:?cp={lat}~{lon}"));
            return m;
        }

        private void AddUnknownAttachment(StackPanel sp, Attachment attachment, Thickness margin)
        {
            sp.Children.Add(new DefaultAttachmentControl
            {
                IconTemplate = (DataTemplate)Application.Current.Resources["Icon24LogoVk"],
                Title = "Unknown attachment",
                Description = $"Type: {attachment.TypeString}",
                Name = Constants.StandartAttachmentControlName,
                Margin = margin
            });
        }

        #endregion

        #region Text and other messageui controls

        private DataTemplate GetIconByMessageState(MessageVMState state)
        {
            switch (state)
            {
                case MessageVMState.Unread: return (DataTemplate)Application.Current.Resources["UnreadMessageIconTemplate"];
                case MessageVMState.Sending: return (DataTemplate)Application.Current.Resources["SendingMessageIconTemplate"];
                case MessageVMState.Deleted: return (DataTemplate)Application.Current.Resources["DeletedMessageIconTemplate"];
                case MessageVMState.Failed: return (DataTemplate)Application.Current.Resources["FailedMessageIconTemplate"];
            }

            return null;
        }

        private ContentControl GetSentTimeControl(ResourceDictionary dict, DateTime dateTime, string template, DateTime? editDateTime)
        {
            TextBlock stx = new TextBlock
            {
                FontSize = 12
            };
            if (editDateTime != null)
                stx.Inlines.Add(new Run
                {
                    Text = " ",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 12
                });
            stx.Inlines.Add(new Run
            {
                Text = dateTime.ToString(@"H\:mm")
            });

            ContentControl stime = new ContentControl
            {
                Name = "SentTime",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Template = dict.ContainsKey(template) ? dict[template] as ControlTemplate : Application.Current.Resources[template] as ControlTemplate,
                Content = stx
            };
            return stime;
        }

        private void AddStateIcons(MessageViewModel msg, Grid bubblecontainer, bool isOutgoing)
        {
            // Star and unread/delete icon
            ContentPresenter cp = new ContentPresenter
            {
                ContentTemplate = GetIconByMessageState(msg.State),
                Width = 12,
                Height = 12
            };

            Button stsb = new Button
            {
                Content = cp,
                Style = Application.Current.Resources["TransparentButtonStyle"] as Style,
                HorizontalAlignment = !isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                RenderTransform = new CompositeTransform
                {
                    TranslateX = !isOutgoing ? 12 : -12
                },
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent),
                Width = 12,
                Height = 12
            };
            stsb.Click += (a, b) =>
            {
                if (msg.State == MessageVMState.Failed)
                {
                    MenuFlyout mf = new MenuFlyout();
                    CellButton retry = new CellButton
                    {
                        Icon = VKIconName.Icon28RefreshOutline,
                        Text = Locale.Get("retry")
                    };
                    CellButton delete = new CellButton
                    {
                        Icon = VKIconName.Icon28DeleteOutline,
                        Text = Locale.Get("msg_ctx_delete")
                    };

                    retry.Click += (c, d) => msg.SendOrEditMessage();
                    delete.Click += (c, d) => VKSession.Current.SessionBase.SelectedConversation.Messages.RemoveMessage(msg);

                    mf.Items.Add(retry);
                    mf.Items.Add(delete);

                    mf.ShowAt(stsb);
                }
            };
            bubblecontainer.Children.Add(stsb);

            ContentPresenter strb = new ContentPresenter
            {
                Width = 12,
                Height = 12,
                HorizontalAlignment = !isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransform = new CompositeTransform
                {
                    TranslateX = !isOutgoing ? 12 : -12
                },
                ContentTemplate = (DataTemplate)Application.Current.Resources["ImportantMessageIconTemplate"],
                Visibility = msg.IsImportant ? Visibility.Visible : Visibility.Collapsed
            };
            bubblecontainer.Children.Add(strb);

            // Update some UI when changed property in message
            msg.PropertyChanged += (a, b) =>
            {
                switch (b.PropertyName)
                {
                    case nameof(msg.State): SynchronizationContext.Current.Post(o => cp.ContentTemplate = GetIconByMessageState(msg.State), null); break;
                    case nameof(msg.IsImportant): strb.Visibility = msg.IsImportant ? Visibility.Visible : Visibility.Collapsed; break;
                }
            };
        }

        private RichTextBlock BuildTextBlock(string text, bool isTimeInline = false)
        {
            RichTextBlock rtb = new RichTextBlock();
            rtb.FontSize = Settings.MessageFontSize;
            rtb.LineHeight = GetLineHeightByFontSize(rtb.FontSize);
            rtb.IsTextSelectionEnabled = true;
            rtb.HorizontalAlignment = HorizontalAlignment.Left;
            rtb.IsRightTapEnabled = false;

            ThemeManager.MessageFontSizeChanged += async (a, b) => await Parent.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                rtb.FontSize = b;
                rtb.LineHeight = GetLineHeightByFontSize(b);
            });

            VKTextParser.SetText(text, rtb, async s => { await Router.LaunchLinkAsync(s); });

            if (isTimeInline)
            {
                InlineUIContainer iuc = new InlineUIContainer();
                Border b = new Border();
                b.Width = 48;
                b.Height = 15;

                iuc.Child = b;
                (rtb.Blocks[0] as Paragraph).Inlines.Add(iuc);
            }

            return rtb;
        }

        private double GetLineHeightByFontSize(double fontSize)
        {
            switch (fontSize)
            {
                case 17: return 22;
                case 16: return 20;
                case 15: return 20;
                case 14: return 18;
                case 13: return 16;
                default: return fontSize * 1.3;
            }
        }

        private Grid AddThumbnailsGrid(List<IPreview> previews, double width)
        {
            Grid g = new Grid();
            if (previews.Count > 1)
            {
                Canvas c = new Canvas();
                List<Windows.Foundation.Size> sizes = previews.Select(p => p.PreviewImageSize).ToList().ToWinSize();
                Tuple<List<Rect>, Windows.Foundation.Size> layoutResult = RectangleLayoutHelper.CreateLayout(new Windows.Foundation.Size(width, width), sizes, 4);

                List<Rect> layout = layoutResult.Item1;
                Windows.Foundation.Size layoutSize = layoutResult.Item2;
                c.Width = g.Width = layoutSize.Width;
                c.Height = g.Height = layoutSize.Height;

                List<AttachmentBase> images = new List<AttachmentBase>();
                previews.Where(k => k is Photo || k is Document d && (d.Type == DocumentType.Image || d.Type == DocumentType.GIF)).ToList().ForEach(i => images.Add((AttachmentBase)i));

                for (int i = 0; i < layout.Count; i++)
                {
                    ClickableImage thmb = new ClickableImage();
                    thmb.Width = layout[i].Width;
                    thmb.Height = layout[i].Height;
                    thmb.Image = previews[i];
                    if (previews[i] is Photo p)
                        thmb.Click += (a, b) => ViewManagement.OpenPhotoViewer(images, p);
                    else if (previews[i] is Video v)
                        thmb.Click += async (a, b) => await ViewManagement.OpenVideoPlayer(v);
                    else if (previews[i] is Document d)
                        thmb.Click += async (a, b) =>
                        {
                            if (d.Type == DocumentType.Image || d.Type == DocumentType.GIF)
                                ViewManagement.OpenPhotoViewer(images, d);
                            else
                                await Launcher.LaunchUriAsync(d.Uri);
                        };
                    Canvas.SetLeft(thmb, layout[i].Left);
                    Canvas.SetTop(thmb, layout[i].Top);
                    c.Children.Add(thmb);
                }

                g.Children.Add(c);
            }
            else
            {
                var preview = previews[0];
                bool oldFormat = preview.PreviewImageSize.Width == 0 && preview.PreviewImageSize.Height == 0;
                double height = oldFormat ? width : width / preview.PreviewImageSize.Width * preview.PreviewImageSize.Height;
                double maxheight = width / 9 * 16;
                ClickableImage thmb = new ClickableImage();
                thmb.Width = width;
                thmb.Height = height > maxheight ? maxheight : height;
                thmb.Image = previews[0];
                thmb.Click += async (a, b) =>
                {
                    if (previews[0] is Photo || previews[0] is Document d && (d.Type == DocumentType.Image || d.Type == DocumentType.GIF))
                    {
                        List<AttachmentBase> images = new List<AttachmentBase> {
                            (AttachmentBase) previews[0]
                        };
                        ViewManagement.OpenPhotoViewer(images, images.First());
                    }
                    else if (previews[0] is Video v)
                    {
                        await ViewManagement.OpenVideoPlayer(v);
                    }
                };
                g.Children.Add(thmb);
            }

            Debug.WriteLine($"PhotoLayout: count: {previews.Count}, children: {g.Children.Count}");
            g.Name = Constants.PhotosContainerControlName;
            return g;
        }

        private void ResizeThumbnailsGrid(Grid g, double width)
        {
            if (g.Children.Count == 0) return;
            switch (g.Children.First())
            {
                case Canvas c:
                {
                    List<Windows.Foundation.Size> sizes = c.Children.Select(z => ((ClickableImage)z).Image.PreviewImageSize).ToList().ToWinSize();
                    Tuple<List<Rect>, Windows.Foundation.Size> layoutResult = RectangleLayoutHelper.CreateLayout(new Windows.Foundation.Size(width, width), sizes, 4);

                    List<Rect> layout = layoutResult.Item1;
                    Windows.Foundation.Size layoutSize = layoutResult.Item2;
                    c.Width = g.Width = layoutSize.Width;
                    c.Height = g.Height = layoutSize.Height;

                    for (int i = 0; i < c.Children.Count; i++)
                    {
                        ClickableImage thmb = c.Children[i] as ClickableImage;
                        thmb.Width = layout[i].Width;
                        thmb.Height = layout[i].Height;
                        Canvas.SetLeft(thmb, layout[i].Left);
                        Canvas.SetTop(thmb, layout[i].Top);
                    }

                    break;
                }
                case ClickableImage thmb:
                {
                    double height = width / thmb.Image.PreviewImageSize.Width * thmb.Image.PreviewImageSize.Height;
                    double maxheight = width / 9 * 16;
                    thmb.Width = width;
                    thmb.Height = height > maxheight ? maxheight : height;
                    break;
                }
            }
        }

        private static void ShowForwardedMessages(IEnumerable<MessageViewModel> forwardedMessages, string title)
        {
            MessagesModal modal = new MessagesModal(forwardedMessages, title);
            modal.Show();
        }

        private BotKeyboardControl BuildInlineKeyboard(BotKeyboard bk, int ownerMessageId)
        {
            BotKeyboardControl bkc = new BotKeyboardControl();
            bkc.Margin = new Thickness(6, 0, 6, 6);
            bkc.Keyboard = bk;
            bkc.OwnerMessageId = ownerMessageId;
            bkc.ButtonClicked += async (a, b) => { await MessageKeyboardHelper.DoAction(b, ownerMessageId); };
            return bkc;
        }

        private ScrollViewer BuildCarousel(List<CarouselElement> elements, int ownerMessageId)
        {
            ScrollViewer sv = new ScrollViewer
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                VerticalScrollMode = ScrollMode.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollMode = ScrollMode.Enabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(12, 0, 0, 0)
            };

            foreach (CarouselElement el in elements)
            {
                List<Photo> cphotos = new List<Photo> {
                    el.Photo
                };
                CarouselElementControl bce = new CarouselElementControl();
                bce.Margin = new Thickness(0, 4, 4, 0);
                bce.OwnerMessageId = ownerMessageId;
                bce.Click += async (a, b) =>
                {
                    switch (b.Type)
                    {
                        case BotButtonType.OpenLink: await Launcher.LaunchUriAsync(b.LinkUri); break;
                        case BotButtonType.OpenPhoto: ViewManagement.OpenPhotoViewer(cphotos.Cast<AttachmentBase>().ToList(), el.Photo); break;
                    }
                };
                bce.ElementButtonClick += async (a, b) => { await MessageKeyboardHelper.DoAction(b, ownerMessageId); };
                bce.Element = el;
                sp.Children.Add(bce);
            }

            sv.Content = sp;
            return sv;
        }

        private string GetNameOrDefaultString(int ownerId, string defaultStr = null)
        {
            if (!string.IsNullOrEmpty(defaultStr)) return defaultStr;
            string from = "";
            if (ownerId > 0)
            {
                User u = CacheManager.GetUser(ownerId);
                from = u != null ? $"{Locale.Get("from")} {u.FirstNameGen} {u.LastNameGen}" : "";
            }
            else if (ownerId < 0)
            {
                Group u = CacheManager.GetGroup(ownerId);
                from = u != null ? $"{Locale.Get("from")} \"{u.Name}\"" : "";
            }

            return from;
        }

        #endregion

        private void LoadColorResources()
        {
            string dict = Settings.ChatBackgroundType == 0 ? "No" : "On";
            string oldt = Settings.ChatBackgroundType == 0 ? "On" : "No";
            ThemeManager.LoadThemedResourceDictionary($"MessageBubble{dict}Wall", $"MessageBubble{oldt}Wall");
        }

        private void SetWidth(Grid bubblecontainer)
        {
            double margin = 38;
            if (hasAvatar) margin += 40;
            if (hasAttachments) bubbleWidth = isFixedWide ? fixedWideWidth - margin : fixedWidth - margin;
            if (hasGift) bubbleWidth = 184;
            if (hasSticker) bubbleWidth = 168;
            if (bubbleWidth > 0) bubblecontainer.Width = bubbleWidth;
        }

        private void FixElements(UIElementCollection elements, bool isInBubble)
        {
            int i = 0;
            foreach (UIElement el in elements)
            {
                bool isFirst = i == 0;
                bool isLast = i == elements.Count - 1;

                switch (el)
                {
                    case RichTextBlock tb when tb.Name == Constants.MsgTextBlockControlName:
                        tb.Margin = new Thickness(12, isFirst ? 7 : 5, 12, isLast ? 9 : 5); break;
                    case Grid pc when pc.Name == Constants.PhotosContainerControlName:
                    {
                        double mb = isInBubble ? 4 : 0;
                        pc.Margin = new Thickness(pc.Margin.Left, pc.Margin.Top, pc.Margin.Right, isLast ? mb : 0);
                        if (isInBubble)
                        {
                            CornerRadius cr = pc.CornerRadius;
                            pc.CornerRadius = new CornerRadius(!isFirst ? 4 : cr.TopLeft, !isFirst ? 4 : cr.TopRight, !isLast ? 4 : cr.BottomRight, !isLast ? 4 : cr.BottomLeft);
                        }
                        break;
                    }
                    case Border map when map.Name == Constants.LocationControlName:
                        map.CornerRadius = new CornerRadius(isFirst ? 14 : 0, isFirst ? 14 : 0, isLast ? 4 : 0, isLast ? 4 : 0); break;
                    case DefaultAttachmentControl uc:
                        uc.Margin = new Thickness(12, isFirst ? 12 : 6, 12, isLast ? 12 : 6); break;
                    case ExtendedAttachmentControl alc:
                        alc.Margin = new Thickness(12, isFirst ? 12 : 6, 12, isLast ? 12 : 6); break;
                    case AudioControl auc:
                        auc.Margin = new Thickness(12, isFirst ? 12 : 6, 12, isLast ? 12 : 6); break;
                    case VoiceMessageControl vmc:
                        vmc.Margin = new Thickness(12, isFirst ? 12 : 6, 12, isLast ? 12 : 6); break;
                    case Button rb when rb.Name == Constants.ReplyMessageButtonName:
                        rb.Margin = new Thickness(rb.Margin.Left, isFirst ? 12 : 6, rb.Margin.Right, rb.Margin.Bottom); break;
                    case HyperlinkButton hbtn:
                        hbtn.Margin = new Thickness(12, isFirst ? 7 : 0, 12, isLast ? 9 : 5); break;
                }

                i++;
            }
        }

        public void Dispose()
        {
            BuiltMessageUI = null;
            Parent = null;
        }
    }
}
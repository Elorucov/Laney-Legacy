using Elorucov.Laney.Controls;
using Elorucov.Laney.Controls.MessageAttachments;
using Elorucov.Laney.Models;
using Elorucov.Laney.Pages.Dialogs;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Services.UI {
    public class MessageUIHelper {
        const string MsgTextBlockControlName = "Text";
        const string StickerOrGraffitiControlName = "StickerOrGraffiti";
        const string GiftControlName = "Gift";
        const string PhotosContainerControlName = "Container";
        const string LocationControlName = "Location";
        const string WallPostControlName = "Wallpost";
        const string WallReplyControlName = "Wallreply";
        const string LinkControlName = "Link";
        const string PollControlName = "Poll";
        const string CallControlName = "Call";
        const string StoryControlName = "Story";
        const string AudioMsgControlName = "AudioMessage";
        const string VideoMsgControlName = "VideoMessage";
        const string DocumentAttachmentControlName = "Document";
        const string AudioAttachmentControlName = "Audio";
        const string StandartAttachmentControlName = "StandartAttachment";

        private static double BubbleFixedWidth { get { return 292; } }

        public static FrameworkElement Build(LMessage msg, LMessage prev, ScrollViewer parentScroll = null, bool nonBubbleView = false, bool showSendDay = false) {
            FrameworkElement g = null;
            if (msg.Action != null) {
                g = BuildActionMessage(msg, msg.SenderId);
            } else {
                if (!msg.IsExpired) {
                    if (nonBubbleView) {
                        g = new LegacyMessageUI(msg, showSendDay);
                        g.Margin = new Thickness(12, 6, 12, 6);
                    } else {
                        bool hideAvatar = prev != null && prev.SenderId == msg.SenderId && prev.Date.Date == msg.Date.Date && prev.Action == null;
                        g = new BubbleMessageUI(msg, parentScroll, hideAvatar);
                    }
                } else {
                    return BuildDisappearedMessageInfo();
                }
            }
            return g;
        }

        public static FrameworkElement BuildDisappearedMessageInfo() {
            HyperlinkButton b = (HyperlinkButton)(Application.Current.Resources["DisappearedMessageTemplate"] as DataTemplate).LoadContent();
            b.Click += async (a, c) => {
                await new ContentDialog {
                    Title = Locale.Get("msg_disappeared_title"),
                    Content = Locale.Get("msg_disappeared_desc"),
                    PrimaryButtonText = Locale.Get("close")
                }.ShowAsync();
            };
            return b;
        }

        private static FrameworkElement BuildActionMessage(LMessage msg, long fromId) {
            var action = msg.Action;

            Photo p = msg.Attachments.Count == 1 ? msg.Attachments[0].Photo : null;
            return APIHelper.GetActionMessageInfoUI(msg.Action, p);
        }

        #region Text and attachments controls

        public static RichTextBlock BuildTextBlock(TextParsingResult parsedTextInfo, bool isTimeInline = false, bool isEdit = false, bool isForOutgoingBubble = false) {
            RichTextBlock rtb = new RichTextBlock { Name = MsgTextBlockControlName };
            rtb.IsTextSelectionEnabled = AppParameters.IsTextSelectionEnabled;
            rtb.HorizontalAlignment = HorizontalAlignment.Left;
            rtb.FontSize = AppParameters.MessageFontSize;
            Theme.MessageBubbleFontSizeChanged += (a, b) => rtb.FontSize = b;
            Theme.IsTextSelectionEnabledChanged += (a, b) => rtb.IsTextSelectionEnabled = b;

            rtb.Blocks.Clear();
            Paragraph p = new Paragraph();
            foreach (var inline in VKTextParser.GetInlines(parsedTextInfo, OnLinkClicked)) {
                p.Inlines.Add(inline);
                if (inline is Hyperlink hl) ChatThemeService.RegisterLink(rtb, hl, isForOutgoingBubble);
            }
            rtb.Blocks.Add(p);

            if (isTimeInline) {
                double width = isEdit ? 80 : 48;
                if (isForOutgoingBubble) width += 16;
                InlineUIContainer iuc = new InlineUIContainer();
                Border b = new Border();
                b.Width = width;
                b.Height = 16;

                iuc.Child = b;
                p.Inlines.Add(iuc);
            }
            return rtb;
        }

        public static void OnLinkClicked(string link, bool isInline) {
            new System.Action(async () => {
                if (link == "ny") {
                    await TitleAndStatusBar.ShowGarland();
                } else {
                    if (!Uri.IsWellFormedUriString(link, UriKind.RelativeOrAbsolute)) return;
                    if (isInline) {
                        await VKLinks.LaunchLinkAsync(new Uri(link));
                    } else {
                        ContentDialog dlg = new ContentDialog {
                            Title = Locale.Get("external_link_confirm_title"),
                            Content = $"{Locale.Get("external_link_confirm")} {link}",
                            PrimaryButtonText = Locale.Get("yes"),
                            SecondaryButtonText = Locale.Get("no")
                        };

                        var result = await dlg.ShowAsync();
                        if (result == ContentDialogResult.Primary) {
                            await Launcher.LaunchUriAsync(new Uri(link));
                        }
                    }
                }
            })();
        }

        public static void OnLinkClicked(string link) {
            new System.Action(async () => {
                if (link == "ny") {
                    await TitleAndStatusBar.ShowGarland();
                } else {
                    if (Uri.IsWellFormedUriString(link, UriKind.RelativeOrAbsolute)) await VKLinks.LaunchLinkAsync(new Uri(link));
                }
            })();
        }

        // objectId — message or post id.
        // Проще прописывать размеры и отступы некоторых вложений,
        // чем дублировать полностью код добавления вложений...
        public static void AddAttachmentsToPanel(long objectId, StackPanel sp, List<Attachment> attachments,
            double photosContainerWidth, Thickness photosContainerMargin,
            double giftControlWidth = 0, Thickness giftControlMargin = new Thickness(),
            double stickerControlWidth = 0, Thickness stickerControlMargin = new Thickness(),
            double graffitiControlWidth = 0, Thickness graffitiControlMargin = new Thickness(),
            double geoControlWidth = 0, Thickness geoControlMargin = new Thickness(),
            Geo geo = null, bool isMsg = false, string atchsOwner = null, bool dontAddPreviews = false) {
            List<ISticker> stickers = new List<ISticker>();
            List<StickerPackPreview> stickersPreview = new List<StickerPackPreview>();
            Gift gift = null;
            List<Graffiti> graffities = new List<Graffiti>();
            List<IPreview> previews = new List<IPreview>();
            WallPost wp = null;
            WallReply wr = null;
            List<Link> links = new List<Link>();
            DonutLink donut = null;
            Market market = null;
            Poll poll = null;
            Call call = null;
            GroupCallInProgress gcall = null;
            Event evt = null;
            List<Story> stories = new List<Story>();
            Narrative nr = null;
            Curator cur = null;
            Artist artist = null;
            List<Document> docs = new List<Document>();
            List<Audio> audios = new List<Audio>();
            List<Podcast> podcasts = new List<Podcast>();
            List<AudioMessage> ams = new List<AudioMessage>();
            VideoMessage vms = null;
            WikiPage page = null;
            Note note = null;
            Album album = null;
            SituationalTheme sth = null;
            Textlive tl = null;
            TextpostPublish tpb = null;
            Article article = null;
            MoneyRequest mr = null;
            MoneyTransfer mt = null;
            List<AudioPlaylist> playlists = new List<AudioPlaylist>();
            List<MiniApp> apps = new List<MiniApp>();
            List<Attachment> unknown = new List<Attachment>();

            foreach (Attachment a in attachments) {
                switch (a.Type) {
                    case AttachmentType.Sticker: stickers.Add(a.Sticker); break;
                    case AttachmentType.StickerPackPreview: stickersPreview.Add(a.StickerPackPreview); break;
                    case AttachmentType.UGCSticker: stickers.Add(a.UGCSticker); break;
                    case AttachmentType.Graffiti: graffities.Add(a.Graffiti); break;
                    case AttachmentType.Gift: gift = a.Gift; break;
                    case AttachmentType.Photo: if (a.Photo.Sizes != null && a.Photo.Sizes.Count > 0) previews.Add(a.Photo); break;
                    case AttachmentType.Video: previews.Add(a.Video); break;
                    case AttachmentType.Audio: audios.Add(a.Audio); break;
                    case AttachmentType.Podcast: podcasts.Add(a.Podcast); break;
                    case AttachmentType.Curator: cur = a.Curator; break;
                    case AttachmentType.Artist: artist = a.Artist; break;
                    case AttachmentType.Wall: wp = a.Wall; break;
                    case AttachmentType.WallReply: wr = a.WallReply; break;
                    case AttachmentType.Link: links.Add(a.Link); break;
                    case AttachmentType.DonutLink: donut = a.DonutLink; break;
                    case AttachmentType.Market: market = a.Market; break;
                    case AttachmentType.Poll: poll = a.Poll; break;
                    case AttachmentType.VideoMessage: vms = a.VideoMessage; break;
                    case AttachmentType.AudioMessage: ams.Add(a.AudioMessage); break;
                    case AttachmentType.Call: call = a.Call; break;
                    case AttachmentType.GroupCallInProgress: gcall = a.GroupCallInProgress; break;
                    case AttachmentType.Event: evt = a.Event; break;
                    case AttachmentType.Story: stories.Add(a.Story); break;
                    case AttachmentType.Narrative: nr = a.Narrative; break;
                    case AttachmentType.Document: if (a.Document.Preview != null) { previews.Add(a.Document); } else { docs.Add(a.Document); }; break;
                    case AttachmentType.Page: page = a.Page; break;
                    case AttachmentType.Note: note = a.Note; break;
                    case AttachmentType.Album: album = a.Album; break;
                    case AttachmentType.PrettyCards: break; // чтобы сниппет "unknown attachment" не добавлялся
                    case AttachmentType.SituationalTheme: sth = a.SituationalTheme; break;
                    case AttachmentType.Textlive: tl = a.Textlive; break;
                    case AttachmentType.TextpostPublish: tpb = a.TextpostPublish; break;
                    case AttachmentType.AudioPlaylist: playlists.Add(a.AudioPlaylist); break;
                    case AttachmentType.MiniApp: apps.Add(a.MiniApp); break;
                    case AttachmentType.Article: article = a.Article; break;
                    case AttachmentType.MoneyRequest: mr = a.MoneyRequest; break;
                    case AttachmentType.MoneyTransfer: mt = a.MoneyTransfer; break;
                    default: unknown.Add(a); break;
                }
            }

            // Gift
            if (gift != null) {
                GiftPresenter gp = new GiftPresenter();
                gp.HorizontalAlignment = HorizontalAlignment.Left;
                gp.Name = GiftControlName;
                gp.Width = giftControlWidth;
                gp.Margin = giftControlMargin;
                gp.Gift = gift;
                sp.Children.Insert(0, gp);
            }

            // Sticker
            foreach (ISticker sticker in stickers) {
                StickerPresenter img = new StickerPresenter();
                img.Name = StickerOrGraffitiControlName;
                img.HorizontalAlignment = HorizontalAlignment.Left;
                img.Width = img.Height = stickerControlWidth;
                img.Margin = stickerControlMargin;
                img.Sticker = sticker;
                sp.Children.Add(img);
            }

            // Sticker pack preview
            foreach (var spp in stickersPreview) {
                var eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Name = LinkControlName,
                    Link = spp.Uri,
                    Title = spp.Title,
                    Caption = spp.Author,
                    Description = spp.Description,
                    ImageDirect = new Uri(spp.Icon.BaseUrl + "/square_2x.png")
                };
                sp.Children.Add(eac);
            }

            // Graffiti
            if (graffities.Count > 0) {
                foreach (Graffiti gr in graffities) {
                    Image img = new Image();
                    img.Name = StickerOrGraffitiControlName;
                    img.HorizontalAlignment = HorizontalAlignment.Left;
                    img.Width = graffitiControlWidth;
                    img.Height = gr.Width == 0 || gr.Height == 0 ? graffitiControlWidth / 2 : img.Width / gr.Width * gr.Height;
                    img.Margin = graffitiControlMargin;

                    sp.Children.Add(img);
                    BitmapImage bi = new BitmapImage();

                    new System.Action(async () => await bi.SetUriSourceAsync(gr.Uri, true))();
                    img.Source = bi;
                }
            }

            // Images
            if (!dontAddPreviews && previews.Count > 0) {
                Grid tg = AddThumbnailsGrid(previews, photosContainerWidth, objectId);
                tg.HorizontalAlignment = HorizontalAlignment.Center;
                tg.Margin = photosContainerMargin;
                sp.Children.Add(tg);
            }

            // Geo
            if (geo != null) {
                sp.Children.Add(new Border {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = geoControlWidth,
                    Margin = geoControlMargin,
                    CornerRadius = new CornerRadius(8),
                    Child = BuildGeoControl(geo),
                    Name = LocationControlName
                });
            }

            // Wall post
            if (wp != null) {
                string def = GetNameOrDefaultString(wp.OwnerOrToId, VKTextParser.GetOnlyParsedText(wp.Text));
                DefaultAttachmentControl dac = new DefaultAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Article"],
                    Title = Locale.Get("wallpost").Capitalize(),
                    Description = def,
                    Name = WallPostControlName,
                };
                dac.Click += async (a, b) => await VKLinks.ShowWallPostAsync(wp);
                sp.Children.Add(dac);
            }

            // Wall reply
            if (wr != null) {
                string def = GetNameOrDefaultString(wr.OwnerId, VKTextParser.GetOnlyParsedText(wr.Text));
                DefaultAttachmentControl dac = new DefaultAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Comment"],
                    Title = Locale.Get("wallpostcomment").Capitalize(),
                    Description = def,
                    Name = WallReplyControlName,
                };
                dac.Click += async (a, b) => await Launcher.LaunchUriAsync(new Uri($"https://vk.com/wall{wr.OwnerId}_{wr.PostId}?reply={wr.Id}"));
                sp.Children.Add(dac);
            }

            // Link
            foreach (Link lnk in links) {
                if (lnk.Photo != null && lnk.Photo.Id != 0) {
                    sp.Children.Add(new ExtendedAttachmentControl {
                        Margin = new Thickness(0, 6, 0, 6),
                        Name = LinkControlName,
                        Link = lnk.Uri,
                        Title = lnk.Title,
                        Caption = lnk.Caption,
                        Description = lnk.Description,
                        Image = lnk.Photo.Sizes.LastOrDefault(),
                        Button = lnk.Button
                    });
                } else {
                    DefaultAttachmentControl dac = new DefaultAttachmentControl {
                        Margin = new Thickness(0, 6, 0, 6),
                        IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Link"],
                        Title = !string.IsNullOrEmpty(lnk.Title) ? lnk.Title : Locale.Get("atch_link").Capitalize(),
                        Description = lnk.Caption ?? lnk.Uri.Host,
                        Name = LinkControlName
                    };
                    dac.Click += async (a, b) => await VKLinks.LaunchLinkAsync(lnk.Uri);
                    sp.Children.Add(dac);
                }
            }

            // Donut
            if (donut != null) {
                var info = AppSession.GetNameAndAvatar(donut.OwnerId);

                sp.Children.Add(new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Name = LinkControlName,
                    Link = donut.Action?.Uri,
                    Title = info.Item1,
                    Caption = donut.Text,
                    Image = new PhotoSizes { Width = 80, Height = 80, Url = info.Item3.AbsoluteUri },
                    Button = donut.Button
                });
            }

            // Market
            if (market != null) {
                Uri link = new Uri($"https://vk.com/product{market.OwnerId}_{market.Id}");
                sp.Children.Add(new ExtendedAttachmentControl {
                    Name = LinkControlName,
                    Link = link,
                    Title = market.Title,
                    Caption = $"{market.Price.Text} · {market.Category.Name}",
                    Description = market.Description,
                    Image = new PhotoSizes { Width = 80, Height = 80, Url = market.ThumbPhoto },
                    Button = new LinkButton { Title = Locale.Get("open"), Action = new LinkButtonAction { Url = link.AbsoluteUri } }
                });
            }

            // Poll
            if (poll != null) {
                string def = GetNameOrDefaultString(poll.AuthorId);
                DefaultAttachmentControl dac = new DefaultAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Poll"],
                    Title = poll.Question,
                    Description = $"{Locale.Get("poll").Capitalize()} {def}",
                    Name = PollControlName,
                };
                dac.Click += async (a, b) => await VKLinks.LaunchLinkAsync(new Uri($"https://vk.com/poll{poll.OwnerId}_{poll.Id}"));
                sp.Children.Add(dac);
            }

            // Call
            if (call != null) {
                AddCallInfoControl(sp, call);
            }

            // Group call in progress
            if (gcall != null) {
                AddCallInfoControl(sp, gcall);
            }

            // Event
            if (evt != null) {
                Group eg = Services.AppSession.GetCachedGroup(evt.Id);
                Uri link = new Uri($"https://vk.com/club{evt.Id}");

                sp.Children.Add(new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Name = LinkControlName,
                    Link = link,
                    Title = eg.Name,
                    Caption = !string.IsNullOrEmpty(evt.Address) ? evt.Address : evt.Text,
                    Description = evt.Text,
                    Image = new PhotoSizes { Width = 80, Height = 80, Url = eg.Photo.AbsoluteUri },
                    Button = new LinkButton { Title = evt.ButtonText, Action = new LinkButtonAction { Url = link.AbsoluteUri } }
                });
            }

            // Story
            foreach (Story st in stories) {
                string def = GetNameOrDefaultString(st.OwnerId);
                DefaultAttachmentControl dac = new DefaultAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Story"],
                    Title = Locale.Get("atch_story").Capitalize(),
                    Description = def,
                    Name = StoryControlName,
                    Tag = st
                };
                dac.Click += StoryClicked;
                sp.Children.Add(dac);
            }

            // Narrative
            if (nr != null) {
                string link = $"https://m.vk.com/narrative{nr.OwnerId}_{nr.Id}";
                sp.Children.Add(new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Name = LinkControlName,
                    Link = new Uri(link),
                    Title = nr.Title,
                    Caption = Locale.Get("atch_narrative").Capitalize() + " " + GetNameOrDefaultString(nr.OwnerId),
                    Image = nr.Cover?.CroppedSizes.LastOrDefault(),
                    Button = nr.CanSee && !nr.IsDeleted ? new LinkButton { Title = Locale.Get("open"), Action = new LinkButtonAction { Url = link } } : null
                });
            }

            // Curator
            if (cur != null) {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = cur.Name,
                    Caption = Locale.Get("atch_curator").Capitalize(),
                    Description = cur.Description,
                    ButtonText = Locale.Get("open"),
                    Image = cur.Photo[0],
                    Name = LinkControlName
                };
                eac.ButtonClick += async (a, b) => await Launcher.LaunchUriAsync(cur.Uri);
                sp.Children.Add(eac);
            }

            // Artist
            if (artist != null) {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = artist.Name,
                    Caption = Locale.Get("atch_artist").Capitalize(),
                    ButtonText = Locale.Get("open"),
                    Image = artist.Photo.LastOrDefault(),
                    Name = LinkControlName
                };
                eac.ButtonClick += async (a, b) => await Launcher.LaunchUriAsync(new Uri($"https://vk.com/artist/{artist.Id}"));
                sp.Children.Add(eac);
            }

            // Audios
            foreach (Audio a in audios) {
                AudioUC auc = new AudioUC {
                    Audio = a,
                    Margin = new Thickness(0, 6, 0, 6),
                    Name = AudioAttachmentControlName,
                };
                auc.IsPlayButtonClicked += async (b, c) => {
                    if (isMsg) {
                        await TryPlayAudio(objectId, audios, a, atchsOwner);
                    } else {
                        AudioPlayerViewModel.PlaySong(audios, a, atchsOwner);
                    }
                };
                sp.Children.Add(auc);
            }

            // Audio message
            foreach (AudioMessage am in ams) {
                VoiceMessageControl vmc = new VoiceMessageControl(am) {
                    Margin = new Thickness(0, 6, 0, 6),
                    Name = AudioMsgControlName,
                };
                vmc.IsPlayButtonClicked += (a, b) => AudioPlayerViewModel.PlayVoiceMessage(ams, am, atchsOwner);
                sp.Children.Add(vmc);
            }

            // Video message
            if (vms != null) {
                VideoMessageControl vmc = new VideoMessageControl(vms) {
                    Margin = new Thickness(0, 6, 0, 6),
                    Name = VideoMsgControlName,
                };
                sp.Children.Add(vmc);
            }

            // Podcasts
            foreach (Podcast p in podcasts) {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = p.Title,
                    Caption = Locale.Get("atch_podcast").Capitalize(),
                    ButtonText = Locale.Get("play"),
                    Image = p.Info.Cover.Sizes[0],
                    Name = LinkControlName
                };
                eac.ButtonClick += (a, b) => {
                    if (isMsg) {
                        TryPlayPodcast(objectId, podcasts, p, atchsOwner);
                    } else {
                        AudioPlayerViewModel.PlayPodcast(podcasts, p, atchsOwner);
                    }
                };
                sp.Children.Add(eac);
            }

            // Playlists
            foreach (AudioPlaylist p in playlists) {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = p.Title,
                    Caption = Locale.Get("atch_audio_playlist_nom").Capitalize(),
                    Description = p.Description,
                    ButtonText = Locale.Get("play"),
                    Name = LinkControlName
                };

                if (p.Thumbs != null && p.Thumbs.Count > 0) {
                    eac.ImageDirect = p.Thumbs[0].Photo300;
                } else if (p.Photo != null) {
                    eac.ImageDirect = p.Photo.Photo300;
                }

                string link = $"https://vk.com/music/album/{p.OwnerId}_{p.Id}";
                if (!string.IsNullOrEmpty(p.AccessKey)) link += $"_{p.AccessKey}";
                eac.Link = new Uri(link);
                eac.ButtonClick += async (a, b) => {
                    if (p.Audios.Count > 0) await TryPlayAudio(objectId, p.Audios, p.Audios.FirstOrDefault(), p.Title);
                };
                sp.Children.Add(eac);
            }

            // Documents
            foreach (Document d in docs) {
                DefaultAttachmentControl dac = new DefaultAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Document"],
                    Title = d.Title,
                    Description = $"{d.Extension} · {Functions.GetFileSize(d.Size)}",
                    Name = DocumentAttachmentControlName,
                };
                dac.Click += async (a, b) => await Launcher.LaunchUriAsync(d.Uri);
                sp.Children.Add(dac);
            }

            // Page
            if (page != null) {
                DefaultAttachmentControl dac = new DefaultAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Link"],
                    Title = page.Title,
                    Description = Locale.Get("atch_wikipage").Capitalize(),
                    Name = LinkControlName,
                    Tag = page
                };
                dac.Click += async (a, b) => await Launcher.LaunchUriAsync(new Uri(page.ViewUrl));
                sp.Children.Add(dac);
            }

            // Note
            if (note != null) {
                DefaultAttachmentControl dac = new DefaultAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Note"],
                    Title = note.Title,
                    Description = Locale.Get("atch_note").Capitalize(),
                    Name = LinkControlName,
                    Tag = note
                };
                dac.Click += async (a, b) => await Launcher.LaunchUriAsync(new Uri(note.ViewUrl));
                sp.Children.Add(dac);
            }

            // Album
            if (album != null) {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = album.Title,
                    Caption = Locale.Get("atch_album").Capitalize(),
                    Description = album.Description,
                    ButtonText = Locale.Get("open"),
                    Image = album.Thumb?.Sizes[1],
                    Name = LinkControlName
                };
                eac.ButtonClick += async (a, b) => await Launcher.LaunchUriAsync(new Uri($"https://vk.com/album{album.OwnerId}_{album.Id}"));
                sp.Children.Add(eac);
            }

            // Mini apps
            foreach (MiniApp app in apps) {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = app.App.Title,
                    Caption = app.Description,
                    ButtonText = Locale.Get("open"),
                    ImageDirect = app.App.Icon,
                    Name = LinkControlName
                };
                eac.ButtonClick += async (a, b) => await VKLinks.LaunchLinkAsync(new Uri($"https://vk.com/app{app.App.Id}?ref=laney"));
                sp.Children.Add(eac);
            }

            // Situational theme
            if (sth != null) {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = sth.Title,
                    Caption = sth.Description,
                    Description = sth.Description,
                    ButtonText = Locale.Get("open"),
                    Image = sth.SquaredCoverPhoto.Sizes[1],
                    Name = LinkControlName
                };
                eac.ButtonClick += async (a, b) => await Launcher.LaunchUriAsync(sth.Uri);
                sp.Children.Add(eac);
            }

            // Textlive
            if (tl != null) {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = tl.Title,
                    Description = tl.Url,
                    ButtonText = Locale.Get("open"),
                    Image = tl.CoverPhoto.Sizes[1],
                    Name = LinkControlName
                };
                eac.ButtonClick += async (a, b) => await Launcher.LaunchUriAsync(tl.Uri);
                sp.Children.Add(eac);
            }

            // Textpost publish
            if (tpb != null) {
                DefaultAttachmentControl dac = new DefaultAttachmentControl {
                    Title = tpb.Title,
                    Name = LinkControlName,
                    IconTemplate = (DataTemplate)Application.Current.Resources["Icon24TextLiveOutline"]
                };
                dac.Click += async (a, b) => await Launcher.LaunchUriAsync(tpb.Uri);
                sp.Children.Add(dac);
            }

            // Money request
            if (mr != null) {
                string title = string.Empty;
                if (mr.TotalAmount?.Number > 0 && mr.HeldAmount?.Number > 0) {
                    title = String.Format(Locale.GetForFormat("money_request_limit_held"), mr.TransferredAmount.Text, mr.TotalAmount.Text, mr.HeldAmount.Text);
                } else if (mr.TotalAmount?.Number > 0 && mr.HeldAmount?.Number == 0) {
                    title = String.Format(Locale.GetForFormat("money_request_limit"), mr.TransferredAmount.Text, mr.TotalAmount.Text);
                } else if (mr.TotalAmount?.Number == 0 && mr.HeldAmount?.Number > 0) {
                    title = String.Format(Locale.GetForFormat("money_request_held"), mr.TransferredAmount.Text, mr.HeldAmount.Text);
                } else if (mr.TransferredAmount != null) {
                    title = String.Format(Locale.GetForFormat("money_request"), mr.TransferredAmount.Text);
                } else {
                    title = mr.Amount.Text;
                }

                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = title,
                    Caption = Locale.Get("atch_money_request").Capitalize(),
                    ButtonText = Locale.Get("send_money"),
                    Name = LinkControlName
                };
                eac.ButtonClick += async (a, b) => await Launcher.LaunchUriAsync(new Uri(mr.InitUrl));
                sp.Children.Add(eac);
            }

            // Money transfer
            if (mt != null) {
                ExtendedAttachmentControl eac = new ExtendedAttachmentControl {
                    Margin = new Thickness(0, 6, 0, 6),
                    Title = mt.Amount.Text,
                    Caption = Locale.Get("atch_money_transfer").Capitalize(),
                    ImageDirect = new Uri("https://sun2-11.userapi.com/impf/c636320/v636320075/20c8e/NQ9_h0G-ePQ.jpg?size=180x180&quality=96&sign=0553d35401a524d0da18d52d294edf88&c_uniq_tag=vuDHo6enSfAmLh0XFddilP9s4vcFbBdwagnWbqjHeWM&type=album"),
                    Name = LinkControlName
                };
                sp.Children.Add(eac);
            }

            // Article
            if (article != null) {
                sp.Children.Add(new ArticlePreview {
                    Article = article,
                    Margin = new Thickness(0, 6, 0, 6)
                });
            }

            // Unknown attachments
            foreach (var a in unknown) {
                AddUnknownAttachment(sp, a, new Thickness(0, 6, 0, 6));
            }
        }

        private static async Task TryPlayAudio(long objectId, List<Audio> audios, Audio a, string atchsOwner) {
            if (a.ContentRestricted > 0 || string.IsNullOrEmpty(a.Url)) {
                object r = await Audios.GetRestrictionPopup(a.Id, API.WebToken);
                if (r != null && r is AudioRestrictionInfo info) {
                    await new ContentDialog {
                        Title = info.Title,
                        Content = info.Text,
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                } else {
                    Functions.ShowHandledErrorDialog(r);
                }
                return;
            }
            AudioPlayerViewModel.PlaySong(audios, a, atchsOwner);
        }

        private static void TryPlayPodcast(long objectId, List<Podcast> podcasts, Podcast p, string atchsOwner) {
            AudioPlayerViewModel.PlayPodcast(podcasts, p, atchsOwner);
        }

        // TODO: return StackPanel itself after getting rid of old photo layout.
        public static Grid AddThumbnailsGrid(List<IPreview> previews, double width, long messageId) {
            Grid g = new Grid();
            if (previews.Count > 1) {
                StackPanel photoLayoutRoot = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                double actualWidth = AddThumbnailsToStackPanel(photoLayoutRoot, previews, width, messageId);
                g.Width = actualWidth;
                g.Children.Add(photoLayoutRoot);
            } else {
                var preview = previews[0];
                bool oldFormat = preview.PreviewImageSize.Width == 0 && preview.PreviewImageSize.Height == 0;
                double height = oldFormat ? width : width / preview.PreviewImageSize.Width * preview.PreviewImageSize.Height;
                double maxheight = width / 9 * 16;
                PhotoVideoThumbnail thmb = new PhotoVideoThumbnail {
                    Width = width,
                    MinHeight = 32,
                    Height = height > maxheight ? maxheight : height,
                    Preview = preview
                };
                if (preview is Photo p) {
                    GalleryItem gp = new GalleryItem(p);
                    Tuple<List<GalleryItem>, GalleryItem> t =
                        new Tuple<List<GalleryItem>, GalleryItem>(new List<GalleryItem> { gp }, gp);
                    thmb.Tag = t;
                    thmb.Click += (a, b) => ShowPhotoViewer(t);
                } else if (preview is Video v) {
                    thmb.Click += async (a, b) => await Pages.VideoPlayerView.Show(messageId, v);
                } else if (preview is Document d) {
                    GalleryItem gd = new GalleryItem(d);
                    Tuple<List<GalleryItem>, GalleryItem> t =
                        new Tuple<List<GalleryItem>, GalleryItem>(new List<GalleryItem> { gd }, gd);
                    thmb.Tag = t;
                    thmb.Click += (a, b) => ShowPhotoViewer(t);
                }
                g.Children.Add(thmb);
            }
            g.Name = PhotosContainerControlName;
            return g;
        }

        private static double AddThumbnailsToStackPanel(StackPanel photoLayoutRoot, List<IPreview> previews, double width, long objectId) {
            var layout = ModernPhotoLayout.GeneratePhotoLayout(previews, width, width / 2 * 3, out double actualWidth);

            int i = 0;
            bool isFirstRow = true;
            foreach (var row in layout) {
                StackPanel rowSP = new StackPanel {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, isFirstRow ? 0 : ModernPhotoLayout.GAP, 0, 0)
                };
                bool firstCol = true;
                foreach (var photo in row) {
                    PhotoVideoThumbnail thmb = GenerateThumbnailControl(previews, previews[i], objectId, photo.Width, photo.Height);
                    if (!firstCol) thmb.Margin = new Thickness(ModernPhotoLayout.GAP, 0, 0, 0);
                    firstCol = false;
                    rowSP.Children.Add(thmb);
                    i++;
                }
                photoLayoutRoot.Children.Add(rowSP);
                isFirstRow = false;
            }
            return actualWidth;
        }

        private static PhotoVideoThumbnail GenerateThumbnailControl(List<IPreview> previews, IPreview preview, long objectId, double width, double height) {
            PhotoVideoThumbnail thmb = new PhotoVideoThumbnail();
            thmb.Width = width;
            thmb.Height = height;
            thmb.Preview = preview;
            if (preview is Photo p) {
                List<GalleryItem> photos = (from ph in previews
                                            where ph is Photo
                                            select new GalleryItem((Photo)ph)).ToList();
                GalleryItem photo = photos.Where(ph => ph.OriginalObject == p).FirstOrDefault();
                Tuple<List<GalleryItem>, GalleryItem> t = new Tuple<List<GalleryItem>, GalleryItem>(photos, photo);
                thmb.Tag = t;
                thmb.Click += (a, b) => ShowPhotoViewer(t);
            } else if (preview is Video v) {
                thmb.Click += async (a, b) => await Pages.VideoPlayerView.Show(objectId, v);
            } else if (preview is Document d) {
                List<GalleryItem> photodocs = (from pd in previews
                                               where pd is Document && ((Document)pd).Preview != null
                                               select new GalleryItem((Document)pd)).ToList();
                GalleryItem photodoc = photodocs.Where(ph => ph.OriginalObject == d).FirstOrDefault();
                Tuple<List<GalleryItem>, GalleryItem> t = new Tuple<List<GalleryItem>, GalleryItem>(photodocs, photodoc);
                thmb.Tag = t;
                thmb.Click += (a, b) => ShowPhotoViewer(t);
            }
            return thmb;
        }

        private static FrameworkElement BuildGeoControl(Geo geo) {
            try {
                MapControl m = new MapControl();
                Geopoint g = new Geopoint(new BasicGeoposition { Latitude = geo.Coordinates.Latitude, Longitude = geo.Coordinates.Longitude });
                m.ActualCamera.Location = g;
                m.ActualCamera.Pitch = 0;
                m.ActualCamera.Heading = 0;
                m.Center = g;
                m.MapServiceToken = "UiW7WOb2oHEe9yQSEVpg~53Z_RFeFeK1NCE1sIE_-Kw~Apg8QKsKCmrLWTa6BttNQVkUvaDQJjwCmgv8814SFKD2SmHWNIOJEUKpp8MDO-GP";
                m.PanInteractionMode = MapPanInteractionMode.Disabled;
                m.RotateInteractionMode = MapInteractionMode.Disabled;
                m.TiltInteractionMode = MapInteractionMode.Disabled;
                m.ZoomInteractionMode = MapInteractionMode.Disabled;
                m.Height = 200;
                m.ZoomLevel = 15;

                return m;
            } catch (Exception) { // В урезанных и г-сборках винды из-за отсутсвия служб, связанных с location, MapControl крашит приложение.
                return new Border() {
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(64, 128, 128, 128)),
                    Child = new TextBlock {
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Text = "Failed to initialize MapControl!"
                    }
                };
            }

            //string lati = geo.Coordinates.Latitude.ToString().Replace(',', '.');
            //string lon = geo.Coordinates.Longitude.ToString().Replace(',', '.');

            //BitmapImage bimg = new BitmapImage();
            //bimg.UriSource = new Uri($"https://static-maps.yandex.ru/1.x/?ll={lon},{lati}&size=360,180&z=16&lang=ru_RU&l=pmap");

            //Border m = new Border();
            //m.MaxWidth = 480;
            //m.Height = 240;
            //m.Background = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128));
            //m.Child = new Image() { Source = bimg, Stretch = Stretch.UniformToFill, HorizontalAlignment = HorizontalAlignment.Center };

            //gr.Children.Add(m);
            //Grid.SetColumn(m, 0);
            //return gr;
        }

        private static void AddCallInfoControl(StackPanel sp, Call call) {
            string title = Locale.Get(call.InitiatorId == AppParameters.UserID ? "outgoing_call" : "incoming_call");
            string subtitle = string.Empty;

            if (call.Participants != null) {
                int c = call.Participants.Count;
                subtitle = $"{c} {Locale.GetDeclension(c, "members")}. ";
            }

            switch (call.State) {
                case "reached": subtitle += call.Duration.ToString(call.Duration.Hours > 0 ? @"h\:mm\:ss" : @"m\:ss"); break;
                case "canceled_by_receiver": subtitle += Locale.Get(call.InitiatorId == AppParameters.UserID ? "call_declined" : "call_canceled"); break;
                case "canceled_by_initiator": subtitle += Locale.Get(call.InitiatorId == AppParameters.UserID ? "call_canceled" : "call_missed"); break;
                default: subtitle += call.State; break;
            }

            sp.Children.Add(new DefaultAttachmentControl {
                IconTemplate = (DataTemplate)Application.Current.Resources[call.Video ? "Icon24Videocam" : "Icon24Phone"],
                Title = call.ReceiverId.IsChat() ? Locale.Get("group_call") : title,
                Description = subtitle,
                Name = CallControlName,
            });
        }

        private static void AddCallInfoControl(StackPanel sp, GroupCallInProgress call) {
            string subtitle = string.Empty;

            if (call.Participants != null) {
                int c = call.Participants.Count;
                subtitle = $"{c} {Locale.GetDeclension(c, "members")}";
            }

            sp.Children.Add(new ExtendedAttachmentControl {
                Title = Locale.Get("group_call"),
                Caption = subtitle,
                Name = CallControlName
            });
        }

        private static void AddUnknownAttachment(StackPanel sp, Attachment attachment, Thickness margin) {
            sp.Children.Add(new DefaultAttachmentControl {
                IconTemplate = (DataTemplate)Application.Current.Resources["Icon24Error"],
                Title = Locale.Get("unknown_attachment"),
                Description = $"Type: {attachment.TypeString}",
                Name = StandartAttachmentControlName,
                Margin = margin
            });
        }

        public static BotKeyboardControl BuildInlineKeyboard(int messageId, BotKeyboard bk) {
            BotKeyboardControl bkc = new BotKeyboardControl();
            bkc.Margin = new Thickness(6, 0, 6, 6);
            bkc.Keyboard = bk;
            bkc.ButtonClicked += async (a, b) => {
                Group g = AppSession.GetCachedGroup(bk.AuthorId);
                string n = g != null ? $"[club{bk.AuthorId * -1}|{g.ScreenName}] {b.Label}" : $"[club{bk.AuthorId * -1}|{b.Label}]";

                string text = AppSession.CurrentConversationVM.ConversationId.IsChat() ? n : b.Label;
                await AppSession.CurrentConversationVM.MessageFormViewModel.SendMessageToBot(b, text, messageId, uiButton: (Button)a);
            };
            return bkc;
        }

        public static ScrollViewer BuildCarousel(List<CarouselElement> elements, int messageId) {
            ScrollViewer sv = new ScrollViewer();
            sv.HorizontalAlignment = HorizontalAlignment.Stretch;
            sv.VerticalAlignment = VerticalAlignment.Top;
            sv.VerticalScrollMode = ScrollMode.Disabled;
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.HorizontalScrollMode = ScrollMode.Enabled;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Margin = new Thickness(12, 0, 0, 0);

            foreach (CarouselElement el in elements) {
                GalleryItem gItem = null;
                if (el.Photo != null) gItem = new GalleryItem(el.Photo);
                BotCarouselElement bce = new BotCarouselElement();
                bce.OwnerMessageId = messageId;
                bce.Margin = new Thickness(0, 4, 4, 0);
                bce.Click += async (a, b) => {
                    switch (b.Type) {
                        case BotButtonType.OpenLink: await Launcher.LaunchUriAsync(b.LinkUri); break;
                        case BotButtonType.OpenPhoto:
                            if (gItem != null) ShowPhotoViewer(new Tuple<List<GalleryItem>, GalleryItem>(new List<GalleryItem> { gItem }, gItem));
                            break;
                    }
                };
                bce.ElementButtonClick += async (a, b) => {
                    await AppSession.CurrentConversationVM.MessageFormViewModel.SendMessageToBot(b, null, messageId);
                };
                bce.Element = el;
                sp.Children.Add(bce);
            }

            sv.Content = sp;
            return sv;
        }

        public static string GetNameOrDefaultString(long ownerId, string defaultStr = null) {
            if (!string.IsNullOrEmpty(defaultStr)) return defaultStr;
            string from = "";
            if (ownerId.IsUser()) {
                VkAPI.Objects.User u = AppSession.GetCachedUser(ownerId);
                from = u != null ? $"{Locale.Get("from")} {u.FirstNameGen} {u.LastNameGen}" : "";
            } else if (ownerId.IsGroup()) {
                Group u = AppSession.GetCachedGroup(ownerId);
                from = u != null ? $"{Locale.Get("from")} \"{u.Name}\"" : "";
            }
            return from;
        }

        #endregion

        #region Attachment controls events

        public static void ShowForwardedMessages(List<LMessage> forwardedMessages) {
            VKMessageDialog md = new VKMessageDialog(forwardedMessages);
            md.Title = forwardedMessages.Count > 1 ? Locale.Get("msgmodaltitle_fwd_messages") : Locale.Get("msgmodaltitle_fwd_message");
            md.Show();
        }

        private static void ShowPhotoViewer(Tuple<List<GalleryItem>, GalleryItem> items) {
            if (ViewManagement.GetWindowType() == WindowType.Main) Pages.PhotoViewer.Show(items);
        }

        private static void StoryClicked(object sender, RoutedEventArgs e) {
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

        #endregion
    }
}
using Elorucov.Laney.Controls;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class PollViewer : Modal {
        Poll poll;
        List<ulong> CheckedAnswerIds = new List<ulong>();

        public PollViewer(Poll poll) {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            CornersRadius = 8;
            Loaded += (a, b) => {
                this.poll = poll;
                SetUp(poll);

                if (ViewManagement.GetWindowType() != WindowType.Main) mfShare.Visibility = Visibility.Collapsed;
            };
        }

        private void SetUp(Poll poll, bool dontUpdateOptions = false) {
            if (poll.Background != null) {
                RequestedTheme = ElementTheme.Dark;
                PollBackground b = poll.Background;

                Background = new SolidColorBrush(b.Color);
                MultiplePollVoteButton.Foreground = new SolidColorBrush(b.Color);
                if (b.Type == PollBackgroundType.Gradient) {
                    GradientStopCollection gsc = new GradientStopCollection();
                    foreach (var p in b.Points) {
                        gsc.Add(new GradientStop { Offset = p.Position, Color = p.Color });
                    }
                    LinearGradientBrush lgb = new LinearGradientBrush(gsc, b.Angle);
                    lgb.EndPoint = new Point(-lgb.EndPoint.X, -lgb.EndPoint.Y);
                    GradientBackground.Background = lgb;
                }
            } else if (poll.Photo != null) {
                RequestedTheme = ElementTheme.Dark;
                PollPhoto pp = poll.Photo;

                ViewRoot.Background = new SolidColorBrush(Color.FromArgb(102, 0, 0, 0));
                Background = new SolidColorBrush(pp.Color);
                GradientStopCollection gsc = new GradientStopCollection();
                gsc.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0, pp.Color.R, pp.Color.G, pp.Color.B) });
                gsc.Add(new GradientStop { Offset = 1, Color = pp.Color });
                LinearGradientBrush lgb = new LinearGradientBrush(gsc, 0) {
                    StartPoint = new Point(0, 0), EndPoint = new Point(0, 1)
                };
                PhotoGradient.Background = lgb;

                BitmapImage bi = new BitmapImage();
                new System.Action(async () => await bi.SetUriSourceAsync(pp.Images.Last().Uri))();
                PhotoBackground.Background = new ImageBrush() { ImageSource = bi, AlignmentY = AlignmentY.Top, Stretch = Stretch.UniformToFill };
            }

            bool finished = poll.Closed || !poll.CanVote || (poll.EndDateUnix > 0 && DateTime.Now >= poll.EndDate);

            Title.Text = poll.Question;

            string author = poll.AuthorId.IsUser() ?
                Services.AppSession.GetCachedUser(poll.OwnerId)?.FullName :
                Services.AppSession.GetCachedGroup(poll.OwnerId)?.Name;
            Owner.Visibility = string.IsNullOrEmpty(author) ? Visibility.Collapsed : Visibility.Visible;
            Owner.Text = string.IsNullOrEmpty(author) ? poll.OwnerId.ToString() : author;

            string type = poll.Anonymous ? Locale.Get("poll_anonymous") : Locale.Get("poll_public");
            if (poll.DisableUnvote && poll.AnswerIds.Count >= 0) {
                type += $" · {Locale.Get("poll_disable_unvote")}";
            } else if (finished) {
                type += $" · {Locale.Get("poll_finished")}";
            } else if (poll.EndDateUnix > 0) {
                type += $" · {Locale.Get("until")} {poll.EndDate.ToTimeOrDate()}";
            }
            Info.Text = type;

            bool canVote = !finished && poll.AnswerIds.Count == 0;

            if (!dontUpdateOptions) {
                foreach (PollAnswer a in poll.Answers) {
                    PollOptionControl poc = new PollOptionControl() {
                        Id = a.Id,
                        Text = a.Text,
                        Rate = a.Rate,
                        Votes = a.Votes,
                        CanVote = canVote,
                        IsMultivariantCheckBoxVisible = poll.Multiple,
                        IsChecked = poll.AnswerIds.Contains(a.Id),
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    poc.Click += OptionClicked;
                    Options.Children.Add(poc);
                }
                (Options.Children.Last() as PollOptionControl).Margin = new Thickness(0);
            }

            bool friendsVoted = poll.Friends != null && poll.Friends.Count > 0;
            FriendsAvatars.Visibility = friendsVoted ? Visibility.Visible : Visibility.Collapsed;

            if (poll.Votes > 0) {
                if (friendsVoted) {
                    Services.AppSession.AddUsersToCache(poll.Profiles);
                    ObservableCollection<UserAvatarItem> avatars = new ObservableCollection<UserAvatarItem>();
                    foreach (User u in poll.Friends) {
                        User f = Services.AppSession.GetCachedUser(u.Id);
                        BitmapImage ava = new BitmapImage();
                        new System.Action(async () => await ava.SetUriSourceAsync(f.Photo))();
                        avatars.Add(new UserAvatarItem { Name = f.FullName, Image = ava });
                    }
                    FriendsAvatars.Avatars = avatars;
                }

                Run voted = new Run() { Text = Locale.GetDeclension(poll.Votes, "poll_result_voted") };
                Run vc = new Run() { Text = $" {poll.Votes} ", FontWeight = new Windows.UI.Text.FontWeight() { Weight = 600 } };
                Run people = new Run() { Text = Locale.GetDeclension(poll.Votes, "poll_result_people") };
                Voters.Inlines.Clear();
                Voters.Inlines.Add(voted);
                Voters.Inlines.Add(vc);
                Voters.Inlines.Add(people);
            } else {
                Voters.Text = finished ? Locale.Get("poll_no_votes") : Locale.Get("poll_vote_first");
            }

            mfShare.Visibility = poll.CanShare ? Visibility.Visible : Visibility.Collapsed;
        }

        #region Methods

        private async Task Vote() {
            Options.IsHitTestVisible = false;
            VotersContainer.Visibility = Visibility.Collapsed;
            MultiplePollVoteButton.Visibility = Visibility.Collapsed;
            VotingSpinner.Visibility = Visibility.Visible;

            object resp = await Execute.AddVoteAndGetResult(poll.OwnerId, poll.Id, CheckedAnswerIds);
            if (resp is AddVoteResponse avr) {
                if (avr.Success) {
                    SetUp(avr.Poll, true);
                    foreach (UIElement el in Options.Children) {
                        if (el is PollOptionControl poc) {
                            var q = from a in avr.Poll.Answers where a.Id == poc.Id select a;
                            if (q.Count() == 1) {
                                bool contains = avr.Poll.AnswerIds.Contains(q.First().Id);
                                poc.CanVote = false;
                                poc.Votes = q.First().Votes;
                                poc.Rate = q.First().Rate;
                                poc.Text = q.First().Text;
                                poc.IsChecked = contains;
                                Debug.WriteLine($"PollOptionControl id: {poc.Id}, contains in answer_ids: {contains}");
                            }
                        }
                    }
                } else {
                    Tips.Show("Failed.");
                }
            } else {
                Functions.ShowHandledErrorTip(resp);
            }

            VotersContainer.Visibility = Visibility.Visible;
            VotingSpinner.Visibility = Visibility.Collapsed;
            Options.IsHitTestVisible = true;
            CheckedAnswerIds.Clear();
        }

        private void UpdateCheckedAnswerIds() {
            CheckedAnswerIds.Clear();
            foreach (var e in Options.Children) {
                if (e is PollOptionControl poc && poc.IsChecked) CheckedAnswerIds.Add(poc.Id);
            }
        }

        #endregion

        #region Events

        private void OptionClicked(object sender, RoutedEventArgs e) {
            PollOptionControl poc = sender as PollOptionControl;
            if (poll.Multiple) {
                UpdateCheckedAnswerIds();
                Voters.Visibility = CheckedAnswerIds.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                MultiplePollVoteButton.Visibility = CheckedAnswerIds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            } else {
                CheckedAnswerIds.Add(poc.Id);
                new System.Action(async () => { await Vote(); })();
            }
        }

        private void Vote(object sender, RoutedEventArgs e) {
            new System.Action(async () => { await Vote(); })();
        }

        private void SharePoll(object sender, RoutedEventArgs e) {
            Hide();
            Main.GetCurrent().StartForwardingAttachments(new List<AttachmentBase> { poll });
        }

        private void CloseModal(object sender, RoutedEventArgs e) {
            Hide();
        }

        #endregion

        private void ShowContextMenu(object sender, RoutedEventArgs e) {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }
    }
}
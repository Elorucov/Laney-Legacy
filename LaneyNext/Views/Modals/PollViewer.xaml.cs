using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Controls;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.VKAPIExecute.Objects;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    public sealed partial class PollViewer : Modal
    {
        Poll poll;
        List<int> CheckedAnswerIds = new List<int>();

        public PollViewer(Poll poll)
        {
            this.InitializeComponent();
            CornersRadius = 18;
            this.poll = poll;
            Loaded += (a, b) => SetUp(poll);
        }

        private void SetUp(Poll poll, bool dontUpdateOptions = false)
        {
            if (poll.Background != null)
            {
                RequestedTheme = ElementTheme.Dark;
                PollBackground b = poll.Background;

                SolidColorBrush pollBrush = new SolidColorBrush(b.ColorHEX.ParseFromHex());
                Background = pollBrush;
                MultiplePollVoteButton.Foreground = pollBrush;
                if (b.Type == PollBackgroundType.Gradient)
                {
                    GradientStopCollection gsc = new GradientStopCollection();
                    foreach (var p in b.Points)
                    {
                        gsc.Add(new GradientStop { Offset = p.Position, Color = p.ColorHEX.ParseFromHex() });
                    }
                    LinearGradientBrush lgb = new LinearGradientBrush(gsc, b.Angle);
                    lgb.EndPoint = new Point(-lgb.EndPoint.X, -lgb.EndPoint.Y);
                    GradientBackground.Background = lgb;
                }
            }
            else if (poll.Photo != null)
            {
                RequestedTheme = ElementTheme.Dark;
                PollPhoto pp = poll.Photo;
                Color ppcolor = pp.ColorHEX.ParseFromHex();

                ViewRoot.Background = new SolidColorBrush(Color.FromArgb(102, 0, 0, 0));
                Background = new SolidColorBrush(ppcolor);
                GradientStopCollection gsc = new GradientStopCollection();
                gsc.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0, ppcolor.R, ppcolor.G, ppcolor.B) });
                gsc.Add(new GradientStop { Offset = 1, Color = ppcolor });
                LinearGradientBrush lgb = new LinearGradientBrush(gsc, 0)
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };
                PhotoGradient.Background = lgb;

                BitmapImage bi = new BitmapImage(pp.Images.Last().Uri);
                PhotoBackground.Background = new ImageBrush() { ImageSource = bi, AlignmentY = AlignmentY.Top, Stretch = Stretch.UniformToFill };
            }

            bool finished = poll.Closed || !poll.CanVote || (poll.EndDateUnix > 0 && DateTime.Now >= poll.EndDate);

            Title.Text = poll.Question;

            CacheManager.Add(poll.Profiles);
            CacheManager.Add(poll.Groups);
            if (poll.OwnerId != 0)
            {
                string author = CacheManager.GetNameOnly(poll.OwnerId);
                Owner.Visibility = Visibility.Visible;
                Owner.Text = author;
            }

            string type = poll.Anonymous ? Locale.Get("poll_anonymous") : Locale.Get("poll_public");
            if (poll.DisableUnvote && poll.AnswerIds.Count >= 0)
            {
                type += $" · {Locale.Get("poll_disable_unvote")}";
            }
            else if (finished)
            {
                type += $" · {Locale.Get("poll_finished")}";
            }
            else if (poll.EndDateUnix > 0)
            {
                type += $" · {Locale.Get("until")} {poll.EndDate.ToTimeOrDate()}";
            }
            Info.Text = type;

            bool canVote = !finished && poll.AnswerIds.Count == 0;

            if (!dontUpdateOptions)
            {
                foreach (PollAnswer a in poll.Answers)
                {
                    PollOptionControl poc = new PollOptionControl()
                    {
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

            if (poll.Votes > 0)
            {
                if (friendsVoted)
                {
                    ObservableCollection<UserAvatarItem> avatars = new ObservableCollection<UserAvatarItem>();
                    foreach (User u in poll.Friends)
                    {
                        User f = CacheManager.GetUser(u.Id);
                        avatars.Add(new UserAvatarItem { Name = f.FullName, Image = new BitmapImage(f.Photo) });
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
            }
            else
            {
                Voters.Text = finished ? Locale.Get("poll_no_votes") : Locale.Get("poll_vote_first");
            }

            mfShare.Visibility = poll.CanShare ? Visibility.Visible : Visibility.Collapsed;
            MoreButton.Visibility = poll.CanShare ? Visibility.Visible : Visibility.Collapsed; // TODO: редактирование опроса
        }

        #region Methods

        private async void Vote()
        {
            Options.IsHitTestVisible = false;
            VotersContainer.Visibility = Visibility.Collapsed;
            MultiplePollVoteButton.Visibility = Visibility.Collapsed;
            VotingSpinner.Visibility = Visibility.Visible;

            try
            {
                AddVoteResponse response = await VKSession.Current.Execute.AddVoteAndGetResultAsync(poll.OwnerId, poll.Id, CheckedAnswerIds);
                SetUp(response.Poll, true);
                foreach (UIElement el in Options.Children)
                {
                    if (el is PollOptionControl poc)
                    {
                        var q = from a in response.Poll.Answers where a.Id == poc.Id select a;
                        if (q.Count() == 1)
                        {
                            bool contains = response.Poll.AnswerIds.Contains(q.First().Id);
                            poc.CanVote = false;
                            poc.Votes = q.First().Votes;
                            poc.Rate = q.First().Rate;
                            poc.Text = q.First().Text;
                            poc.IsChecked = contains;
                            Debug.WriteLine($"PollOptionControl id: {poc.Id}, contains in answer_ids: {contains}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) Vote();
            }

            VotersContainer.Visibility = Visibility.Visible;
            VotingSpinner.Visibility = Visibility.Collapsed;
            Options.IsHitTestVisible = true;
            CheckedAnswerIds.Clear();
        }

        private void UpdateCheckedAnswerIds()
        {
            CheckedAnswerIds.Clear();
            foreach (var e in Options.Children)
            {
                if (e is PollOptionControl poc && poc.IsChecked) CheckedAnswerIds.Add(poc.Id);
            }
        }

        #endregion

        #region Events

        private void OptionClicked(object sender, RoutedEventArgs e)
        {
            PollOptionControl poc = sender as PollOptionControl;
            if (poll.Multiple)
            {
                UpdateCheckedAnswerIds();
                Voters.Visibility = CheckedAnswerIds.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                MultiplePollVoteButton.Visibility = CheckedAnswerIds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                CheckedAnswerIds.Add(poc.Id);
                Vote();
            }
        }

        private void Vote(object sender, RoutedEventArgs e)
        {
            Vote();
        }

        private void SharePoll(object sender, RoutedEventArgs e)
        {
            Hide();
            InternalSharing ish = new InternalSharing(poll);
            ish.Show();
        }

        private void CloseModal(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        #endregion
    }
}
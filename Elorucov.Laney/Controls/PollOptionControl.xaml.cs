using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class PollOptionControl : UserControl {
        public ulong Id { get; set; }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
           "Text", typeof(string), typeof(PollOptionControl), new PropertyMetadata(string.Empty));

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty VotesProperty = DependencyProperty.Register(
           "Votes", typeof(int), typeof(PollOptionControl), new PropertyMetadata(0));

        public int Votes {
            get { return (int)GetValue(VotesProperty); }
            set { SetValue(VotesProperty, value); }
        }

        public static readonly DependencyProperty RateProperty = DependencyProperty.Register(
           "Rate", typeof(double), typeof(PollOptionControl), new PropertyMetadata(default(object)));

        public double Rate {
            get { return (double)GetValue(RateProperty); }
            set { SetValue(RateProperty, value); }
        }

        public static readonly DependencyProperty CanVoteProperty = DependencyProperty.Register(
           "CanVote", typeof(bool), typeof(PollOptionControl), new PropertyMetadata(default(object)));

        public bool CanVote {
            get { return (bool)GetValue(CanVoteProperty); }
            set { SetValue(CanVoteProperty, value); }
        }

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
           "IsChecked", typeof(bool), typeof(PollOptionControl), new PropertyMetadata(default(object)));

        public bool IsChecked {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public static readonly DependencyProperty IsMultivariantCheckBoxVisibleProperty = DependencyProperty.Register(
           "IsMultivariantCheckBoxVisible", typeof(bool), typeof(PollOptionControl), new PropertyMetadata(default(object)));

        public bool IsMultivariantCheckBoxVisible {
            get { return (bool)GetValue(IsMultivariantCheckBoxVisibleProperty); }
            set { SetValue(IsMultivariantCheckBoxVisibleProperty, value); }
        }

        public event RoutedEventHandler Click;

        public PollOptionControl() {
            this.InitializeComponent();
            Loaded += (c, d) => {
                SetUp();
                RegisterPropertyChangedCallback(TextProperty, (a, b) => SetUp());
                RegisterPropertyChangedCallback(VotesProperty, (a, b) => SetUp());
                RegisterPropertyChangedCallback(RateProperty, (a, b) => SetUp());
                RegisterPropertyChangedCallback(CanVoteProperty, (a, b) => SetUp());
                RegisterPropertyChangedCallback(IsCheckedProperty, (a, b) => SetUp());
                RegisterPropertyChangedCallback(IsMultivariantCheckBoxVisibleProperty, (a, b) => SetUp());
            };
            SizeChanged += (a, b) => SetUpRateBorder(true);
        }

        private void SetUp() {
            OptionText.Text = Text;
            OptionVotes.Text = CanVote ? string.Empty : $" · {Votes}";
            Result.Visibility = CanVote ? Visibility.Collapsed : Visibility.Visible;
            ResultCheckIcon.Visibility = IsChecked ? Visibility.Visible : Visibility.Collapsed;
            ResultRate.Text = $"{Math.Round(Rate, 1)}%";

            SetUpRateBorder();

            MultivariantCheckBox.IsChecked = IsChecked;
            if (!CanVote) {
                MultivariantCheckBox.Visibility = Visibility.Collapsed;
            } else {
                MultivariantCheckBox.Visibility = IsMultivariantCheckBoxVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SetUpRateBorder(bool noAnimate = false) {
            OptionRate.Visibility = CanVote ? Visibility.Collapsed : Visibility.Visible;
            if (!CanVote) {
                OptionRate.Width = ActualWidth / 100 * Rate;
            }
        }

        private void OnClick(object sender, RoutedEventArgs e) {
            if (!CanVote) return;
            if (IsMultivariantCheckBoxVisible) {
                IsChecked = !IsChecked;
            }
            Click?.Invoke(this, new RoutedEventArgs());
        }
    }
}

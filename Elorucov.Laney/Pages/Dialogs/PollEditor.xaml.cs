using Elorucov.Laney.Controls;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

// Документацию по шаблону элемента "Диалоговое окно содержимого" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class PollEditor : Modal {
        const string PollOptionTextBoxTag = "polloption";
        int PollBackgroundId = 0;

        public PollEditor() {
            this.InitializeComponent();
        }

        private void Init(object sender, RoutedEventArgs e) {
            AddOptionForm();
            AddOptionForm();
            LimitDate.Date = DateTime.Now.AddDays(1);
            LimitTime.Time = DateTime.Now.TimeOfDay;
            LimitDate.MinYear = DateTime.Now;

            PollBackgroundPreviews.Children.Add(BuildPollBackgroundPreview(new PollBackground() { Type = PollBackgroundType.Unknown }, ElementTheme.Default, true));

            new System.Action(async () => {
                object resp = await Polls.GetBackgrounds();
                if (resp is List<PollBackground>) {
                    List<PollBackground> backgrounds = resp as List<PollBackground>;
                    ShowPollBackgrounds(backgrounds);
                }
            })();
        }

        private void OptionsLayoutUpdated(object sender, object e) {
            AddOptionButton.IsEnabled = Options.Children.Count <= 10 ? true : false;
        }

        #region Options

        private void AddOptionForm() {
            if (Options.Children.Count <= 10) {
                Grid g = new Grid();
                g.Margin = new Thickness(0, 8, 0, 0);
                g.ColumnDefinitions.Add(new ColumnDefinition());
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBox tb = new TextBox();
                tb.Tag = PollOptionTextBoxTag;
                tb.MaxLength = 140;

                HyperlinkButton db = new HyperlinkButton();
                db.Width = 32; db.Height = 32;
                db.Padding = new Thickness(0);
                db.FontFamily = Theme.DefaultIconsFont;
                db.FontSize = 16;
                db.Content = "";
                db.Click += (a, b) => {
                    if (Options.Children.Count > 2) {
                        RemoveAnimation();
                        Options.Children.Remove(g);
                    }
                };

                Grid.SetColumn(tb, 0);
                Grid.SetColumn(db, 1);

                g.Children.Add(tb);
                g.Children.Add(db);

                AddAnimation();

                Options.Children.Insert(Options.Children.Count - 1, g);
            }
        }

        private void AddAnimation() {
            var ts = Options.ChildrenTransitions;
            bool hasAnimation = false;
            foreach (var t in ts) {
                if (t is AddDeleteThemeTransition) {
                    hasAnimation = true; break;
                }
            }
            if (!hasAnimation) ts.Add(new AddDeleteThemeTransition());
        }

        private void RemoveAnimation() {
            var ts = Options.ChildrenTransitions;
            AddDeleteThemeTransition tr = null;
            foreach (var t in ts) {
                if (t is AddDeleteThemeTransition) {
                    tr = t as AddDeleteThemeTransition; break;
                }
            }
            if (tr != null) ts.Remove(tr);
        }

        private void AddOption(object sender, RoutedEventArgs e) {
            AddOptionForm();
        }

        #endregion

        #region Backgrounds

        private void ShowPollBackgrounds(List<PollBackground> items) {
            foreach (var i in items) {
                PollBackgroundPreviews.Children.Add(BuildPollBackgroundPreview(i, ElementTheme.Dark));
            }
        }

        private StackPanel BuildPollBackgroundPreview(PollBackground i, ElementTheme theme = ElementTheme.Default, bool isChecked = false) {
            StackPanel sp = new StackPanel();
            sp.Width = 84;
            sp.Margin = new Thickness(0, 0, 12, 12);
            sp.Transitions = new TransitionCollection {
                new ContentThemeTransition { HorizontalOffset = 32, VerticalOffset = 0 }
            };

            PollBackgroundPreview pbp = new PollBackgroundPreview();
            pbp.Margin = new Thickness(0, 0, 0, 8);
            pbp.PollBackgroundColor = i.Type != PollBackgroundType.Unknown ? new SolidColorBrush(i.Color) : null;
            if (i.Type == PollBackgroundType.Gradient) {
                GradientStopCollection gsc = new GradientStopCollection();
                foreach (var p in i.Points) {
                    gsc.Add(new GradientStop { Offset = p.Position, Color = p.Color });
                }
                LinearGradientBrush lgb = new LinearGradientBrush(gsc, i.Angle);
                lgb.EndPoint = new Point(-lgb.EndPoint.X, -lgb.EndPoint.Y);
                pbp.PollBackground = lgb;
                pbp.RequestedTheme = theme;
            }
            sp.Children.Add(pbp);

            RadioButton rb = new RadioButton();
            rb.HorizontalAlignment = HorizontalAlignment.Center;
            rb.Width = 20;
            rb.MinWidth = 20;
            rb.GroupName = "pollbkg";
            rb.Tag = i.Id;
            rb.Checked += (a, b) => PollBackgroundId = i.Id;
            if (isChecked) rb.IsChecked = true;
            sp.Children.Add(rb);

            return sp;
        }

        #endregion

        private void SaveButtonClick(object sender, RoutedEventArgs args) {
            QuError.Visibility = Visibility.Collapsed;
            OpError.Visibility = Visibility.Collapsed;
            TimeError.Visibility = Visibility.Collapsed;

            TextBox firstOptionTB = null;

            string q = Question.Text;
            List<string> o = new List<string>();
            foreach (var a in Options.Children) {
                if (a is Grid && ((Grid)a).Children.Count > 0) {
                    TextBox tb = ((Grid)a).Children[0] as TextBox;
                    if (firstOptionTB == null) firstOptionTB = tb;
                    if (tb != null && (string)tb.Tag == PollOptionTextBoxTag) {
                        if (!string.IsNullOrEmpty(tb.Text)) o.Add(tb.Text);
                    }
                }
            }

            DateTimeOffset dt = LimitDate.Date.Date;
            dt = dt.AddTicks(LimitTime.Time.Ticks);
            long unixtime = Limited.IsChecked == true ? dt.ToUnixTimeSeconds() : 0;

            // Check errors

            if (string.IsNullOrEmpty(q)) {
                QuError.Visibility = Visibility.Visible;
                Question.Focus(FocusState.Programmatic);
                return;
            }
            if (o.Count == 0) {
                OpError.Visibility = Visibility.Visible;
                if (firstOptionTB != null) firstOptionTB.Focus(FocusState.Programmatic);
                return;
            }
            if (Limited.IsChecked == true && dt <= DateTime.Now) {
                TimeError.Visibility = Visibility.Visible;
                LimitDate.Focus(FocusState.Programmatic);
                return;
            }

            // Send request
            primarybtn.IsEnabled = false;
            secondarybtn.IsEnabled = false;
            new System.Action(async () => {
                object resp = await Polls.Create(q, o, PollBackgroundId, AnonymousPoll.IsChecked.Value, MultAnswers.IsChecked.Value, DisableUnvote.IsChecked.Value, unixtime);
                if (resp is Poll) {
                    Hide(resp as Poll);
                } else {
                    Functions.ShowHandledErrorDialog(resp);
                    primarybtn.IsEnabled = true;
                    secondarybtn.IsEnabled = true;
                }
            })();
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e) {
            Hide();
        }
    }
}

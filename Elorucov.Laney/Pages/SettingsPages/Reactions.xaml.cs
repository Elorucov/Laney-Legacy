using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.SettingsPages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Reactions : Page {
        public Reactions() {
            this.InitializeComponent();
            var host = Main.GetCurrent();
            BackButton.Visibility = host.IsWideMode ? Visibility.Collapsed : Visibility.Visible;
            host.SizeChanged += Host_SizeChanged;
            Unloaded += (a, b) => host.SizeChanged -= Host_SizeChanged;
        }

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e) {
            BackButton.Visibility = Main.GetCurrent().IsWideMode ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) {
            Main.GetCurrent().GoBack();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) {
            ShowQuickReactions();
            FastReactionButton.DataContext = AppParameters.FastReactionId;
        }

        ObservableCollection<Tuple<int, int>> quickReactionsWithPos = new ObservableCollection<Tuple<int, int>>();
        private void ShowQuickReactions() {
            if (ReactionsButtons.ItemsSource != null) quickReactionsWithPos.CollectionChanged -= QuickReactionsWithPos_CollectionChanged;
            quickReactionsWithPos.Clear();
            List<int> quickReactions = AppSession.SortedReactions.Take(7).ToList();
            for (int i = 0; i < quickReactions.Count; i++) {
                int rid = quickReactions[i];
                quickReactionsWithPos.Add(new Tuple<int, int>(i, rid));
            }
            if (ReactionsButtons.ItemsSource == null) ReactionsButtons.ItemsSource = quickReactionsWithPos;
            quickReactionsWithPos.CollectionChanged += QuickReactionsWithPos_CollectionChanged;
        }

        private void QuickReactionsWithPos_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            AppParameters.QuickReactions = String.Join(",", quickReactionsWithPos.Select(q => q.Item2));
            ShowQuickReactions();
        }

        private void Reset_Click(object sender, RoutedEventArgs e) {
            AppParameters.QuickReactions = null;
            ShowQuickReactions();
        }

        private void ReactionsButtons_ItemClick(object sender, ItemClickEventArgs e) {
            var tuple = e.ClickedItem as Tuple<int, int>;
            FrameworkElement el = ReactionsButtons.ContainerFromItem(tuple) as FrameworkElement;
            ShowReactionsPicker(el, false, (rid) => SetQuickReactions(rid, tuple.Item1));
        }

        private void Image_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            if (args.NewValue == null) return;
            var tuple = args.NewValue as Tuple<int, int>;
            int rid = tuple.Item2;
            Image image = sender as Image;
            image.Source = new SvgImageSource {
                UriSource = Reaction.GetImagePathById(rid)
            };
        }

        private void ShowReactionsPicker(FrameworkElement target, bool forFastReaction, Action<int> onClick) {
            Flyout flyout = new Flyout();
            VariableSizedWrapGrid ReactionsIcons = new VariableSizedWrapGrid {
                Margin = new Thickness(4),
                Width = 252,
                MaximumRowsOrColumns = 7,
                Orientation = Orientation.Horizontal
            };

            var list = forFastReaction ? AppSession.AvailableReactions : AppSession.SortedReactions.Skip(7);
            foreach (var rid in list) {
                HyperlinkButton hb = new HyperlinkButton {
                    BorderThickness = new Thickness(0),
                    Width = 36, Height = 36,
                    Padding = new Thickness(2),
                    Content = new Image {
                        Width = 32, Height = 32,
                        Source = new SvgImageSource {
                            UriSource = Reaction.GetImagePathById(rid),
                        },
                        Stretch = Stretch.Uniform
                    },
                };

                hb.Click += (a, c) => {
                    onClick?.Invoke(rid);
                    flyout.Hide();
                };

                Border br = new Border {
                    Width = 36, Height = 36,
                    CornerRadius = new CornerRadius(18),
                    Child = hb
                };

                ReactionsIcons.Children.Add(br);
            }

            if (forFastReaction) {
                Button disableButton = new Button {
                    Content = Locale.Get("disable"),
                    Width = 238,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 6, 0, 12)
                };
                disableButton.Click += (a, c) => {
                    onClick?.Invoke(0);
                    flyout.Hide();
                };

                StackPanel sp = new StackPanel();
                sp.Children.Add(ReactionsIcons);
                sp.Children.Add(disableButton);

                flyout.Content = sp;
            } else {
                flyout.Content = ReactionsIcons;
            }
            flyout.ShowAt(target);
        }

        private void SetQuickReactions(int rid, int position) {
            var sorted = AppSession.SortedReactions.Take(7).ToList();
            sorted[position] = rid;
            AppParameters.QuickReactions = String.Join(",", sorted);
            ShowQuickReactions();
        }

        private void FastReactionButton_Click(object sender, RoutedEventArgs e) {
            Button b = sender as Button;
            ShowReactionsPicker(b, true, (rid) => {
                AppParameters.FastReactionId = rid;
                b.DataContext = rid;
            });
        }

        private void Button_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            Button b = sender as Button;
            b.Content = null;
            if (args.NewValue is int rid) {
                if (AppSession.AvailableReactions.Contains(rid)) {
                    b.Content = new Image {
                        Width = 32, Height = 32,
                        Source = new SvgImageSource {
                            UriSource = Reaction.GetImagePathById(rid),
                        },
                        Stretch = Stretch.Uniform
                    };
                } else {
                    b.Content = new TextBlock {
                        Text = Locale.Get("choosebtn/Content"),
                        Margin = new Thickness(12, 0, 12, 0)
                    };
                }
            }
        }
    }
}
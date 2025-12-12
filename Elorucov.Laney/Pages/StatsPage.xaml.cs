using Elorucov.Laney.Models;
using Elorucov.Laney.Models.Stats;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Laney.ViewModel;
using Elorucov.VkAPI;
using System;
using System.Linq;
using System.Reflection;
using VK.VKUI.Popups;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StatsPage : Page {
        public StatsPage() {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()}");
            DateSelectionInfo.Text = Locale.Get("choose_period");

            if (!API.Initialized) {
                API.Initialize(AppParameters.AccessToken, Locale.Get("lang"), ApplicationInfo.UserAgent, AppParameters.ApplicationID, AppParameters.ApplicationSecret, AppParameters.VkApiDomain);
            }
        }

        StatsViewModel ViewModel { get { return DataContext as StatsViewModel; } }

        private void StatsPage_Loaded(object sender, RoutedEventArgs e) {
            App.FixMicaBackground();
            TitleAndStatusBar.ExtendView(true);
            TitleAndStatusBar.ChangeBackgroundColor(Colors.Transparent);

            if (AppParameters.DisplayRamUsage) {
                DbgRAMContainer.Visibility = Visibility.Visible;
                DispatcherTimer tmr = new DispatcherTimer();
                tmr.Interval = TimeSpan.FromSeconds(0.5);
                tmr.Tick += (c, d) => DbgRAM.Text = $"{Functions.GetMemoryUsageInMb()} Mb";
                tmr.Start();
            }

            new System.Action(async () => { await ViewModel.StartAsync(); })();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if (Theme.IsMicaAvailable) LayoutRoot.Background = null;

            if (e.Parameter is long id) {
                DataContext = new StatsViewModel(id);
            } else {
                Window.Current.Close();
            }
        }

        private int previouslySelectedDatesCount = 0;
        private void Calendar_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args) {
            sender.SelectedDatesChanged -= Calendar_SelectedDatesChanged;
            ViewModel.StartPeriod = null;
            ViewModel.EndPeriod = null;

            if (previouslySelectedDatesCount == 1 && sender.SelectedDates.Count == 2) {
                var selected = sender.SelectedDates.ToList();
                var early = selected.First();
                var later = selected.Last();

                if (later < early) {
                    later = selected.First();
                    early = selected.Last();
                }

                sender.SelectedDates.Clear();

                DateTimeOffset current = early;
                bool canProcess = true;
                for (int i = 0; canProcess; i++) {
                    sender.SelectedDates.Add(current);
                    current = current.AddDays(1);
                    canProcess = !(current.Day == later.Day && current.Month == later.Month && current.Year == later.Year);
                }
                sender.SelectedDates.Add(later);
                ViewModel.StartPeriod = early;
                ViewModel.EndPeriod = later;
                DateSelectionInfo.Text = $"{early.ToString("dd.MM.yyyy")} - {later.ToString("dd.MM.yyyy")}";
            } else {
                DateTimeOffset? single = null;
                if (args.AddedDates.Count == 1) {
                    single = args.AddedDates[0];
                } else if (args.RemovedDates.Count == 1) {
                    single = args.RemovedDates[0];
                }
                sender.SelectedDates.Clear();
                if (single != null) {
                    sender.SelectedDates.Add(single.Value);
                    DateSelectionInfo.Text = single?.ToString("dd.MM.yyyy");
                    ViewModel.StartPeriod = single;
                    ViewModel.EndPeriod = single;
                } else {
                    DateSelectionInfo.Text = Locale.Get("global_error");
                }
            }

            bool failed = ViewModel.StartPeriod == null || ViewModel.EndPeriod == null;
            if (failed) DateSelectionInfo.Text = Locale.Get("choose_period");
            CalcButton.IsEnabled = !failed;
            previouslySelectedDatesCount = sender.SelectedDates.Count;

            sender.SelectedDatesChanged += Calendar_SelectedDatesChanged;
        }

        private void SelectAllPeriod(object sender, RoutedEventArgs e) {
            Calendar.SelectedDatesChanged -= Calendar_SelectedDatesChanged;
            Calendar.SelectedDates.Clear();

            Calendar.SelectedDatesChanged += Calendar_SelectedDatesChanged;
            Calendar.SelectedDates.Add(Calendar.MinDate);
            Calendar.SelectedDates.Add(Calendar.MaxDate);
        }

        private void StartExport(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                HTMLStatsResultExport export = new HTMLStatsResultExport();
                ScreenSpinner<Exception> ssp = new ScreenSpinner<Exception>();
                var result = await ssp.ShowAsync(export.ExportAsync(ViewModel.Name, ViewModel.Avatar, ViewModel.Info, ViewModel.Result));
                if (result == null) {
                    await new ContentDialog {
                        Title = Locale.Get("donebtn/Content"),
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                } else {
                    Functions.ShowHandledErrorDialog(result);
                }
            })();
        }

        private void ReactionImage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            Image image = sender as Image;
            if (args.NewValue == null) {
                image.Visibility = Visibility.Collapsed;
                image.Tag = args.NewValue;
                return;
            }

            Entity reactionStat = image.DataContext as Entity;
            if (image.Tag != null && image.Tag is Entity or && reactionStat.Id == or.Id) return;

            Uri reactionImage = reactionStat.Image;
            if (reactionStat.Image != null) image.Source = new SvgImageSource {
                UriSource = reactionStat.Image
            };
        }


        // К сожалению, из-за этого костыля скроллинг горизонтальных элементов с помощью тачпада
        // НЕ будет работать, зато горизонтальный ScrollViewer не будет перехватывать вертикальный
        // скроллинг колёсиком мышки. (PointerWheelChanged не работает)

        private void HLV_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            ListView lv = sender as ListView;
            if (lv == null) return;
            bool isNotTouch = e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch;
            lv.IsHitTestVisible = !isNotTouch;
        }

        private void HLV_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e) {
            ListView lv = sender as ListView;
            if (lv == null) return;
            lv.IsHitTestVisible = true;
        }

        private void ScrollToLeft(object sender, RoutedEventArgs e) {
            Button button = sender as Button;
            ListView lv = button.Tag as ListView;
            if (lv == null) return;

            ScrollViewer sv = lv.GetScrollViewerFromListView();
            if (sv == null) return;

            double newOffset = Math.Max(0, sv.HorizontalOffset - 372);
            sv.ChangeView(newOffset, null, null);
        }

        private void ScrollToRight(object sender, RoutedEventArgs e) {
            Button button = sender as Button;
            ListView lv = button.Tag as ListView;
            if (lv == null) return;

            ScrollViewer sv = lv.GetScrollViewerFromListView();
            if (sv == null) return;

            double newOffset = Math.Min(sv.ScrollableWidth, sv.HorizontalOffset + 372);
            sv.ChangeView(newOffset, null, null);
        }

        private void ResultsViewSizeChanged(object sender, SizeChangedEventArgs e) {
            FrameworkElement root = sender as FrameworkElement;
            if (root == null) return;

            FrameworkElement tabs1 = root.FindName("TopMembersTabs") as FrameworkElement;
            FrameworkElement header1 = root.FindName("TopMembersTabsHeader") as FrameworkElement;
            FrameworkElement list1 = root.FindName("TopMembersListWrap") as FrameworkElement;

            FrameworkElement tabs2 = root.FindName("TopMembersByReactionsTabs") as FrameworkElement;
            FrameworkElement header2 = root.FindName("TopMembersByReactionsTabsHeader") as FrameworkElement;
            FrameworkElement list2 = root.FindName("TopMembersByReactionsListWrap") as FrameworkElement;

            if (tabs1 == null || header1 == null || list1 == null) return;
            if (tabs2 == null || header2 == null || list2 == null) return;

            SetupStickyHeaderForTabs(root, list1, header1, tabs1);
            SetupStickyHeaderForTabs(root, list2, header2, tabs2);
        }

        private void SetupStickyHeaderForTabs(FrameworkElement root, FrameworkElement list, FrameworkElement header, FrameworkElement block) {
            // Тут то же самое, что и в SetupStickyHeader, только для блоков с табами
            // (когда-то был pivot, но с ними были проблемы).
            // list — ListView (т. е. список участников) внутри таба.
            // header — элемент, где находятся сами табы.
            // tabs — контейнер с табами и списком.
            // root — дочерний элемент внутри ResultsViewRoot.

            ElementCompositionPreview.SetIsTranslationEnabled(header, true);
            Compositor c = ElementCompositionPreview.GetElementVisual(this).Compositor;

            float diff = (float)block.ActualHeight - (float)ResultsViewSV.ViewportHeight;
            if (diff < 0) diff = 0;

            GeneralTransform hgt = header.TransformToVisual(root);
            Point hp = hgt.TransformPoint(new Point(0, 0));
            float headerOffset = (float)hp.Y;

            Visual lvv = ElementCompositionPreview.GetElementVisual(list);
            InsetClip clip = c.CreateInsetClip();
            lvv.Clip = clip;

            CompositionHelper.SetupExpressionAnimation(c, ResultsViewSV, header, $"Clamp(-Scroll.Translation.Y - {headerOffset}, 0, {diff})", "Translation.Y");
            CompositionHelper.SetupExpressionClipAnimation(c, ResultsViewSV, clip, $"Clamp(-Scroll.Translation.Y - {headerOffset}, 0, {diff})", "TopInset");
        }

        private void ScrollToBlockStart(object sender, SelectionChangedEventArgs e) {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null) return;

            FrameworkElement target = element.Tag as FrameworkElement;
            if (target == null) return;

            FrameworkElement root = ResultsViewRoot.FindControlByName<StackPanel>("ResultsTemplateNarrowRoot");
            if (root == null) return;

            GeneralTransform gt = target.TransformToVisual(root);
            Point p = gt.TransformPoint(new Point(0, 0));

            ResultsViewSV.ChangeView(null, p.Y, null);
        }

        // В 1703 параметр SelectedIndex в GridView приводил к неизвесной ошибке (The parameter is incorrect) и Laney падал. Microsoft moment...
        private void SelectFirstTab(FrameworkElement sender, DataContextChangedEventArgs args) {
            sender.DataContextChanged -= SelectFirstTab;
            GridView gv = sender as GridView;
            gv.SelectedIndex = 0;
        }
    }
}
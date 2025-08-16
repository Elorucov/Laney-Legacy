using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Objects;
using System;
using VK.VKUI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Services.UI {
    public class VKButtonHelper {
        private static event EventHandler<string> CallbackActionReceived;
        public static void FireCallbackActionReceived(string eventId) {
            CallbackActionReceived?.Invoke(null, eventId);
        }

        public static Button GenerateButton(int messageId, BotButton bb, object eventSender, EventHandler<BotButtonAction> buttonClickHandler, bool inBubble = false) {
            Button btn = new Button();
            btn.HorizontalAlignment = HorizontalAlignment.Stretch;

            string label = bb.Action.Label;

            // btn.Click += (a, b) => { buttonClickHandler?.Invoke(eventSender, bb.Action); };
            btn.Click += (a, b) => { buttonClickHandler?.Invoke(btn, bb.Action); };

            switch (bb.Color) {
                case BotButtonColor.Default:
                    btn.Style = Application.Current.Resources[inBubble ? "VKButtonImBubbleLarge" : "VKButtonSecondaryLarge"] as Style;
                    break;
                case BotButtonColor.Primary:
                    btn.Style = Application.Current.Resources["VKButtonPrimaryLarge"] as Style;
                    break;
                case BotButtonColor.Positive:
                    btn.Style = Application.Current.Resources["VKButtonCommerceLarge"] as Style;
                    break;
                case BotButtonColor.Negative:
                    btn.Style = Application.Current.Resources["VKButtonCommerceLarge"] as Style;
                    btn.Background = Application.Current.Resources["VKDestructiveBrush"] as SolidColorBrush;
                    btn.Foreground = Application.Current.Resources["VKButtonCommerceForegroundBrush"] as SolidColorBrush;
                    break;
            }

            switch (bb.Action.Type) {
                case BotButtonType.VKPay:
                    btn.Background = Application.Current.Resources["VKAccentBrush"] as SolidColorBrush;
                    btn.Foreground = Application.Current.Resources["VKButtonCommerceForegroundBrush"] as SolidColorBrush;
                    btn.Padding = new Thickness(0);
                    btn.ContentTemplate = (DataTemplate)Application.Current.Resources["VKPayLabel"];
                    break;
                case BotButtonType.Location:
                    SetButtonContentWithIcon(btn, "Icon24Place", Locale.Get("botbtn_position"));
                    break;
                case BotButtonType.Callback:
                    SetDefaultButtonContent(btn, label, false, true);
                    break;
                case BotButtonType.OpenApp:
                    SetButtonContentWithIcon(btn, "Icon24Services", string.IsNullOrEmpty(label) ? Locale.Get("botbtn_services") : label);
                    break;
                case BotButtonType.OpenLink:
                    SetDefaultButtonContent(btn, label, true);
                    break;
                default:
                    SetDefaultButtonContent(btn, label, false);
                    break;
            }

            return btn;
        }

        private static void SetDefaultButtonContent(Button btn, string label, bool showLinkIcon, bool isCallbackButton = false) {
            btn.Padding = new Thickness(0);
            btn.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            ToolTipService.SetToolTip(btn, label);

            Grid g = new Grid();
            g.Height = 24;
            g.Margin = new Thickness(0, 5, 0, 5);
            g.HorizontalAlignment = HorizontalAlignment.Stretch;
            TextBlock lt = new TextBlock {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 17,
                Margin = new Thickness(0, -3, 0, 0),
                Text = label,
                TextAlignment = TextAlignment.Center
            };
            g.Children.Add(lt);
            if (isCallbackButton) {
                btn.Click += (a, b) => {
                    btn.IsEnabled = false;
                    lt.Visibility = Visibility.Collapsed;
                    Spinner spinner = new Spinner { Width = 16, Height = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    g.Children.Add(spinner);

                    string boundEventId = (string)btn.Tag;

                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(60);
                    timer.Tick += (c, d) => {
                        Logger.Log.Info($"Callback action not received within a minute. boundEventId = {boundEventId}");
                        timer.Stop();
                        spinner.Visibility = Visibility.Collapsed;
                        lt.Visibility = Visibility.Visible;
                        btn.IsEnabled = true;
                    };
                    timer.Start();

                    CallbackActionReceived += (c, d) => {
                        Logger.Log.Info($"Callback action received: eventId = {d}; boundEventId = {boundEventId}");
                        if (d == (string)btn.Tag) {
                            timer.Stop();
                            spinner.Visibility = Visibility.Collapsed;
                            lt.Visibility = Visibility.Visible;
                            btn.IsEnabled = true;
                        }
                    };
                };
            }
            if (showLinkIcon) {
                g.Children.Add(new ContentPresenter {
                    ContentTemplate = (DataTemplate)Application.Current.Resources["Icon16Up"],
                    Width = 12,
                    Height = 12,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    RenderTransform = new CompositeTransform { Rotation = 45, TranslateX = 6, TranslateY = -6 }
                });
            }
            btn.Content = g;
        }

        private static void SetButtonContentWithIcon(Button btn, string iconres, string label) {
            btn.Padding = new Thickness(0);
            StackPanel sp = new StackPanel();
            sp.Height = 24;
            sp.Orientation = Orientation.Horizontal;
            sp.Margin = new Thickness(8, 5, 8, 5);
            sp.Children.Add(new ContentPresenter {
                Height = 24, ContentTemplate = (DataTemplate)Application.Current.Resources[iconres]
            });
            sp.Children.Add(new TextBlock {
                VerticalAlignment = VerticalAlignment.Center, FontSize = 17, Margin = new Thickness(6, -3, 0, 0), Text = label
            });
            ToolTipService.SetToolTip(btn, label);
            btn.Content = sp;
        }
    }
}

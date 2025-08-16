using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels;
using System;
using System.Threading.Tasks;
using VK.VKUI.Controls;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Helpers.UI
{
    public class MessageKeyboardHelper
    {
        private static event EventHandler<int> CallbackActionReceived;
        public static void FireCallbackActionReceived(int messageId)
        {
            CallbackActionReceived?.Invoke(null, messageId);
        }

        public static Button GenerateButton(int messageId, BotButton bb, object eventSender, EventHandler<BotButtonAction> buttonClickHandler)
        {
            Button btn = new Button();
            btn.HorizontalAlignment = HorizontalAlignment.Stretch;

            string label = bb.Action.Label;

            btn.Click += (a, b) => { buttonClickHandler?.Invoke(eventSender, bb.Action); };

            switch (bb.Color)
            {
                case BotButtonColor.Default:
                    btn.Style = Application.Current.Resources["VKButtonSecondaryLarge"] as Style;
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

            switch (bb.Action.Type)
            {
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
                    SetDefaultButtonContent(btn, label, false, messageId);
                    break;
                case BotButtonType.OpenApp:
                    SetButtonContentWithIcon(btn, "Icon24Services", String.IsNullOrEmpty(label) ? Locale.Get("botbtn_services") : label);
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

        private static void SetDefaultButtonContent(Button btn, string label, bool showLinkIcon, int messageId = -1)
        {
            btn.Padding = new Thickness(0);
            btn.HorizontalContentAlignment = HorizontalAlignment.Stretch;

            Grid g = new Grid();
            g.Height = 24;
            g.Margin = new Thickness(0, 5, 0, 5);
            g.HorizontalAlignment = HorizontalAlignment.Stretch;
            TextBlock lt = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 17,
                Margin = new Thickness(0, -3, 0, 0),
                Text = label,
                TextAlignment = TextAlignment.Center
            };
            g.Children.Add(lt);
            if (messageId >= 0)
            {
                btn.Click += (a, b) =>
                {
                    lt.Visibility = Visibility.Collapsed;
                    Spinner spinner = new Spinner { Width = 16, Height = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    g.Children.Add(spinner);
                    CallbackActionReceived += (c, d) =>
                    {
                        if (d == messageId)
                        {
                            Log.General.Info("Callback action received", new ValueSet { { "message_id", d } });
                            spinner.Visibility = Visibility.Collapsed;
                            lt.Visibility = Visibility.Visible;
                        }
                    };
                };
            }
            if (showLinkIcon)
            {
                g.Children.Add(new ContentPresenter
                {
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

        private static void SetButtonContentWithIcon(Button btn, string iconres, string label)
        {
            btn.Padding = new Thickness(0);
            StackPanel sp = new StackPanel();
            sp.Height = 24;
            sp.Orientation = Orientation.Horizontal;
            sp.Margin = new Thickness(8, 5, 8, 5);
            sp.Children.Add(new ContentPresenter
            {
                Height = 24,
                ContentTemplate = (DataTemplate)Application.Current.Resources[iconres]
            });
            sp.Children.Add(new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 17,
                Margin = new Thickness(6, -3, 0, 0),
                Text = label,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            btn.Content = sp;
        }

        public static async Task<bool> DoAction(BotButtonAction action, int ownerMessageId = 0, int authorId = 0)
        {
            try
            {
                ConversationViewModel cvm = ConversationViewModel.CurrentFocused;
                Log.General.Info("Bot button clicked", new ValueSet { { "action_type", action.Type }, { "current_conv_id", cvm.Id },
                    { "owner_message_id", ownerMessageId }, { "author_id", authorId } });
                switch (action.Type)
                {
                    case BotButtonType.Location:
                        var geo = await LocationHelper.GetCurrentGeopositionAsync();
                        if (geo == null) return false;
                        Tuple<double, double> l = new Tuple<double, double>(geo.Value.Latitude, geo.Value.Longitude);
                        MessageViewModel mvm = MessageViewModel.Build(Int32.MaxValue, DateTime.Now, cvm.Id, null, Locale.Get("place"), null, null, l, null, action.Payload);
                        cvm.Messages.Insert(mvm);
                        mvm.SendOrEditMessage();
                        return true;

                    case BotButtonType.OpenApp:
                        string link = String.IsNullOrEmpty(action.Hash) ? $"https://m.vk.com/app{action.AppId}" : $"https://m.vk.com/app{action.AppId}#{action.Hash}";
                        return await Launcher.LaunchUriAsync(new Uri(link));

                    case BotButtonType.OpenLink:
                        return await Launcher.LaunchUriAsync(action.LinkUri);

                    case BotButtonType.Text:
                        ELOR.VKAPILib.Objects.Group g = CacheManager.GetGroup(action.OwnerId);
                        string n = g != null ? $"[club{-action.OwnerId}|{g.ScreenName}] {action.Label}" : $"[club{-action.OwnerId}|{action.Label}]";
                        string text = cvm.Id > 2000000000 ? n : action.Label;
                        MessageViewModel mvm2 = MessageViewModel.Build(Int32.MaxValue, DateTime.Now, cvm.Id, null, text, null, null, null, null, action.Payload);
                        cvm.Messages.Insert(mvm2);
                        mvm2.SendOrEditMessage();
                        return true;

                    case BotButtonType.VKPay:
                        return await Launcher.LaunchUriAsync(new Uri($"https://m.vk.com/app6217559#{action.Hash}"));

                    case BotButtonType.Callback:
                        await Task.Delay(10);
                        var r = await VKSession.Current.API.Messages.SendMessageEventAsync(cvm.Id, action.Payload, ownerMessageId, authorId);
                        FireCallbackActionReceived(ownerMessageId);
                        return true;

                    default: return false;
                }
            }
            catch (Exception ex)
            {
                if (await ExceptionHelper.ShowErrorDialogAsync(ex)) return await DoAction(action, ownerMessageId, authorId);
                return false;
            }
        }
    }
}
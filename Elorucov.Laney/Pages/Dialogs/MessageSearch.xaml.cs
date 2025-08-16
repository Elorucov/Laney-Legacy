using Elorucov.Laney.Controls;
using Elorucov.Laney.Models;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.UI;
using Elorucov.Toolkit.UWP.Controls;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Pages.Dialogs {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MessageSearch : Modal {
        long peerId = 0;
        DateTime FilterDate = DateTime.Now.Date;
        ScrollViewer ListScrollViewer;
        ObservableCollection<LMessage> FoundMessages = new ObservableCollection<LMessage>();

        public MessageSearch(Tuple<long, string> conversationInfo) {
            this.InitializeComponent();
            Log.Info($"Init {GetType().GetTypeInfo().BaseType.Name} {GetType()} > peer id: {conversationInfo.Item1}");
            peerId = conversationInfo.Item1;
            Title = $"{Locale.Get("msgsearch")} — {conversationInfo.Item2}";

            Loaded += async (a, b) => {
                if (ListScrollViewer == null) {
                    ListScrollViewer = listView.GetScrollViewerFromListView();
                    ListScrollViewer.ViewChanged += ListScrollViewer_ViewChanged;
                }

                listView.ItemsSource = FoundMessages;

                CalendarDP.MinDate = new DateTime(2006, 10, 10, 0, 0, 0);
                CalendarDP.MaxDate = DateTime.Now;

                // Focus to text box
                await Task.Delay(200);
                Query.Focus(FocusState.Keyboard);
            };
        }

        private void ListScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            if (e.IsIntermediate) {
                ScrollViewer sv = sender as ScrollViewer;
                if (sv.VerticalOffset >= sv.ScrollableHeight - 4) {
                    if (!progress.IsIndeterminate && FoundMessages.Count != 0) new System.Action(async () => { await StartSearch(); })();
                }
            }
        }

        private async Task StartSearch() {
            progress.IsIndeterminate = true;
            string q = Query.Text;
            object resp = await Messages.Search(q, peerId, 0, FoundMessages.Count, 60, APIHelper.ConvertDateToVKFormat(FilterDate), VkAPI.API.WebToken);
            if (resp is MessagesHistoryResponse) {
                MessagesHistoryResponse scr = resp as MessagesHistoryResponse;
                AppSession.AddUsersToCache(scr.Profiles);
                AppSession.AddGroupsToCache(scr.Groups);
                AppSession.AddContactsToCache(scr.Contacts);

                foreach (var c in scr.Items) {
                    FoundMessages.Add(new LMessage(c));
                }
            } else {
                Functions.ShowHandledErrorDialog(resp);
            }
            progress.IsIndeterminate = false;
        }

        #region Context Menu

        private void ShowConversationContextMenu(UIElement sender, ContextRequestedEventArgs args) {
            FrameworkElement el = sender as FrameworkElement;
            LMessage msg = el.DataContext as LMessage;
            if (msg == null) return;

            MenuFlyout mf = new MenuFlyout();

            if (AppParameters.AdvancedMessageInfo) {
                MenuFlyoutItem dbg = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = $"MID: {msg.Id} CMID: {msg.ConversationMessageId}" };
                dbg.Click += (a, b) => {
                    Dev.MessageJSONView mjv = new Dev.MessageJSONView(msg);
                    mjv.Show();
                };
                mf.Items.Add(dbg);
                mf.Items.Add(new MenuFlyoutSeparator());
            }

            MenuFlyoutItem gotom = new MenuFlyoutItem { Icon = new FixedFontIcon { Glyph = "" }, Text = Locale.Get("go_to_message") };

            gotom.Click += (a, b) => {
                Hide(msg);
            };

            mf.Items.Add(gotom);

            Point pos;
            bool posReached = args != null && args.TryGetPosition(el, out pos);

            if (mf.Items.Count > 0) {
                if (posReached) {
                    mf.ShowAt(el, pos);
                } else {
                    mf.ShowAt(el);
                }
            }
        }

        #endregion

        private void CalendarDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) {
            if (args.NewDate == null || args.NewDate > DateTimeOffset.Now) {
                FilterDate = DateTime.Now;
            } else {
                FilterDate = args.NewDate.Value.Date;
            }
            FoundMessages.Clear();
            new System.Action(async () => { await StartSearch(); })();
        }

        private void ListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args) {
            if (args.ItemContainer != null && args.Item != null) {
                LMessage msg = args.Item as LMessage;

                if (args.ItemContainer.ContentTemplateRoot is Border b) {
                    args.ItemContainer.BorderThickness = new Thickness(0);
                    b.Child = MessageUIHelper.Build(msg, null, null, true, true);
                }
            }
        }

        private void Query_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            FoundMessages.Clear();
            new System.Action(async () => { await StartSearch(); })();
        }
    }
}

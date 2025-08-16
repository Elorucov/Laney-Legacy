using Elorucov.Laney.Models.Stats;
using Elorucov.Laney.Services;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Execute;
using Elorucov.Laney.Services.Execute.Objects;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.ViewModel.Controls;
using Elorucov.VkAPI;
using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VK.VKUI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.ViewModel {
    public class StatsViewModel : BaseViewModel {
        private string _name;
        private string _info;
        private Uri _avatar;
        private PlaceholderViewModel _placeholder;
        private bool _isInSetupMode;
        private DateTimeOffset _startDate;
        private DateTimeOffset _endDate;
        private DateTimeOffset? _startPeriod;
        private DateTimeOffset? _endPeriod;

        private bool _isLoading;
        private double _currentProgress;
        private string _currentProgressInfo;
        private StatsResult _result;

        private RelayCommand _calcCommand;

        public string Name { get { return _name; } private set { _name = value; OnPropertyChanged(); } }
        public string Info { get { return _info; } private set { _info = value; OnPropertyChanged(); } }
        public Uri Avatar { get { return _avatar; } private set { _avatar = value; OnPropertyChanged(); } }
        public PlaceholderViewModel Placeholder { get { return _placeholder; } private set { _placeholder = value; OnPropertyChanged(); } }
        public bool IsInSetupMode { get { return _isInSetupMode; } private set { _isInSetupMode = value; OnPropertyChanged(); } }
        public DateTimeOffset StartDate { get { return _startDate; } private set { _startDate = value; OnPropertyChanged(); } }
        public DateTimeOffset EndDate { get { return _endDate; } private set { _endDate = value; OnPropertyChanged(); } }
        public DateTimeOffset? StartPeriod { get { return _startPeriod; } set { _startPeriod = value; OnPropertyChanged(); } }
        public DateTimeOffset? EndPeriod { get { return _endPeriod; } set { _endPeriod = value; OnPropertyChanged(); } }

        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; OnPropertyChanged(); } }
        public double CurrentProgress { get { return _currentProgress; } private set { _currentProgress = value; OnPropertyChanged(); } }
        public string CurrentProgressInfo { get { return _currentProgressInfo; } private set { _currentProgressInfo = value; OnPropertyChanged(); } }
        public StatsResult Result { get { return _result; } private set { _result = value; OnPropertyChanged(); } }

        public RelayCommand CalcCommand { get { return _calcCommand; } private set { _calcCommand = value; OnPropertyChanged(); } }

        private long PeerId;
        private string PeerType = string.Empty;
        private int FirstMessageCMID = 0;

        public StatsViewModel(long peerId) {
            PeerId = peerId;
            CalcCommand = new RelayCommand(PrepareToLoadMessages);
        }

        bool started = false;
        public async Task StartAsync() {
            if (started) return;
            started = true;

            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            var response = await ssp.ShowAsync(Execute.StatsPrepare(PeerId));
            if (response is StatsPrepareResponse resp) {
                if (AppSession.ReactionsAssets == null) AppSession.ReactionsAssets = resp.ReactionsAssets;
                if (resp.MessagesCount == 0) {
                    var result = new ContentDialog {
                        Title = Locale.Get("global_error"),
                        Content = Locale.Get("no_messages"),
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                    Window.Current.Close();
                    return;
                }

                PeerType = resp.Type;
                FirstMessageCMID = resp.FirstCMID;

                StartDate = DateTimeOffset.FromUnixTimeSeconds(resp.FirstDate);
                EndDate = DateTimeOffset.FromUnixTimeSeconds(resp.LastDate);
                string startStr = StartDate.ToString("dd.MM.yyyy");
                // string endStr = EndDate.ToString("dd.MM.yyyy");
                string countStr = String.Format(Locale.GetDeclensionForFormat(resp.MessagesCount, "messages"), resp.MessagesCount);
                string dateStr = String.Format(Locale.GetForFormat("starting_from"), startStr);

                Name = resp.Name;
                Info = $"{countStr} {dateStr}";
                if (Uri.IsWellFormedUriString(resp.Avatar, UriKind.Absolute)) Avatar = new Uri(resp.Avatar);

                IsInSetupMode = true;
            } else {
                Functions.ShowHandledErrorDialog(response, async () => {
                    started = false;
                    await StartAsync();
                });
            }
        }

        private void PrepareToLoadMessages(object obj) {
            if (StartPeriod == null || EndPeriod == null) return;
            new System.Action(async () => { await PrepareToLoadMessagesAsync(); })();
        }

        private async Task PrepareToLoadMessagesAsync(bool noStartFix = false) {
            var startFix = noStartFix ? StartPeriod.Value : StartPeriod.Value.AddDays(-1);
            string start = startFix.ToString("ddMMyyyy");
            string end = EndPeriod.Value.ToString("ddMMyyyy");
            if (noStartFix && StartPeriod.Value > EndPeriod.Value) {
                var result = new ContentDialog {
                    Title = Locale.Get("global_error"),
                    Content = Locale.Get("choose_period_err"),
                    PrimaryButtonText = Locale.Get("close")
                }.ShowAsync();
                return;
            }

            ScreenSpinner<object> ssp = new ScreenSpinner<object>();
            var response = await ssp.ShowAsync(Execute.StatsGetIdsByRange(PeerId, start, end));
            if (response is StatsRangeResponse resp) {
                if (resp.MessagesCount == 0) {
                    var result = new ContentDialog {
                        Title = Locale.Get("global_error"),
                        Content = Locale.Get("no_messages"),
                        PrimaryButtonText = Locale.Get("close")
                    }.ShowAsync();
                    return;
                } else if (resp.FirstCMID == 0 && resp.LastCMID > 0) {
                    Log.Warn($"StatsViewModel > PrepareToLoadMessages: FirstCMID is 0!");
                    if (noStartFix) throw new Exception("Invalid data from VK API! (first message's CMID is 0)");
                    await PrepareToLoadMessagesAsync(true);
                    return;
                }
                await LoadMessagesAsync(resp.FirstCMID, resp.LastCMID, resp.MessagesCount);
            } else {
                Functions.ShowHandledErrorDialog(response, async () => await PrepareToLoadMessagesAsync(noStartFix));
            }
        }

        Stopwatch sw1 = new Stopwatch();
        private async Task LoadMessagesAsync(int firstCMID, int lastCMID, int messagesCount) {
            IsLoading = true;
            int offset = 0;
            List<MessageLite> loadedMessages = new List<MessageLite>();
            List<int> reactedMessagesIdsForLoad = new List<int>();

            sw1.Start();
            bool isWorking = true;
            while (isWorking) {
                try {
                    double percent = (double)100 / (double)messagesCount * (double)offset;
                    CurrentProgress = Math.Floor(percent);
                    CurrentProgressInfo = $"{Locale.Get("loading_messages")} ({String.Format(Locale.GetForFormat("out_of"), offset, messagesCount)})";
                    if (reactedMessagesIdsForLoad.Count > 0) CurrentProgressInfo += $"\n({Locale.Get("phase")} {String.Format(Locale.GetForFormat("out_of"), 1, 2)})";
                    await Task.Delay(500).ConfigureAwait(false);

                    var response = await GetHistoryLite(PeerId, lastCMID, offset, 200);
                    if (response is VKListLite<MessageLite> messages) {
                        if (messages.Items.Count == 0) {
                            isWorking = false;
                            sw1.Stop();
                            break;
                        }
                        AppSession.AddUsersToCache(messages.Profiles);
                        AppSession.AddGroupsToCache(messages.Groups);
                        foreach (MessageLite message in messages.Items) {
                            loadedMessages.Add(message);

                            if (message.Reactions != null && message.Reactions.Count > 0) {
                                bool hasReactionsWithoutUserIds = message.Reactions.Any(r => r.UserIds.Count == 0);
                                if (hasReactionsWithoutUserIds) reactedMessagesIdsForLoad.Add(message.ConversationMessageId);
                            }

                            if (message.ConversationMessageId == firstCMID || (firstCMID == 0 && loadedMessages.Count == messagesCount)) {
                                isWorking = false;
                                break;
                            }
                        }
                        if (!isWorking) {
                            sw1.Stop();
                            await GetReactedPeersAsync(loadedMessages, reactedMessagesIdsForLoad);
                            break;
                        }
                        offset = offset + messages.Items.Count;
                    } else {
                        sw1.Stop();
                        IsLoading = false;
                        Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await LoadMessagesAsync(firstCMID, lastCMID, messagesCount));
                        break;
                    }
                } catch (Exception ex) {
                    sw1.Stop();
                    IsLoading = false;
                    Placeholder = PlaceholderViewModel.GetForHandledError(ex, async () => await LoadMessagesAsync(firstCMID, lastCMID, messagesCount));
                    break;
                }
            }
        }

        Stopwatch sw2 = new Stopwatch();
        private async Task GetReactedPeersAsync(List<MessageLite> loadedMessages, List<int> reactedMessagesIdsForLoad) {
            Dictionary<int, List<ReactedPeer>> reactedPeers = new Dictionary<int, List<ReactedPeer>>(); // key = cmid

            if (reactedMessagesIdsForLoad.Count == 0) {
                StartJourney(loadedMessages, reactedPeers);
                return;
            }

            CurrentProgress = 0;
            CurrentProgressInfo = Locale.Get("loading_reacted_peers");
            CurrentProgressInfo += $" ({Locale.Get("phase")} {String.Format(Locale.GetForFormat("out_of"), 1, 2)})";

            sw2.Start();

            // Получаем участников, которые отреагировали на сообщения reactedMessagesIdsForLoad.
            int offset = 0;
            const int chunk = 20;
            int count = reactedMessagesIdsForLoad.Count;

            while (offset < count) {
                try {
                    double percent = (double)100 / (double)count * (double)offset;
                    CurrentProgress = Math.Floor(percent);
                    CurrentProgressInfo = $"{Locale.Get("loading_reacted_peers")} ({String.Format(Locale.GetForFormat("out_of"), offset, count)})";
                    CurrentProgressInfo += $"\n({Locale.Get("phase")} {String.Format(Locale.GetForFormat("out_of"), 2, 2)})";
                    await Task.Delay(500).ConfigureAwait(false);
                    var cmids = reactedMessagesIdsForLoad.Skip(offset).Take(chunk).ToList();

                    var response = await Execute.GetReactedPeersMulti(PeerId, cmids);
                    if (response is List<GetReactedPeersResponse> resp) {
                        foreach (var rpr in resp) {
                            AppSession.AddUsersToCache(rpr.Profiles);
                            AppSession.AddGroupsToCache(rpr.Groups);

                            if (!reactedPeers.ContainsKey(rpr.CMID)) reactedPeers.Add(rpr.CMID, rpr.Reactions);
                        }
                        offset = offset + chunk;
                    } else {
                        sw2.Stop();
                        IsLoading = false;
                        Placeholder = PlaceholderViewModel.GetForHandledError(response, async () => await GetReactedPeersAsync(loadedMessages, reactedMessagesIdsForLoad));
                        break;
                    }
                } catch (Exception ex) {
                    sw2.Stop();
                    IsLoading = false;
                    Placeholder = PlaceholderViewModel.GetForHandledError(ex, async () => await GetReactedPeersAsync(loadedMessages, reactedMessagesIdsForLoad));
                    break;
                }
            }

            sw2.Stop();
            StartJourney(loadedMessages, reactedPeers);
        }

        private void StartJourney(List<MessageLite> loadedMessages, Dictionary<int, List<ReactedPeer>> reactedPeers) {
            Log.Info($"Loaded {loadedMessages.Count} messages. Used RAM: {Functions.GetMemoryUsageInMb()} Mb.");

            CurrentProgress = 100;
            CurrentProgressInfo = Locale.Get("stats_calculating");

            // Отображение результатов.
            try {
                Info = String.Format(Locale.GetForFormat("stats_period"), StartPeriod?.ToString("dd.MM.yyyy"), EndPeriod?.ToString("dd.MM.yyyy"));
                Result = new StatsResult(loadedMessages, reactedPeers, sw1.Elapsed, sw2.Elapsed, PeerType == "chat");
                IsInSetupMode = false;
                IsLoading = false;
            } catch (Exception ex) {
                Placeholder = PlaceholderViewModel.GetForHandledError(ex);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private async Task<object> GetHistoryLite(long peerId, int startCmid, int offset = 0, int count = 40) {
            Dictionary<string, string> req = new Dictionary<string, string> {
                { "peer_id", peerId.ToString() },
                { "start_cmid", startCmid.ToString() },
                { "offset", offset.ToString() },
                { "count", count.ToString() },
                { "extended", "1" },
                { "fields", "photo_50,photo_100,has_photo" }
            };

            var res = await API.SendRequestAsync("messages.getHistory", req);
            return VKResponseHelper.ParseResponse<VKListLite<MessageLite>>(res);
        }
    }
}
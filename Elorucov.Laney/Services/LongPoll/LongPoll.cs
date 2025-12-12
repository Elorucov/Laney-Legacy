using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.VkAPI.Methods;
using Elorucov.VkAPI.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Popups;

namespace Elorucov.Laney.Services.LongPoll {
    public class LPOnlineInfo {
        public bool IsOnline { get; set; }
        public long UserId { get; set; }
        public int Platform { get; set; }
        public long AppId { get; set; }
        public bool IsMobile { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class LPTypingInfo {
        public long ConversationId { get; set; }
        public List<long> TypingUsers { get; set; } = new List<long>();
        public List<long> RecordingVoiceUsers { get; set; } = new List<long>();
    }

    [DataContract]
    public enum LPBotCallbackActionType {
        [EnumMember(Value = "show_snackbar")]
        ShowSnackbar,

        [EnumMember(Value = "open_link")]
        OpenLink,

        [EnumMember(Value = "open_app")]
        OpenApp,

        [EnumMember(Value = "open_modal_view")]
        OpenModalView,
    }

    public class LPBotCallbackAction {
        [JsonProperty("type")]
        public LPBotCallbackActionType Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("app_id")]
        public long AppId { get; set; }

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }
    }

    public class LPBotCallback {
        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("peer_id")]
        public long PeerId { get; set; }

        [JsonProperty("event_id")]
        public string EventId { get; set; }

        [JsonProperty("action")]
        public LPBotCallbackAction Action { get; set; }
    }

    public class LPTranslation {
        [JsonProperty("peer_id")]
        public long PeerId { get; set; }

        [JsonProperty("cmid")]
        public int ConversationMessageId { get; set; }

        [JsonProperty("translation")]
        public string Translation { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("error")]
        public int Error { get; set; }
    }

    public enum ReactionEventType {
        Unknown, IAdded, SomeoneAdded, IRemoved, SomeoneRemoved
    }

    public enum LPMessageFlagState {
        Changed = 1, Set = 2, Reset = 3
    }

    public enum AppBackgroundState {
        Entered, Leaving, Leaved
    }

    public enum LongPollActivityStatus { Typing, RecordingVoiceMessage, SendingPhoto, SendingVideo, SendingFile }

    public class LongPollActivityInfo {
        internal LongPollActivityInfo(long id, LongPollActivityStatus status) {
            MemberId = id;
            Status = status;
        }

        public long MemberId { get; private set; }

        public LongPollActivityStatus Status { get; private set; }

        public override string ToString() {
            return $"{MemberId}={Status}";
        }
    }

    public class LongPoll {
        #region Variables

        static bool IsLPCycleEnabled = false;

        const int WaitTime = 25;
        const int Mode = 234;
        const int Version = 19;

        static string Server = null;
        static string Key = null;
        static string TimeStamp;
        static string PTS = null;
        // static int LastMessageId = 0;

        public static bool IsInitialized { get; private set; }
        public static bool IsInternetAvailable { get { return NetworkInformation.GetInternetConnectionProfile() != null; } }

        private static int msgrFromLP, msgrFromAPI, msgrErrors = 0;

        #endregion

        static List<string> botCallbackEventIds = new List<string>();

        public static void AddBotCallbackEventId(string eventId) {
            if (!botCallbackEventIds.Contains(eventId)) botCallbackEventIds.Add(eventId);
        }



        #region Events

        public delegate void DEBUGLongPollResponseDelegate(string response);
        public static event DEBUGLongPollResponseDelegate DEBUGLongPollResponseReceived;

        public delegate void FailedDelegate();
        public static event FailedDelegate Failed;

        public delegate void CounterUpdatedDelegate(int unread, int unreadUnmuted, bool onlyUnmuted, int archiveUnread, int archiveUnreadUnmuted, int archiveMentions);
        public static event CounterUpdatedDelegate CounterUpdated;

        public delegate void MessageReceivedDelegate(Message message);
        public static event MessageReceivedDelegate MessageReceived;

        public delegate void DefaultKeyboardReceivedDelegate(BotKeyboard keyboard, long peerId);
        public static event DefaultKeyboardReceivedDelegate DefaultKeyboardReceived;

        public delegate void MessageEditedDelegate(Message message);
        public static event MessageEditedDelegate MessageEdited;

        public delegate void UserOnlineOrOfflineDelegate(LPOnlineInfo lpoi);
        public static event UserOnlineOrOfflineDelegate UserOnlineOrOffline;

        public delegate void UserActivityStateChangedDelegate(Tuple<long, List<LongPollActivityInfo>> info);
        public static event UserActivityStateChangedDelegate UserActivityStateChanged;

        public delegate void FlagsEventDelegate(LPMessageFlagState state, int msgid, long peerId, int flags);
        public static event FlagsEventDelegate FlagsEvent;

        public delegate void MessageMarkedAsReadDelegate(long peerId, int messageId, int unreadCount, bool isOutgoing);
        public static event MessageMarkedAsReadDelegate MessageMarkedAsRead;

        public delegate void BotCallbackReceivedDelegate(LPBotCallback callback);
        public static event BotCallbackReceivedDelegate BotCallbackReceived;

        public delegate void Event10ReceivedDelegate(long peerId, int flags);
        public static event Event10ReceivedDelegate Event10Received;

        public delegate void Event12ReceivedDelegate(long peerId, int flags);
        public static event Event12ReceivedDelegate Event12Received;

        public delegate void ConversationMentionReceivedDelegate(long peerId, int messageId, bool isBomb);
        public static event ConversationMentionReceivedDelegate ConversationMentionReceived;

        public delegate void ConversationDeletedDelegate(long peerId, int lastMessageId);
        public static event ConversationDeletedDelegate ConversationDeleted;

        public delegate void FolderCreatedDelegate(int id, string name, int randomId);
        public static event FolderCreatedDelegate FolderCreated;

        public delegate void FolderDeletedDelegate(int id);
        public static event FolderDeletedDelegate FolderDeleted;

        public delegate void FolderRenamedDelegate(int id, string name);
        public static event FolderRenamedDelegate FolderRenamed;

        public delegate void FolderConversationsAddedDelegate(int id, List<long> conversationsIds);
        public static event FolderConversationsAddedDelegate FolderConversationsAdded;

        public delegate void FolderConversationsRemovedDelegate(int id, List<long> conversationsIds);
        public static event FolderConversationsRemovedDelegate FolderConversationsRemoved;

        public delegate void FoldersReorderedDelegate(List<int> foldersIds);
        public static event FoldersReorderedDelegate FoldersReordered;

        public delegate void FoldersUnreadCountersChangedDelegate(int id, int unread, int unreadUnmuted);
        public static event FoldersUnreadCountersChangedDelegate FoldersUnreadCountersChanged;

        public delegate void ReactionsChangedDelegate(long peerId, int cmId, ReactionEventType type, int myReactionId, List<Reaction> reactions);
        public static event ReactionsChangedDelegate ReactionsChanged;

        public delegate void UnreadReactionsChangedDelegate(long peerId, List<int> cmIds);
        public static event UnreadReactionsChangedDelegate UnreadReactionsChanged;

        public delegate void ConversationsSortIdChangedDelegate(Dictionary<long, int> list);
        public static event ConversationsSortIdChangedDelegate ConversationsMajorIdChanged;
        public static event ConversationsSortIdChangedDelegate ConversationsMinorIdChanged;

        public static event EventHandler<LPTranslation> TranslationReceived;

        public delegate void ConvMemberRestrictionChangedDelegate(long peerId, long memberId, bool deny, int duration);
        public static event ConvMemberRestrictionChangedDelegate ConvMemberRestrictionChanged;

        public delegate void ConversationAccessRightsChangedDelegate(long peerId, long mask);
        public static event ConversationAccessRightsChangedDelegate ConversationAccessRightsChanged;

        public static event EventHandler<string> StatusChanged;

        #endregion

        public static async Task<bool> InitLongPoll(LongPollServerInfo lp = null) {
            Logger.Log.Info($"LongPoll > Init. Is fix enabled: {AppParameters.LongPollFix}");
            DEBUGLongPollResponseReceived?.Invoke($"Init longpoll...");

            if (lp != null) {
                await SetUpLPAsync(lp);
                IsInitialized = true;
                return true;
            }

            StatusChanged?.Invoke(null, Locale.Get("status_connecting"));
            bool result = false;
            object res = await Messages.GetLongPollServer();
            if (res is LongPollServerInfo) {
                var a = res as LongPollServerInfo;
                await SetUpLPAsync(a);
                result = true;
            } else if (res is VKErrorResponse err) {
                Logger.Log.Error($"LongPoll > Init API error {err.error.error_code}: {err.error.error_msg}");
                result = false;
            } else if (res is Exception ex) {
                Logger.Log.Error($"LongPoll > Init Exception 0x{ex.HResult.ToString("x8")}.");
                result = false;
            }
            IsInitialized = result;
            return result;
        }

        private static async Task GetLongPollHistoryAsync() {
            Log.Verbose($"LongPoll > GetLongPollHistory: TS: {TimeStamp}; PTS: {PTS}...");
            StatusChanged?.Invoke(null, Locale.Get("status_updating"));
            object res = await Messages.GetLongPollHistory(TimeStamp, PTS, 5000, 1000);
            if (res is LongPollHistoryResponse lph) {
                AppSession.AddUsersToCache(lph.Profiles);
                AppSession.AddGroupsToCache(lph.Groups);
                AppSession.AddContactsToCache(lph.Contacts);
                await ParseLPUpdatesAsync(lph.History, true);
                InvokeMessages(lph.Messages.Items, false, 20);
                await SetUpLPAsync(lph.Credentials);
            } else {
                await InitLongPoll();
            }
        }

        static bool eventsRegistered = false;
        private static async Task SetUpLPAsync(LongPollServerInfo lpinfo) {
            Server = AppParameters.LongPollFix ? lpinfo.Server.Replace("im.vk.ru/nim", "api.vk.me/ruim") : lpinfo.Server;
            Key = lpinfo.Key;
            TimeStamp = lpinfo.TS;
            PTS = lpinfo.PTS;

            DEBUGLongPollResponseReceived?.Invoke($"Server: {Server}; Key: {Key}; Timestamp: {TimeStamp}; PTS: {PTS}.");

            IsLPCycleEnabled = true;
            d = SynchronizationContext.Current;
            await Task.Factory.StartNew(async () => await RunAsync());
            StatusChanged?.Invoke(null, String.Empty);

            if (!eventsRegistered) {
                eventsRegistered = true;
                CoreApplication.EnteredBackground += (b, c) => {
                    Logger.Log.Info($"LongPoll > Stop LP because entering background.");
                    Stop();
                };
                CoreApplication.LeavingBackground += async (b, c) => {
                    Logger.Log.Info($"LongPoll > Get LP history after leave background.");
                    if (!IsLPCycleEnabled) await GetLongPollHistoryAsync();
                };
                NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
            }
        }

        private static void NetworkInformation_NetworkStatusChanged(object sender) {
            Logger.Log.Warn($"LongPoll > is internet available: {IsInternetAvailable}; initialized: {IsInitialized}");
            new System.Action(async () => {
                if (IsInternetAvailable) {
                    if (IsInitialized) return;
                    await GetLongPollHistoryAsync();
                    StatusChanged?.Invoke(null, null);
                } else {
                    Stop();
                    StatusChanged?.Invoke(null, Locale.Get("status_no_internet"));
                }
            })();
        }

        public static void Stop() {
            IsLPCycleEnabled = false;
            cts.Dispose();
            cts = new CancellationTokenSource();
        }

        static CancellationTokenSource cts = new CancellationTokenSource();
        static SynchronizationContext d = null;

        private static async Task RunAsync() {
            var ct = cts.Token;
            string raw = "N/A";
            while (IsLPCycleEnabled) {
                DEBUGLongPollResponseReceived?.Invoke($"Waiting ({TimeStamp})...");
                Log.Verbose($"LongPoll > Waiting ({TimeStamp})...");
                try {
                    if (d == null) d = SynchronizationContext.Current;
                    Tuple<object, string> resp = await LongPollServer.GetState(new Uri($"https://{Server}?act=a_check&key={Key}&ts={TimeStamp}&wait={WaitTime}&mode={Mode}&version={Version}"), ct).ConfigureAwait(false);
                    object r = resp.Item1;
                    raw = resp.Item2;
                    if (r is LongPollResponse res) {
                        if (!AppParameters.LongPollDebugInfoInStatus) StatusChanged?.Invoke(null, String.Empty);
                        DEBUGLongPollResponseReceived?.Invoke($"\n{res.Raw}");
                        d.Post(async o => await ParseLPUpdatesAsync(res.Updates), null);
                        TimeStamp = res.TS;
                    } else if (r is LongPollFail rerr) {
                        DEBUGLongPollResponseReceived?.Invoke($"LP FAIL: {rerr.FailCode}.");
                        switch (rerr.FailCode) {
                            case 1: TimeStamp = rerr.TS; IsLPCycleEnabled = true; break;
                            case 2:
                                StatusChanged?.Invoke(null, String.Format(Locale.GetForFormat("status_waiting_reconnect"), 2));
                                await Task.Delay(2000).ConfigureAwait(false);
                                await RestartLongPollAsync();
                                break;
                            case 3:
                                StatusChanged?.Invoke(null, String.Format(Locale.GetForFormat("status_waiting_reconnect"), 2));
                                await Task.Delay(2000).ConfigureAwait(false);
                                await RestartLongPollAsync();
                                break;
                        }
                    }
                } catch (Exception ex) {
                    StatusChanged?.Invoke(null, IsInternetAvailable ? String.Format(Locale.GetForFormat("status_waiting_reconnect"), 5) : Locale.Get("status_no_internet"));
#if DEBUG
                    File.AppendAllText(Path.Combine(ApplicationData.Current.LocalFolder.Path, "lp_errors.log"), $"Response:\n{raw}\n\nError code 0x{ex.HResult.ToString("x8")}. {ex.Message}\n\n\n");
                    d?.Post(async o => await new MessageDialog($"Error code 0x{ex.HResult.ToString("x8")}. {ex.Message}\n\n{raw}\n\nCheck lp_errors.log file.", "LongPoll Error!").ShowAsync(), null);
#endif
                    DEBUGLongPollResponseReceived?.Invoke($"EX FAIL: {ex.HResult} (0x{ex.HResult.ToString("x8")}).");
                    await Task.Delay(5000).ConfigureAwait(false);
                    await RestartLongPollAsync();
                    break;
                }
            }
        }

        private static async Task RestartLongPollAsync() {
            IsLPCycleEnabled = false;
            bool ka = await InitLongPoll();
            if (!ka) {
                Failed?.Invoke();
            }
        }

        public static async Task TestLPResponseAsync(string lpresponse) {
            List<object[]> lpevents = JsonConvert.DeserializeObject<List<object[]>>(lpresponse);
            await ParseLPUpdatesAsync(lpevents);
        }

        private static async Task ParseLPUpdatesAsync(List<object[]> updates, bool dontInvokeMessages = false) {
            long lpevent = 0;
            List<KeyValuePair<long, int>> NewMessages = new List<KeyValuePair<long, int>>();
            List<KeyValuePair<long, int>> EditedMessages = new List<KeyValuePair<long, int>>();
            Dictionary<long, int> ChangedMajorIDs = new Dictionary<long, int>();
            Dictionary<long, int> ChangedMinorIDs = new Dictionary<long, int>();

            // reactions peer_id, cmid
            var reactedIds = updates.Where(u => (long)u[0] == 601).Select(u => new Tuple<long, long>((long)u[2], (long)u[3]));

            try {
                foreach (var u in updates) {
                    lpevent = (long)u[0];
                    switch (lpevent) {
                        case 10002:
                            if (u.Length > 3) { // Иногда LP может возвращать обрезанное событие.
                                FlagsEvent?.Invoke(LPMessageFlagState.Set, Convert.ToInt32(u[1]), Convert.ToInt64(u[3]), Convert.ToInt32(u[2]));
                                DEBUGLongPollResponseReceived?.Invoke($"EVENT {lpevent} (msgflag set): msgid={u[1]}, peer={u[3]}, flag={(long)u[2]}");
                            }
                            break;
                        case 10003:
                            FlagsEvent?.Invoke(LPMessageFlagState.Reset, Convert.ToInt32(u[1]), Convert.ToInt64(u[3]), Convert.ToInt32(u[2]));
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT {lpevent} (msgflag reset): msgid={u[1]}, peer={(long)u[3]}, flag={u[2]}");
                            break;
                        case 10004:
                            if (dontInvokeMessages) return;
                            var msgid = (long)u[4];
                            if (ChangedMinorIDs.ContainsKey(msgid)) {
                                ChangedMinorIDs[msgid] = Convert.ToInt32(u[3]);
                            } else {
                                ChangedMinorIDs.Add(msgid, Convert.ToInt32(u[3]));
                            }
                            // Признак того, что это "приветственное" сообщение группы.
                            // if (Functions.CheckFlag(Convert.ToInt32(u[2]), 65536) && Functions.CheckFlag(Convert.ToInt32(u[2]), 65536)) return;
                            bool isPartial = false;
                            Exception ex = null;
                            Message rmsg = Message.BuildFromLP(u, AppParameters.UserID, CheckIsCached, out isPartial, out ex);
                            if (ex == null && rmsg != null) {
                                // LastMessageId = rmsg.Id;
                                MessageReceived?.Invoke(rmsg);
                                CheckMentionAndSelfDestructMessage(u[7], Convert.ToInt32(u[1]));
                                if (rmsg.Keyboard != null)
                                    DefaultKeyboardReceived?.Invoke(rmsg.Keyboard, rmsg.PeerId);

                                if (isPartial) {
                                    EditedMessages.Add(new KeyValuePair<long, int>(msgid, Convert.ToInt32(u[1])));
                                    msgrFromAPI++;
                                } else {
                                    msgrFromLP++;
                                }
                            } else {
                                msgrFromAPI++;
                                msgrErrors++;
                                DEBUGLongPollResponseReceived?.Invoke($"LP message parsing failed! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                                NewMessages.Add(new KeyValuePair<long, int>(msgid, Convert.ToInt32(u[1])));
                                if (u.Length > 4) { // Иногда LP может возвращать обрезанное событие.
                                    CheckKeyboardAvailability(u[7], (long)u[4]);
                                    CheckMentionAndSelfDestructMessage(u[7], Convert.ToInt32(u[1]));
                                }
                            }
                            if (AppParameters.LongPollDebugInfoInStatus)
                                StatusChanged?.Invoke(null, $"LP: {msgrFromLP}; API: {msgrFromAPI}; Err: {msgrErrors}");
                            break;
                        case 10005:
                        case 10018:
                            if (dontInvokeMessages) break;
                            if (reactedIds.Count() > 0) {
                                bool needIgnore = reactedIds.Any(r => r.Item1 == (long)u[3] && r.Item2 == (long)u[1]);
                                if (needIgnore) {
                                    Logger.Log.Info($"LongPoll edit msg event: there is a 601 event for message {(long)u[3]}_{(long)u[1]}, ignoring.");
                                    break;
                                }
                            }
                            EditedMessages.Add(new KeyValuePair<long, int>((long)u[3], Convert.ToInt32(u[1])));
                            break;
                        case 10006:
                        case 10007: MessageMarkedAsRead?.Invoke(Convert.ToInt64((long)u[1]), Convert.ToInt32((long)u[2]), u.Length > 3 ? Convert.ToInt32((long)u[3]) : -1, lpevent == 10007); break;
                        case 10: Event10Received?.Invoke(Convert.ToInt64(u[1]), Convert.ToInt32(u[2])); break;
                        case 12: Event12Received?.Invoke(Convert.ToInt64(u[1]), Convert.ToInt32(u[2])); break;
                        case 10013: ConversationDeleted?.Invoke(Convert.ToInt64(u[1]), Convert.ToInt32(u[2])); break;
                        case 20: ChangedMajorIDs.Add(Convert.ToInt64(u[1]), Convert.ToInt32(u[2])); break;
                        case 21: ChangedMinorIDs.Add(Convert.ToInt64(u[1]), Convert.ToInt32(u[2])); break;
                        case 50:
                            LPTranslation tr = JsonConvert.DeserializeObject<LPTranslation>(u[1].ToString());
                            TranslationReceived?.Invoke(null, tr);
                            break;
                        case 52:
                            long subtype = (long)u[1];
                            switch (subtype) {
                                case 4:
                                    ConversationAccessRightsChanged?.Invoke((long)u[2], (long)u[3]);
                                    break;
                            }
                            break;
                        case 63:
                        case 64:
                        case 65:
                        case 66:
                        case 67:
                            List<LongPollActivityInfo> ct = new List<LongPollActivityInfo>();
                            foreach (var b in (JArray)u[2]) {
                                int memberId = Int32.Parse(b.ToString());
                                LongPollActivityStatus status = LongPollActivityStatus.Typing;
                                switch (lpevent) {
                                    case 64: status = LongPollActivityStatus.RecordingVoiceMessage; break;
                                    case 65: status = LongPollActivityStatus.SendingPhoto; break;
                                    case 66: status = LongPollActivityStatus.SendingVideo; break;
                                    case 67: status = LongPollActivityStatus.SendingFile; break;
                                }
                                ct.Add(new LongPollActivityInfo(memberId, status));
                            }
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT {lpevent}: Peer={(long)u[1]}, 4th parameter={(long)u[3]}");
                            UserActivityStateChanged?.Invoke(new Tuple<long, List<LongPollActivityInfo>>(Convert.ToInt64(u[1]), ct));
                            break;
                        case 80:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 80: (counter update). {u[1]}, {u[2]}, {u[3]}, {u[7]}, {u[8]}, {u[9]}");
                            CounterUpdated?.Invoke(Convert.ToInt32(u[1]), Convert.ToInt32(u[2]), (long)u[3] != 0,
                                Convert.ToInt32(u[7]), Convert.ToInt32(u[8]), Convert.ToInt32(u[9]));
                            break;
                        case 91:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 91: (chat member restriction changed).");
                            int mrtype = Convert.ToInt32(u[1]);
                            bool isDeny = mrtype == 1 || mrtype == 2;
                            int duration = mrtype == 1 ? Convert.ToInt32(u[4]) : 0;
                            ConvMemberRestrictionChanged?.Invoke((long)u[2], (long)u[3], isDeny, duration);
                            break;
                        case 119:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 119 (bot callback).");
                            LPBotCallback cb = JsonConvert.DeserializeObject<LPBotCallback>(u[1].ToString());
                            if (botCallbackEventIds.Contains(cb.EventId)) {
                                botCallbackEventIds.Remove(cb.EventId);
                                BotCallbackReceived?.Invoke(cb);
                            }
                            break;
                        case 501:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 501 (folder created). Id={u[1]}, Name={u[2]}");
                            FolderCreated?.Invoke(Convert.ToInt32(u[1]), (string)u[2], Convert.ToInt32(u[3]));
                            break;
                        case 502:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 502 (folder deleted). Id={u[1]}");
                            FolderDeleted?.Invoke(Convert.ToInt32(u[1]));
                            break;
                        case 503:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 503 (folder renamed). Id={u[1]}, Name={u[2]}");
                            FolderRenamed?.Invoke(Convert.ToInt32(u[1]), (string)u[2]);
                            break;
                        case 504:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 504 (convs added in folder). Folder id={u[1]}");
                            List<long> convIds = new List<long>();
                            for (byte i = 2; i < u.Length; i++) {
                                convIds.Add(Convert.ToInt64(u[i]));
                            }
                            FolderConversationsAdded?.Invoke(Convert.ToInt32(u[1]), convIds);
                            break;
                        case 505:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 505 (convs deleted from folder). Folder id={u[1]}");
                            List<long> convIds2 = new List<long>();
                            for (byte i = 2; i < u.Length; i++) {
                                convIds2.Add(Convert.ToInt64(u[i]));
                            }
                            FolderConversationsRemoved?.Invoke(Convert.ToInt32(u[1]), convIds2);
                            break;
                        case 506:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 506 (folders reordered).");
                            List<int> folderIds = new List<int>();
                            for (byte i = 1; i < u.Length; i++) {
                                folderIds.Add(Convert.ToInt32(u[i]));
                            }
                            FoldersReordered?.Invoke(folderIds);
                            break;
                        case 507:
                            DEBUGLongPollResponseReceived?.Invoke($"EVENT 507 (unread counters changed in folders).");
                            for (byte i = 1; i < u.Length; i++) {
                                JArray array = (JArray)u[i];
                                FoldersUnreadCountersChanged?.Invoke(Convert.ToInt32(array[0]), Convert.ToInt32(array[1]), Convert.ToInt32(array[2]));
                            }
                            break;
                        case 601:
                            ParseReactionsAndInvoke(u.Select(num => Convert.ToInt64(num)).ToArray());
                            break;
                        case 602:
                            ParseUnreadReactionsAndInvoke(u.Select(num => Convert.ToInt32(num)).ToArray());
                            break;
                        default: DEBUGLongPollResponseReceived?.Invoke($"EVENT {lpevent} (unknown)"); break;
                    }
                }
                if (NewMessages.Count > 0) {
                    await GetMessagesAsync(NewMessages);
                    DEBUGLongPollResponseReceived?.Invoke($"EVENT 4: {NewMessages.Count} new message(s).");
                }
                if (EditedMessages.Count > 0) {
                    await GetMessagesAsync(EditedMessages, true);
                    DEBUGLongPollResponseReceived?.Invoke($"EVENT 5: {EditedMessages.Count} edited message(s).");
                }
                if (ChangedMajorIDs.Count > 0) {
                    ConversationsMajorIdChanged?.Invoke(ChangedMajorIDs);
                    DEBUGLongPollResponseReceived?.Invoke($"EVENT 20: changed major ids for {String.Join(",", ChangedMajorIDs.Keys)}.");
                }
                if (ChangedMinorIDs.Count > 0) {
                    ConversationsMinorIdChanged?.Invoke(ChangedMinorIDs);
                    DEBUGLongPollResponseReceived?.Invoke($"EVENT 21: changed minor ids for {String.Join(",", ChangedMajorIDs.Keys)}.");
                }
            } catch (Exception ex) {
                // UI.Tips.Show("Longpoll parse error", $"Event: {lpevent};\nHResult: 0x{ex.HResult.ToString("x8")}.");
                Log.Error($"Longpoll > ParseLPUpdates: HResult: 0x{ex.HResult.ToString("x8")}, event: {lpevent}");
            }
        }

        private static void ParseReactionsAndInvoke(long[] u) {
            ReactionEventType type = (ReactionEventType)u[1];
            long myReaction = 0;
            long peerId = u[2];
            long cmId = u[3];

            int pos = 4;
            if (type == ReactionEventType.IAdded) {
                myReaction = u[4];
                pos = 5;
            }

            bool changedByMe = type == ReactionEventType.IAdded || type == ReactionEventType.IRemoved;
            long i3 = u[pos];
            long i4 = pos + 1;

            List<Reaction> reactions = new List<Reaction>();

            for (long i = 0; i < i3; i++) {
                Tuple<long, Reaction> b = ParseReactions(u, i4);
                i4 = b.Item1;
                reactions.Add(b.Item2);
            }
            DEBUGLongPollResponseReceived?.Invoke($"EVENT 601: Type: {type}, my reaction id: {myReaction}, message: {peerId}_{cmId}, reactions: [{String.Join("], [", reactions)}]");
            ReactionsChanged?.Invoke(peerId, Convert.ToInt32(cmId), type, Convert.ToInt32(myReaction), reactions);
        }

        private static Tuple<long, Reaction> ParseReactions(long[] u, long start) {
            long i2 = u[start];
            long i3 = start + 1;
            long reactionId = u[start + 1];
            long count = u[start + 2];
            long end = u[start + 3];

            List<long> members = new List<long>();
            for (int j = 0; j < end; j++) {
                members.Add(u[start + 4 + j]);
            }
            return new Tuple<long, Reaction>(i3 + i2, new Reaction(Convert.ToInt32(reactionId)) { Count = Convert.ToInt32(count), Members = members });
        }

        private static void ParseUnreadReactionsAndInvoke(int[] u) {
            int peerId = u[1];
            int cmidsCount = u[2];
            List<int> cmIds = new List<int>();
            if (cmidsCount > 0) cmIds = u.Skip(3).ToList();
            DEBUGLongPollResponseReceived?.Invoke($"EVENT 602: peer={peerId}, count={cmidsCount}, cmids=[{String.Join(", ", cmIds)}");
            UnreadReactionsChanged?.Invoke(peerId, cmIds);
        }

        private static bool CheckIsCached(long id) {
            return AppSession.GetCachedUser(id) != null || AppSession.GetCachedGroup(id) != null;
        }

        private static async Task GetMessagesAsync(List<KeyValuePair<long, int>> peerMessagePair, bool edited = false) {
            object resp = null;

            if (peerMessagePair.Count == 1) {
                var pair = peerMessagePair[0];
                resp = await Messages.GetByConversationMessageId(pair.Key, new List<int> { pair.Value });
            } else {
                resp = await Messages.GetById(peerMessagePair);
            }

            if (resp is MessagesHistoryResponse r) {
                AppSession.AddUsersToCache(r.Profiles);
                AppSession.AddGroupsToCache(r.Groups);
                AppSession.AddContactsToCache(r.Contacts);
                InvokeMessages(r.Items, edited, 8);
            }
        }

        private static void InvokeMessages(List<Message> items, bool edited = false, int delay = 0) {
            foreach (Message msg in items) {
                if (edited) {
                    MessageEdited?.Invoke(msg);
                } else {
                    // LastMessageId = msg.Id;
                    if (!msg.IsHidden) MessageReceived?.Invoke(msg);
                }
                if (PendingMentions.ContainsKey(msg.Id)) {
                    var a = PendingMentions[msg.Id];
                    Logger.Log.Info($"Message {msg.Id} contains a mark for conv: {a}");
                    ConversationMentionReceived?.Invoke(msg.PeerId, msg.Id, a);
                    PendingMentions.Remove(msg.Id);
                }
                if (delay > 0) Task.Delay(delay).Wait();
            }
        }

        private static void CheckKeyboardAvailability(object v, long peerId) {
            BotKeyboard kbd = null;
            try {
                JToken t = ((JToken)v)["keyboard"];
                if (t != null) {
                    long from = ((JToken)v)["from"].Value<long>();
                    kbd = JsonConvert.DeserializeObject<BotKeyboard>(t.ToString(Formatting.None));
                    kbd.AuthorId = from;
                }
            } catch { }
            DefaultKeyboardReceived?.Invoke(kbd, (int)peerId);
        }

        static Dictionary<int, bool> PendingMentions = new Dictionary<int, bool>();

        private static void CheckMentionAndSelfDestructMessage(object v, int messageId) {
            try {
                JToken t = ((JToken)v)["marked_users"];
                if (t != null) {
                    foreach (JArray o in (JArray)t) {
                        int flag = Int32.Parse(o[0].ToString());
                        bool isBomb = flag == 2;

                        if (o[1].ToString() == "all") {
                            PendingMentions.Add(messageId, isBomb);
                        } else {
                            JArray u1 = (JArray)o[1];
                            if (Int64.Parse(u1.First.ToString()) == AppParameters.UserID) PendingMentions.Add(messageId, isBomb);
                        }
                    }
                }
            } catch {
                Logger.Log.Error($"marked_users parsing error!");
            }
        }
    }
}
using ELOR.VKAPILib;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.UI.Core;

namespace Elorucov.Laney.Core
{
    public enum LongPollActivityStatus { Typing, RecordingVoiceMessage, SendingPhoto, SendingVideo, SendingFile }
    public enum LPMessageFlagState
    {
        Changed = 1, Set = 2, Reset = 3
    }
    public enum LPConversationMarkType
    {
        Mention, SelfDestructMessage, Unread
    }

    public class LongPollActivityInfo
    {
        internal LongPollActivityInfo(int id, LongPollActivityStatus status)
        {
            MemberId = id;
            Status = status;
        }

        public int MemberId { get; private set; }

        public LongPollActivityStatus Status { get; private set; }

        public override string ToString()
        {
            return $"{MemberId}={Status}";
        }
    }

    public class NotificationSettingsChangedInfo
    {
        [JsonProperty("peer_id")]
        public int PeerId { get; private set; }

        [JsonProperty("disabled_until")]
        public int? DisabledUntil { get; private set; }
    }

    public class LongPoll
    {

        #region Core

        public event EventHandler<int> NeedNewServerInfo;
        public event EventHandler<bool> InternetAvailabilityChanged;
        public event EventHandler<Tuple<Exception, int>> CaughtException;

        Log log = null;

        const int WaitTime = 25;
        const int Mode = 234;

        private LongPollServerInfo Info;
        private VKAPI API;
        private CancellationTokenSource cts;
        private bool IsRunning = false;
        private int RetryAfterSeconds = 0;

        private bool IsInternetAvailable { get { return NetworkInformation.GetInternetConnectionProfile() != null; } }
        string Name { get { return GroupId > 0 ? $"{API.UserId}_{GroupId}" : API.UserId.ToString(); } }

        private int MaxMessageId = 0;
        private int GroupId = 0;
        private int SessionId { get { return GroupId == 0 ? API.UserId : GroupId; } }
        public CoreDispatcher Dispatcher { get; set; }

        private HttpClient httpClient = new HttpClient();
        private string CurrentServerUrl { get { return $"https://{Info.Server}?act=a_check&key={Info.Key}&ts={Info.TS}&wait={WaitTime}&mode={Mode}&version=10"; } }

        public LongPoll(VKAPI api, int groupId)
        {
            GroupId = groupId;
            API = api;

            //log = new Log($"lp{Name}");
            //log?.ReinitRequested += (a, b) => log = new Log($"lp{Name}");

            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
            CoreApplication.EnteredBackground += (b, c) =>
            {
                Stop();
            };
            CoreApplication.LeavingBackground += (b, c) =>
            {
                if (!IsRunning) GetLongPollHistory();
            };
        }

        private void NetworkInformation_NetworkStatusChanged(object sender)
        {
            log?.Warn($"Network status changed to {IsInternetAvailable}.");
            if (Info == null) return;
            InternetAvailabilityChanged?.Invoke(this, IsInternetAvailable);
            IsRunning = IsInternetAvailable;
            if (!IsInternetAvailable)
            {
                httpClient.Dispose();
            }
            else
            {
                Run();
            }
        }

        public void SetNewServer(LongPollServerInfo info)
        {
            Stop();
            SetInfo(info);
        }

        private void SetInfo(LongPollServerInfo info)
        {
            Info = info;
            Run();
        }

        public void Stop()
        {
            log?.Info($"Stopping...");
            if (!IsRunning) return;
            IsRunning = false;
            currentlyWaitingLPUrl = null;
            log?.Stop();
            cts.Cancel();
            NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
            httpClient.Dispose();
        }

        string currentlyWaitingLPUrl = null;

        private async void Run()
        {
            httpClient = new HttpClient();
            cts = new CancellationTokenSource();
            if (currentlyWaitingLPUrl == CurrentServerUrl)
            {
                Debug.WriteLine("Don't know what is calling Run() method again...");
                return;
            }
            IsRunning = true;
            log?.Info("Start!");
            while (IsRunning)
            {
                try
                {
                    currentlyWaitingLPUrl = CurrentServerUrl;
                    object r = await GetStateAsync(new Uri(currentlyWaitingLPUrl), cts.Token).ConfigureAwait(false);
                    if (r is LongPollResponse res)
                    {
                        currentlyWaitingLPUrl = null;
                        RetryAfterSeconds = 0;
                        Info.TS = res.TS;
                        await Dispatcher?.RunAsync(CoreDispatcherPriority.Normal, () => ParseUpdates(res.Updates));
                    }
                    else if (r is LongPollFail rf)
                    {
                        RetryAfterSeconds = 0;
                        log?.Error($"LP server returns an error.", new ValueSet { { "code", rf.FailCode } });
                        switch (rf.FailCode)
                        {
                            case 1: Info.TS = rf.TS; break;
                            case 2:
                            case 3: Stop(); NeedNewServerInfo?.Invoke(this, rf.FailCode); break;
                        }
                    }
                    else if (r is Exception ex)
                    {
                        log?.Critical($"LP crashed!", ex);
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    if (!IsInternetAvailable || ex is TaskCanceledException) return;
                    RetryAfterSeconds += 15;
                    log?.Error($"LP crashed! HResult: 0x{ex.HResult.ToString("x8")}. Retry after {RetryAfterSeconds} sec.");
                    CaughtException?.Invoke(this, new Tuple<Exception, int>(ex, RetryAfterSeconds));
                    await Task.Delay(RetryAfterSeconds * 1000).ConfigureAwait(false);
                }
            }
        }

        private async Task<object> GetStateAsync(Uri longPollUri, CancellationToken ct)
        {
            try
            {
                var res = await httpClient.GetAsync(longPollUri);

                if (ct.IsCancellationRequested)
                {
                    log?.Info("Cancellation requested.");
                    throw new TaskCanceledException();
                }
                ;

                res.EnsureSuccessStatusCode();
                string restr = await res.Content.ReadAsStringAsync();
                JObject jr = JObject.Parse(restr);
                if (jr["ts"] != null)
                {
                    LongPollResponse resp = JsonConvert.DeserializeObject<LongPollResponse>(restr);
                    resp.Raw = restr;
                    res.Dispose();
                    return resp;
                }
                else if (jr["failed"] != null)
                {
                    res.Dispose();
                    return JsonConvert.DeserializeObject<LongPollFail>(restr);
                }
                else
                {
                    res.Dispose();
                    log?.Error("A non-standart response was received!");
                    throw new ArgumentException($"A non-standart response was received!\n{restr}");
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private async void GetLongPollHistory()
        {
            try
            {
                var lph = await API.Messages.GetLongPollHistoryAsync(GroupId, int.Parse(Info.TS), int.Parse(Info.PTS), 0, true, 5000, 1000, MaxMessageId, APIHelper.Fields);
                CacheManager.Add(lph.Profiles);
                CacheManager.Add(lph.Groups);

                foreach (Message m in lph.Messages.Items)
                {
                    NewMessageReceived?.Invoke(this, m);
                }

                SetInfo(lph.Credentials);
            }
            catch (Exception ex)
            {
                log?.Error($"Error getting longpoll history!", ex);
                await Task.Delay(5000).ConfigureAwait(false);
                GetLongPollHistory();
            }
        }

        #endregion

        #region Responses

        public delegate void FlagsEventDelegate(LPMessageFlagState state, int msgid, int peerId, int flags);
        public event FlagsEventDelegate FlagsEvent; // 1 - 3

        public event EventHandler<Message> NewMessageReceived; // 4
        public event EventHandler<Tuple<int, BotKeyboard>> KeyboardChanged; // 4
        public event EventHandler<Message> MessageEdit; // 5
        public event EventHandler<Tuple<int, int, int>> IncomingMessagesRead; // 6
        public event EventHandler<Tuple<int, int, int>> OutgoingMessagesRead; // 7
        public event EventHandler<Tuple<int, int, bool>> UserOnline; // 8
        public event EventHandler<Tuple<int, bool>> UserOffline; // 9
        public event EventHandler<Tuple<int, int>> ConversationRemoved; // 13
        public event EventHandler<Dictionary<int, int>> MajorIdChanged; // 20
        public event EventHandler<Tuple<int, int, int>> ChatInfoChanged; // 52 <peer_id, type, extra>
        public event EventHandler<Tuple<int, List<LongPollActivityInfo>>> ActivityStatusChanged; // 63 & 64
        public event EventHandler<NotificationSettingsChangedInfo> NotificationSettingsChanged; // 114
        public event EventHandler<LPBotCallback> BotCallbackReceived; // 119

        // Если получено сообщение с упоминанием или самоисчезающее, или если юзер отметил беседу как непрочитанной
        // Тип отметки, id беседы, id сообщения (0 при LPConversationMarkType = Unread)
        public event EventHandler<Tuple<LPConversationMarkType, int, int>> ConversationMarked;
        public event EventHandler<Tuple<LPConversationMarkType, int>> ConversationUnmarked;

        private async void ParseUpdates(List<object[]> updates)
        {
            List<int> newMessageIds = new List<int>();
            List<int> editMessageIds = new List<int>();
            Dictionary<int, int> ChangedMajorIDs = new Dictionary<int, int>();

            if (updates == null) return;

            foreach (var u in updates)
            {
                long eventindex = (long)u[0];
                switch (eventindex)
                {
                    case 1:
                        FlagsEvent?.Invoke(LPMessageFlagState.Changed, Convert.ToInt32(u[1]), Convert.ToInt32(u[3]), Convert.ToInt32(u[2]));
                        break;
                    case 2: // Установка флага
                        FlagsEvent?.Invoke(LPMessageFlagState.Set, Convert.ToInt32(u[1]), Convert.ToInt32(u[3]), Convert.ToInt32(u[2]));
                        break;
                    case 3: // Удаление флага
                        FlagsEvent?.Invoke(LPMessageFlagState.Reset, Convert.ToInt32(u[1]), Convert.ToInt32(u[3]), Convert.ToInt32(u[2]));
                        break;
                    case 4: // Новое сообщение
                        newMessageIds.Add(Convert.ToInt32(u[1]));
                        MaxMessageId = Convert.ToInt32(u[1]);
                        CheckKeyboard(u[6], Convert.ToInt32(u[3]));
                        CheckMentionAndSelfDestructMessage(u[6], Convert.ToInt32(u[1]));
                        break;
                    case 5: // Сообщение отредактировано
                        editMessageIds.Add(Convert.ToInt32(u[1]));
                        break;
                    case 6: // Прочитано входящие сообщения: peer_id, msg_id, count
                        IncomingMessagesRead?.Invoke(this, new Tuple<int, int, int>(Convert.ToInt32(u[1]), Convert.ToInt32(u[2]), Convert.ToInt32(u[3])));
                        break;
                    case 7: // Прочитано исходящие сообщения: peer_id, msg_id, count
                        OutgoingMessagesRead?.Invoke(this, new Tuple<int, int, int>(Convert.ToInt32(u[1]), Convert.ToInt32(u[2]), Convert.ToInt32(u[3])));
                        break;
                    case 8: // Пользователь в сети: id польз., app_id, с мобильного
                        int p = Convert.ToInt32(u[2]); // platform
                        bool isMobile = p == 1 || p == 2 || p == 4 || p == 5;
                        UserOnline?.Invoke(this, new Tuple<int, int, bool>(-Convert.ToInt32(u[1]), Convert.ToInt32(u[4]), isMobile));
                        break;
                    case 9: // Пользователь вышел из сети: id польз., таймаут
                        UserOffline?.Invoke(this, new Tuple<int, bool>(-Convert.ToInt32(u[1]), Convert.ToInt32(u[2]) == 1));
                        break;
                    case 10:
                        int flag1 = Convert.ToInt32(u[2]);
                        if (flag1 == 17408)
                        { // Просмотрено исчезающее сообщение / сообщение с упоминанием
                            ConversationUnmarked?.Invoke(this, new Tuple<LPConversationMarkType, int>(LPConversationMarkType.Mention, Convert.ToInt32(u[1])));
                            ConversationUnmarked?.Invoke(this, new Tuple<LPConversationMarkType, int>(LPConversationMarkType.SelfDestructMessage, Convert.ToInt32(u[1])));
                        }
                        else if (flag1 == 1048576)
                        { // Беседа была отмечена как прочитанной после ручной отметки как непрочитанный
                            ConversationUnmarked?.Invoke(this, new Tuple<LPConversationMarkType, int>(LPConversationMarkType.Unread, Convert.ToInt32(u[1])));
                        }
                        else
                        {
                            log?.Info($"Unknown flag.", new ValueSet { { "index", eventindex }, { "flag", flag1 } });
                        }
                        break;
                    case 12:
                        int flag2 = Convert.ToInt32(u[2]);
                        if (flag2 == 1048576)
                        { // Беседа была отмечена как непрочитанной
                            ConversationMarked?.Invoke(this, new Tuple<LPConversationMarkType, int, int>(LPConversationMarkType.Unread, Convert.ToInt32(u[1]), 0));
                        }
                        else
                        {
                            log?.Info($"Unknown flag.", new ValueSet { { "index", eventindex }, { "flag", flag2 } });
                        }
                        break;
                    case 13: // беседа удалена (peer_id, last_message_id)
                        ChangedMajorIDs.Add(Convert.ToInt32(u[1]), Convert.ToInt32(u[2]));
                        break;
                    case 20: // изменено поле sort_id.major_id в беседе (например, её закрепили)
                        ChangedMajorIDs.Add(Convert.ToInt32(u[1]), Convert.ToInt32(u[2]));
                        break;
                    case 52: // изменены данные чата
                        ChatInfoChanged?.Invoke(this, new Tuple<int, int, int>(Convert.ToInt32(u[2]), Convert.ToInt32(u[1]), Convert.ToInt32(u[3])));
                        break;
                    case 63: // статус набора сообщения
                    case 64: // статус записи голосового
                    case 65: // статус отправки фото
                    case 66: // статус отправки видео
                    case 67: // статус отправки файла
                        List<LongPollActivityInfo> ct = new List<LongPollActivityInfo>();
                        foreach (var b in (JArray)u[2])
                        {
                            int memberId = Int32.Parse(b.ToString());
                            LongPollActivityStatus status = LongPollActivityStatus.Typing;
                            switch (eventindex)
                            {
                                case 64: status = LongPollActivityStatus.RecordingVoiceMessage; break;
                                case 65: status = LongPollActivityStatus.SendingPhoto; break;
                                case 66: status = LongPollActivityStatus.SendingVideo; break;
                                case 67: status = LongPollActivityStatus.SendingFile; break;
                            }

                            ct.Add(new LongPollActivityInfo(memberId, status));
                        }
                        ActivityStatusChanged?.Invoke(this, new Tuple<int, List<LongPollActivityInfo>>(Convert.ToInt32(u[1]), ct));
                        break;
                    case 114: // Изменение настроек уведомлений в беседе
                        NotificationSettingsChangedInfo info = JsonConvert.DeserializeObject<NotificationSettingsChangedInfo>(u[1].ToString());
                        NotificationSettingsChanged?.Invoke(this, info);
                        break;
                    case 119: // Получено событие бота после нажатия на cb-кнопку
                        LPBotCallback cb = JsonConvert.DeserializeObject<LPBotCallback>(u[1].ToString());
                        BotCallbackReceived?.Invoke(this, cb);
                        break;
                    default:
                        log?.Warn($"Unknown event received.", new ValueSet { { "index", eventindex } });
                        break;
                }
            }

            // Обработка 20-й событии
            if (ChangedMajorIDs.Count > 0)
            {
                MajorIdChanged?.Invoke(this, ChangedMajorIDs);
                log?.Info($"Major id for peer_ids {String.Join(",", ChangedMajorIDs.Keys)} changed to {String.Join(",", ChangedMajorIDs.Values)}");
            }

            // Обработка новых/отредактированных сообщений
            try
            {
                var ids = newMessageIds.Concat(editMessageIds).ToList();
                if (ids.Count == 0) return;
                var messages = await API.Messages.GetByIdAsync(GroupId, ids, 0, true, APIHelper.Fields);
                CacheManager.Add(messages.Profiles);
                CacheManager.Add(messages.Groups);
                foreach (Message m in messages.Items)
                {
                    if (newMessageIds.Contains(m.Id))
                    {
                        NewMessageReceived?.Invoke(this, m);
                        if (PendingMentions.ContainsKey(m.Id))
                        {
                            var a = PendingMentions[m.Id];
                            log?.Info($"Message {m.Id} contains a mark for conv: {a}");
                            ConversationMarked?.Invoke(this, new Tuple<LPConversationMarkType, int, int>(a, m.PeerId, m.Id));
                            PendingMentions.Remove(m.Id);
                        }
                    }
                    if (editMessageIds.Contains(m.Id)) MessageEdit?.Invoke(this, m);
                }
            }
            catch (Exception ex)
            {
                log?.Error($"Error getting message objects from api!", ex);
            }
        }

        Dictionary<int, LPConversationMarkType> PendingMentions = new Dictionary<int, LPConversationMarkType>();

        private void CheckKeyboard(object v, int peerId)
        {
            BotKeyboard kbd = null;
            try
            {
                JToken t = ((JToken)v)["keyboard"];
                if (t == null) return;
                int from = ((JToken)v)["from"].Value<int>();
                if (t != null)
                {
                    kbd = JsonConvert.DeserializeObject<BotKeyboard>(t.ToString(Formatting.None));
                    kbd.AuthorId = from;
                }
                if (kbd != null && !kbd.Inline) KeyboardChanged?.Invoke(this, new Tuple<int, BotKeyboard>(peerId, kbd));
            }
            catch (Exception ex)
            {
                log?.Error($"Error parsing keyboard!", ex);
            }
        }

        private void CheckMentionAndSelfDestructMessage(object v, int messageId)
        {
            try
            {
                JToken t = ((JToken)v)["marked_users"];
                if (t != null)
                {
                    foreach (JArray o in (JArray)t)
                    {
                        int flag = Int32.Parse(o[0].ToString());
                        LPConversationMarkType type = flag == 2 ? LPConversationMarkType.SelfDestructMessage : LPConversationMarkType.Mention;

                        if (o[1].ToString() == "all")
                        {
                            PendingMentions.Add(messageId, type);
                        }
                        else
                        {
                            JArray u1 = (JArray)o[1];
                            if (Int32.Parse(u1.First.ToString()) == SessionId) PendingMentions.Add(messageId, type);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Error($"marked_users parsing error!", ex);
            }
        }

        #endregion
    }
}
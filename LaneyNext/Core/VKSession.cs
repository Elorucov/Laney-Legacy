using ELOR.VKAPILib;
using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Helpers.Security;
using Elorucov.Laney.ViewModels;
using Elorucov.Laney.VKAPIExecute;
using Elorucov.Laney.VKAPIExecute.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Elorucov.Laney.Core
{
    public enum SessionType { VKUser, VKGroup }

    public class VKSession : BaseViewModel
    {
        private int _id;
        private string _displayName;
        private Uri _avatar;
        private string _accessToken;
        private int _groupId;

        [JsonProperty("id")]
        public int Id { get { return _id; } set { _id = value; OnPropertyChanged(); } }

        [JsonProperty("display_name")]
        public string DisplayName { get { return _displayName; } set { _displayName = value; OnPropertyChanged(); } }

        [JsonProperty("avatar")]
        public Uri Avatar { get { return _avatar; } set { _avatar = value; OnPropertyChanged(); } }

        [JsonProperty("at")]
        public string AccessToken { get { return _accessToken; } set { _accessToken = value; OnPropertyChanged(); } }

        [JsonProperty("group_id")]
        public int GroupId { get { return _groupId; } set { _groupId = value; OnPropertyChanged(); } }

        [JsonIgnore]
        public int SessionId { get { return GroupId == 0 ? Id : -GroupId; } }

        [JsonIgnore]
        public SessionType Type { get { return GroupId == 0 ? SessionType.VKUser : SessionType.VKGroup; } }

        [JsonIgnore]
        public string ShortDisplayName { get { return GetShortDisplayName(); } }

        [JsonIgnore]
        public VKAPI API { get; private set; }

        [JsonIgnore]
        public Execute Execute { get { return API.Execute as Execute; } }

        [JsonIgnore]
        public LongPoll LongPoll { get; private set; }

        [JsonIgnore]
        public SessionBaseViewModel SessionBase { get; set; }

        bool isAuthorized = false;
        List<ConversationViewModel> cachedConversations = new List<ConversationViewModel>();

        public VKSession(int id, string accessToken, int groupId = 0)
        {
            Id = id; AccessToken = accessToken; GroupId = groupId;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public void StartSession()
        {
            if (isAuthorized) return;
            isAuthorized = true;
            Log.General.Info("Hi!", new ValueSet { { "session_id", Id } });
            if (String.IsNullOrEmpty(_accessToken)) return;
            Log.General.Info(String.Empty, new ValueSet { { "id", Id }, { "group_id", GroupId } });
            API = new VKAPI(Id, _accessToken, Locale.Get("apilang"), typeof(Execute));
            LongPoll = new LongPoll(API, GroupId);
            UpdateInfo();
        }

        public void EndSesson()
        {
            if (!isAuthorized) return;
            Log.General.Info("Bye!", new ValueSet { { "session_id", Id } });
            LongPoll.Stop();
            LongPoll = null;
            isAuthorized = false;
        }

        public async void UpdateInfo()
        {
            if (!isAuthorized) return;
            try
            {
                Log.General.Info($"Updating session info", new ValueSet { { "id", Id }, { "group_id", GroupId } });
                StartSessionResponse ssr = await Execute.StartSessionAsync(Id, GroupId);
                StickersKeywords.InitDictionary(ssr.StickersKeywords);
                LongPoll.SetNewServer(ssr.LongPoll);
                LongPoll.NeedNewServerInfo += (a, b) =>
                {
                    GetNewLongPollServer();
                };

                bool needUpdateSessionFile = true;

                if (Type == SessionType.VKUser)
                {
                    User u = ssr.User;
                    needUpdateSessionFile = u.FullName != DisplayName || u.Photo != Avatar;

                    DisplayName = u.FullName;
                    Avatar = u.Photo;
                }
                else
                {
                    Group g = ssr.Group;
                    needUpdateSessionFile = g.Name != DisplayName || g.Photo != Avatar;

                    DisplayName = g.Name;
                    Avatar = g.Photo;
                }

                if (needUpdateSessionFile)
                {
                    bool res = await AddSessionAsync(this, true);
                    if (res)
                    {
                        Log.General.Info($"Session info updated", new ValueSet { { "id", Id }, { "group_id", GroupId } });
                    }
                    else
                    {
                        Log.General.Warn($"Session info not updated", new ValueSet { { "id", Id }, { "group_id", GroupId } });
                        await Task.Delay(2000);
                        UpdateInfo();
                    }
                }
                else
                {
                    Log.General.Warn($"Session info not updated, because info is not changed", new ValueSet { { "id", Id }, { "group_id", GroupId } });
                }
            }
            catch (Exception ex)
            {
                Log.General.Error($"Error while updating session info for id={Id} & group id={GroupId}", ex);
                await Task.Delay(2000);
                UpdateInfo();
            }
        }

        private async void GetNewLongPollServer()
        {
            try
            {
                LongPollServerInfo lp = await API.Messages.GetLongPollServerAsync(true, GroupId);
                LongPoll.SetNewServer(lp);
            }
            catch (Exception ex)
            {
                Log.General.Error($"Error while getting new lp server for id={Id} & group id={GroupId}", ex);
            }
        }

        private string GetShortDisplayName()
        {
            if (Type == SessionType.VKUser)
            {
                string[] s = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (s.Length == 0 || s.Length == 1) return DisplayName;
                return s[0];
            }
            return DisplayName;
        }

        public void AddConversationToCache(ConversationViewModel cvm)
        {
            if (cvm == null) return;
            if (GetCachedConversation(cvm.Id) == null)
            {
                // Remove oldest
                //if (cachedConversations.Count == 5) {
                //    ConversationViewModel old = cachedConversations.First();
                //    old.Dispose();
                //    cachedConversations.Remove(old);
                //    Log.General.Info("Oldest cached CVM is removed.", new ValueSet { { "peer_id", old.Id } });
                //}
                cachedConversations.Add(cvm);
            }
            else
            {
                Log.General.Info("CVM is already open and cached.", new ValueSet { { "peer_id", cvm.Id } });
            }
        }

        public ConversationViewModel GetCachedConversation(int peerId)
        {
            return cachedConversations.Where(c => c.Id == peerId).FirstOrDefault();
        }

        #region Static members

        private static readonly string File = "sessions";

        public static void BindSessionToCurrentView(VKSession session)
        {
            var appview = CoreApplication.GetCurrentView();
            BindSessionToView(session, appview);
        }

        public static void BindSessionToView(VKSession session, CoreApplicationView appview)
        {
            var props = appview.Properties;
            if (props.ContainsKey("session"))
            {
                props["session"] = session;
            }
            else
            {
                props.Add("session", session);
            }
            if (session.LongPoll != null) session.LongPoll.Dispatcher = appview.Dispatcher;
            SessionBound?.Invoke(null, appview);

            if (ViewManagement.CurrentViewType == ViewType.Session)
            {
                appview.CoreWindow.Closed += (a, b) =>
                {
                    session.EndSesson();
                    props.Remove("session");
                };
            }
        }

        public static VKSession Current { get { return GetCurrent(); } }

        public static VKSession CurrentUser { get { return GetSessionByUserId(Current.Id); } }

        private static VKSession GetCurrent()
        {
            var view = CoreApplication.GetCurrentView();
            if (view.Properties.ContainsKey("session")) return view.Properties["session"] as VKSession;
            return null;
        }

        public static bool Compare(VKSession first, VKSession second)
        {
            return first.Id == second.Id && first.AccessToken == second.AccessToken && first.GroupId == second.GroupId;
        }

        public static event EventHandler<CoreApplicationView> SessionBound;

        private static List<VKSession> Sessions;

        public static async Task<List<VKSession>> GetSessionsAsync()
        {
            return await Task.Run(async () =>
            {
                if (Sessions != null) return Sessions;
                List<VKSession> s = new List<VKSession>();
                try
                {
                    StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(File, CreationCollisionOption.OpenIfExists);
                    if (file != null)
                    {
                        IBuffer buff = await FileIO.ReadBufferAsync(file);
                        if (buff.Length == 0) return s;
                        byte[] data = buff.ToArray();

                        var a = AuthorizationHelper.Bytes.Reverse();
                        var b = ViewModels.Controls.MessageInputViewModel.Enhancements.Reverse();
                        var c = Views.Settings.Debug.Features.Reverse();
                        byte[] by = a.Concat(b).Concat(c).ToArray();
                        string str = data.Decrypt(Encoding.UTF8.GetString(by));

                        if (!String.IsNullOrEmpty(str)) s = JsonConvert.DeserializeObject<List<VKSession>>(str);
                    }
                    Sessions = s;
                    Log.General.Info("Sessions list loaded", new ValueSet { { "count", s.Count } });
                }
                catch (Exception ex)
                {
                    Log.General.Error($"Sessions list load failed!", ex);
                }
                return s;
            });
        }

        public static List<VKSession> GetSessionsForVKUser(int userId)
        {
            List<VKSession> all = Sessions;
            List<VKSession> result = new List<VKSession>();
            foreach (var s in all)
            {
                if (s.Id == userId)
                {
                    if (s.GroupId == 0)
                    {
                        result.Insert(0, s);
                    }
                    else
                    {
                        result.Add(s);
                    }
                }
            }
            return result;
        }

        private static VKSession GetSessionByUserId(int userid)
        {
            return Sessions.Where(s => s.Id == userid && s.Type == SessionType.VKUser).FirstOrDefault();
        }

        private static async Task<bool> SaveSessionsInFileAsync(List<VKSession> sessions)
        {
            return await Task.Run<bool>(async () =>
            {
                try
                {
                    string str = JsonConvert.SerializeObject(sessions);

                    var a = AuthorizationHelper.Bytes.Reverse();
                    var b = ViewModels.Controls.MessageInputViewModel.Enhancements.Reverse();
                    var c = Views.Settings.Debug.Features.Reverse();
                    byte[] by = a.Concat(b).Concat(c).ToArray();
                    byte[] enc = str.Encrypt(Encoding.UTF8.GetString(by));

                    StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(File, CreationCollisionOption.OpenIfExists);
                    if (file != null)
                    {
                        await FileIO.WriteBytesAsync(file, enc);
                        Sessions = sessions;
                        Log.General.Info("Sessions list saved");
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log.General.Error($"Error while saving sessions list!", ex);
                    return false;
                }
            });
        }

        public static async Task<bool> AddSessionsAsync(List<VKSession> savingSessions, bool savePosition = false)
        {
            return await Task.Run<bool>(async () =>
            {
                try
                {
                    List<VKSession> sessions = await GetSessionsAsync();
                    foreach (var session in savingSessions)
                    {
                        int idx = -1;
                        if (sessions.Count > 0)
                        {
                            List<VKSession> a = null;
                            if (session.Type == SessionType.VKUser) a = (from q in sessions where q.Id == session.Id && q.Type == session.Type select q).ToList();
                            if (session.Type == SessionType.VKGroup) a = (from q in sessions where q.GroupId == session.GroupId && q.Type == session.Type select q).ToList();
                            if (a.Count > 0)
                            {
                                idx = sessions.IndexOf(a.First());
                                sessions.Remove(a.First());
                            }
                        }
                        if (savePosition && idx >= 0)
                        {
                            sessions.Insert(idx, session);
                        }
                        else
                        {
                            sessions.Add(session);
                        }
                    }
                    return await SaveSessionsInFileAsync(sessions);
                }
                catch
                {
                    return false;
                }
            });
        }

        public static async Task<bool> AddSessionAsync(VKSession session, bool savePosition = false)
        {
            return await AddSessionsAsync(new List<VKSession> { session }, savePosition);
        }

        public static async Task<bool> RefreshGroupSessionsAsync(VKSession ownerSession, List<VKSession> groupSessions)
        {
            return await Task.Run<bool>(async () =>
            {
                try
                {
                    groupSessions.Insert(0, ownerSession);
                    return await SaveSessionsInFileAsync(groupSessions);
                }
                catch
                {
                    return false;
                }
            });
        }

        public static async Task<bool> RemoveSessionAsync(VKSession session)
        {
            return await Task.Run<bool>(async () =>
            {
                try
                {
                    List<VKSession> sessions = await GetSessionsAsync();
                    if (sessions.Count > 0)
                    {
                        List<VKSession> a = null;
                        if (session.Type == SessionType.VKUser) a = (from q in sessions where q.Id == session.Id && q.Type == session.Type select q).ToList();
                        if (session.Type == SessionType.VKGroup) a = (from q in sessions where q.GroupId == session.GroupId && q.Type == session.Type select q).ToList();
                        if (a.Count == 1)
                        {
                            bool r = sessions.Remove(a[0]);
                            if (r) return await SaveSessionsInFileAsync(sessions);
                        }
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        public static async void LogoutAsync()
        {
            Log.General.Info("Requesting logout");
            AudioPlayerViewModel.CloseMainInstance();
            AudioPlayerViewModel.CloseVoiceMessageInstance();

            ViewManagement.CloseAllAnotherWindows();

            HttpBaseProtocolFilter f = new HttpBaseProtocolFilter();
            HttpCookieCollection c = f.CookieManager.GetCookies(new Uri("https://vk.com"));
            foreach (HttpCookie hc in c)
            {
                f.CookieManager.DeleteCookie(hc);
            }

            ViewManagement.OpenLandingPage();

            Log.StopAll();
            if (Settings.KeepLogsAfterLogout)
            {
                foreach (var p in ApplicationData.Current.LocalSettings.Values)
                {
                    ApplicationData.Current.LocalSettings.Values[p.Key] = null;
                }
                var sessionFile = await ApplicationData.Current.LocalFolder.GetFileAsync(File);
                await sessionFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            else
            {
                await ApplicationData.Current.ClearAsync();
            }
        }

        #endregion
    }
}
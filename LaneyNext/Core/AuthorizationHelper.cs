using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Views.Modals;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;

namespace Elorucov.Laney.Core
{
    public class AuthorizationHelper
    {
        private static readonly string userUrl = $"https://oauth.vk.com/authorize?client_id={Constants.ApplicationClientId}&display=windows_mobile&redirect_uri=https://oauth.vk.com/blank.html&scope=friends,photos,audio,video,stories,messages,wall,offline,docs,groups,apps&response_type=token&revoke=1&v={ELOR.VKAPILib.VKAPI.Version}";
        private static readonly string endUrl = "https://oauth.vk.com/blank.html";
        internal static readonly byte[] Bytes = new byte[] { 0x73, 0x61, 0x65, 0x42 };

        public static async Task<VKSession> AuthVKUserLegacy()
        {
            WebAuthenticationResult result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(userUrl), new Uri(endUrl));
            if (result.ResponseStatus == WebAuthenticationStatus.Success)
            {
                if (result.ResponseData.Contains("access_token"))
                {
                    string[] a = result.ResponseData.Split('#');
                    string[] b = a[1].Split('&');
                    string id = b[2].Split('=')[1];
                    string at = b[0].Split('=')[1];

                    VKSession session = new VKSession(Int32.Parse(id), at);
                    User currentUser = await session.API.Users.GetAsync();
                    session.DisplayName = currentUser.FullName;
                    session.Avatar = currentUser.Photo;

                    await VKSession.AddSessionAsync(session);
                    return session;
                }
                else if (result.ResponseData.Contains("error"))
                {
                    string[] a = result.ResponseData.Split('#');
                    string[] b = a[1].Split('&');
                }
            }
            return null;
        }

        public static async Task<VKSession> AuthVKUser()
        {
            Uri result = await AuthWebView.AuthenticateAsync(new Uri(userUrl), new Uri(endUrl));
            if (result == null) return null;
            if (result.OriginalString.Contains("access_token"))
            {
                string[] a = result.OriginalString.Split('#');
                string[] b = a[1].Split('&');
                string id = b[2].Split('=')[1];
                string at = b[0].Split('=')[1];

                Log.General.Info("Auth success", new ValueSet { { "id", id } });

                VKSession session = new VKSession(Int32.Parse(id), at);
                session.StartSession();
                User currentUser = await session.API.Users.GetAsync();
                session.DisplayName = currentUser.FullName;
                session.Avatar = currentUser.Photo;
                Log.General.Info("User info received");

                await VKSession.AddSessionAsync(session);
                return session;
            }
            else if (result.OriginalString.Contains("error"))
            {
                string[] a = result.OriginalString.Split('#');
                string[] b = a[1].Split('&');
                string type = b[0].Split('=')[1];
                string message = b[1].Split('=')[1];
                Log.General.Error("Auth failed", new ValueSet { { "type", type }, { "message", message } });
                if (message == "user_denied") return null;
            }
            return null;
        }

        public static async Task<List<VKSession>> AuthVKGroupLegacy(VKSession groupOwnerSession, List<Group> groups)
        {
            List<VKSession> sessions = new List<VKSession>();
            foreach (var g in groups)
            {
                VKSession gs = new VKSession(groupOwnerSession.Id, groupOwnerSession.AccessToken, g.Id);
                gs.DisplayName = g.Name;
                gs.Avatar = g.Photo;
                sessions.Add(gs);
            }

            await VKSession.AddSessionsAsync(sessions);
            return sessions;
        }

        public static async Task<List<VKSession>> AuthVKGroup(VKSession groupOwnerSession, List<Group> groups)
        {
            List<VKSession> sessions = new List<VKSession>();
            foreach (var g in groups)
            {
                VKSession gs = new VKSession(groupOwnerSession.Id, groupOwnerSession.AccessToken, g.Id);
                gs.DisplayName = g.Name;
                gs.Avatar = g.Photo;
                sessions.Add(gs);
            }

            await VKSession.RefreshGroupSessionsAsync(groupOwnerSession, sessions);
            return sessions;
        }
    }
}

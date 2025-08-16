using Newtonsoft.Json;
using System;

namespace Elorucov.Laney.Models {
    public class VKSession {
        public long Id { get; private set; }
        public string Name { get; private set; }
        public string Avatar { get; private set; }

        [JsonIgnore]
        public Uri AvatarUri { get { return Uri.IsWellFormedUriString(Avatar, UriKind.Absolute) ? new Uri(Avatar) : null; } }

        public string AccessToken { get; private set; }
        public string VKMAccessToken { get; private set; }
        public long VKMAccessTokenExpires { get; private set; }
        public string VKMExchangeToken { get; private set; }
        public string LocalPasscode { get; set; }

        public VKSession(long id, string accessToken, string vkmAccessToken, long vkmExpires, string exchangeToken, string name, string avatar) {
            Id = id;
            AccessToken = accessToken;
            VKMAccessToken = vkmAccessToken;
            VKMAccessTokenExpires = vkmExpires;
            VKMExchangeToken = exchangeToken;
            Name = name;
            Avatar = avatar;
        }
    }
}
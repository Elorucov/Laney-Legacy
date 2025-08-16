using Elorucov.VkAPI.Helpers;
using Elorucov.VkAPI.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elorucov.VkAPI.Methods {
    public class Audios {
        public static async Task<object> Get(long playlistId, int offset, string accessToken) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "access_token", accessToken },
                { "count", "500" }
            };

            if (playlistId > 0) p.Add("playlist_id", playlistId.ToString());
            if (offset > 0) p.Add("offset", offset.ToString());
            var res = await API.SendRequestAsync("audio.get", p);
            return VKResponseHelper.ParseResponse<VKList<Audio>>(res);
        }

        public static async Task<object> GetRestrictionPopup(long audioId, string accessToken) {
            Dictionary<string, string> p = new Dictionary<string, string> {
                { "audio_id", audioId.ToString() },
                { "access_token", accessToken }
            };

            var res = await API.SendRequestAsync("audio.getRestrictionPopup", p);
            return VKResponseHelper.ParseResponse<AudioRestrictionInfo>(res);
        }
    }
}
using ELOR.VKAPILib.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Elorucov.Laney.Core
{
    public class CacheManager
    {
        private static List<User> CachedUsers = new List<User>();
        private static List<Group> CachedGroups = new List<Group>();

        public static void Add(IEnumerable<User> users)
        {
            if (users == null) return;
            foreach (User user in users)
            {
                Add(user);
            }
        }

        public static void Add(IEnumerable<Group> groups)
        {
            if (groups == null) return;
            foreach (Group group in groups)
            {
                Add(group);
            }
        }

        public static void Add(User user)
        {
            lock (CachedUsers)
            {
                int index = CachedUsers.FindIndex(i => i.Id == user.Id);
                if (index >= 0)
                {
                    CachedUsers[index] = user;
                }
                else
                {
                    CachedUsers.Add(user);
                }
            }
        }

        public static void Add(Group group)
        {
            lock (CachedGroups)
            {
                int index = CachedGroups.FindIndex(i => i.Id == group.Id);
                if (index >= 0)
                {
                    CachedGroups[index] = group;
                }
                else
                {
                    CachedGroups.Add(group);
                }
            }
        }

        public static User GetUser(int id)
        {
            try
            {
                return CachedUsers.FirstOrDefault(i => i.Id == id);
            }
            catch (Exception ex)
            {
                Log.General.Error($"Error while get user with id {id} from cache!", ex);
                return null;
            }
        }

        public static Group GetGroup(int id)
        {
            try
            {
                if (id < 0) id = id * -1;
                return CachedGroups.FirstOrDefault(i => i.Id == id);
            }
            catch (Exception ex)
            {
                Log.General.Error($"Error while get group with id {id} from cache!", ex);
                return null;
            }
        }

        // First name, last name, avatar
        public static Tuple<string, string, Uri> GetNameAndAvatar(int id)
        {
            if (id > 0)
            {
                User u = GetUser(id);
                if (u == null) return null;
                return new Tuple<string, string, Uri>(u.FirstName, u.LastName, u.Photo);
            }
            else if (id < 0)
            {
                Group g = GetGroup(id);
                if (g == null) return null;
                return new Tuple<string, string, Uri>(g.Name, null, g.Photo);
            }
            return null;
        }

        public static string GetNameOnly(int id)
        {
            var t = GetNameAndAvatar(id);
            if (t == null) return id.ToString();
            return id > 0 ? $"{t.Item1} {t.Item2}" : t.Item1;
        }

        // Cached files

        public static async Task<StorageFile> DownloadFileToCacheAsync(string folderName, Uri uri)
        {
            StorageFolder folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
            StorageFile file = (await folder.TryGetItemAsync(uri.Segments.Last())) as StorageFile;
            if (file != null)
            {
                return file;
            }

            HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter() { AllowUI = false };
            HttpClient client = new HttpClient(filter);
            HttpResponseMessage result = await client.GetAsync(uri);

            file = await folder.CreateFileAsync(uri.Segments.Last(), CreationCollisionOption.GenerateUniqueName);
            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await result.Content.WriteToStreamAsync(fileStream);
                await fileStream.FlushAsync();
            }
            return file;
        }
    }
}

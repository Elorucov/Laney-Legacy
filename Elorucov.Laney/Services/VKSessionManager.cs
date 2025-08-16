using Elorucov.Laney.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Elorucov.Laney.Services {
    // Это не похож на класс VKSession из L2.
    // С помощью него будем работать с файлом сессий.
    public class VKSessionManager {
        const string FileName = "sessions";
        const string ProtectionDescriptor = "LOCAL=user";

        public static async Task<List<VKSession>> GetSessionsAsync() {
            List<VKSession> sessions = new List<VKSession>();
            Logger.Log.Info($"Getting saved sessions...");

            try {
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
                if (file != null) {
                    IBuffer buff = await FileIO.ReadBufferAsync(file);
                    if (buff.Length == 0) return sessions;

                    string str = "";
                    DataProtectionProvider dpp = new DataProtectionProvider(ProtectionDescriptor);
                    IBuffer buffdec = await dpp.UnprotectAsync(buff);
                    using (var dr = DataReader.FromBuffer(buffdec)) {
                        dr.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                        str = dr.ReadString(buffdec.Length);
                    }

                    if (!string.IsNullOrEmpty(str)) sessions = JsonConvert.DeserializeObject<List<VKSession>>(str);
                }
                Logger.Log.Info($"Loaded {sessions.Count} session(-s).");
            } catch (Exception ex) {
                Logger.Log.Error($"Failed to load sessions! 0x{ex.HResult.ToString()}: {ex.Message}");
            }

            return sessions;
        }

        public static async Task AddOrUpdateSessionAsync(VKSession session) {
            List<VKSession> sessions = await GetSessionsAsync();

            var foundSession = sessions.Where(s => s.Id == session.Id).ToList().FirstOrDefault();
            if (foundSession != null) sessions.Remove(foundSession);

            if (sessions.Count >= 3) throw new Exception("Cannot add more than 3 sessions!");
            sessions.Add(session);
            sessions = sessions.OrderBy(s => s.Id).ToList();
            await SaveSessionsAsync(sessions);
        }

        public static async Task DeleteSessionAsync(long id) {
            List<VKSession> sessions = await GetSessionsAsync();
            if (sessions.Count == 0) throw new Exception("This session is not found!");

            var session = sessions.Where(s => s.Id == id).ToList().FirstOrDefault();
            if (session == null) throw new Exception("This session is not found!");

            sessions.Remove(session);
            await SaveSessionsAsync(sessions);
        }

        private static async Task SaveSessionsAsync(List<VKSession> sessions) {
            Logger.Log.Info($"Saving sessions... (count: {sessions.Count})");
            string str = JsonConvert.SerializeObject(sessions);
            DataProtectionProvider dpp = new DataProtectionProvider(ProtectionDescriptor);
            var buffdec = Encoding.UTF8.GetBytes(str).AsBuffer();
            var buff = await dpp.ProtectAsync(buffdec);

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
            if (file != null) {
                await FileIO.WriteBufferAsync(file, buff);
                Logger.Log.Info($"Sessions saved.");
            }
        }
    }
}
using Elorucov.Laney.Services.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Elorucov.Laney.Services.Logger {
    public class Log {
        static StreamWriter sw = null;
        static string filename;

        public static int LoggerViewId = 0;

        public static async Task InitAsync(string firstLine = null) {
            try {
                _logs = new Queue<string>();
                if (string.IsNullOrEmpty(filename)) filename = $"laney_{DateTimeOffset.Now.ToUnixTimeSeconds()}.log";
                var a = await ApplicationData.Current.LocalFolder.CreateFolderAsync("logs", CreationCollisionOption.OpenIfExists);
                sw = File.AppendText($"{a.Path}\\{filename}");
                string start = $"Laney version: {Services.ApplicationInfo.GetVersion()}.\nOS Version: {Functions.GetOSVersion()}; Device type: {Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily}.\n";
                Debug.WriteLine($"Logger: {start}");
                if (string.IsNullOrEmpty(firstLine)) sw.WriteLine(start);
                StorageFile oldest = null;
                var b = await a.GetFilesAsync();
                foreach (var c in b) {
                    if (oldest == null || c.DateCreated < oldest.DateCreated) oldest = c;
                }
                if (oldest != null && b.Count > 10) await oldest.DeleteAsync();
            } catch { }
        }

        public static void UnInit() {
            if (sw != null) {
                sw.Flush();
                sw.Dispose();
                sw = null;
            }
        }

        private static Queue<string> _logs;

        public delegate void WroteDelegate(string text);
        public static event WroteDelegate Wrote;

        public static void Verbose(string message) {
            if (AppParameters.AdvancedLogging) Write("[VRB]", message);
        }

        public static void Info(string message) {
            Write("[INF]", message);
        }

        public static void Warn(string message) {
            Write("[WRN]", message);
        }

        public static void Error(string message) {
            Write("[ERR]", message);
        }

        public static void Error(Exception ex, string message) {
            Write("[ERR]", message + $"(0x{ex.HResult.ToString("x8")}: {ex.Message})");
        }

        public static void Write(string prefix, string text) {
            if (sw != null && _logs != null) {
                try {
                    string t = GetNormalDateTime();
                    Debug.WriteLine($"Logger: {prefix} {text}");

                    if (_logs.Count == 100) {
                        _logs.Dequeue();
                    }
                    _logs.Enqueue($"{t} | {prefix} {text}");

                    Wrote?.Invoke($"{t} | {prefix} {text}");
                    sw.WriteLine($"{t} | {prefix} {text}");
                } catch (Exception ex) {
                    Debug.WriteLine($"Error while logging, lol! 0x{ex.HResult.ToString("x8")}");
                }
            }
        }

        private static string GetNormalDateTime() {
            DateTime dt = DateTime.Now;
            int ms = dt.Millisecond;
            string mss = ms.ToString();
            if (ms < 10) {
                mss = $"00{ms}";
            } else if (ms >= 10 && ms < 100) {
                mss = $"0{ms}";
            }
            return $"{dt.ToString("d")} {dt.ToString("T")}.{mss}";
        }
    }
}
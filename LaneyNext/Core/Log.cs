using Elorucov.Laney.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Elorucov.Laney.Core
{
    public class Log
    {
        string FileName = "";
        StorageFolder folder = null;
        bool stopped = false;

        #region Instances and static methods

        private static List<Log> registeredLoggers = new List<Log>();

        public static Log General { get; set; }

        public static void StopAll()
        {
            foreach (var log in registeredLoggers)
            {
                log.StopInternal();
            }
        }

        public static void ReinitAllLogs()
        {
            var rl = new List<Log>(registeredLoggers);
            foreach (var log in rl)
            {
                log.ReinitRequested?.Invoke(log, null);
            }
        }

        #endregion

        public Log(string providerName)
        {
            FileName = $"laney_{providerName}_{DateTimeOffset.Now.ToUnixTimeSeconds()}.log";
            registeredLoggers.Add(this);
            Init();
        }

        private async void Init()
        {
            try
            {
                folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("logs", CreationCollisionOption.OpenIfExists);

                var v = AppInfo.Version;
                string start = $"Laney version: {v.Major}.{v.Minor}.{v.Build}.\nOS Version: {OSHelper.GetVersion()}; Device type: {Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily}.\n";
                Debug.WriteLine($"Logger: {start}");
                File.AppendAllText($"{folder.Path}\\{FileName}", start);

                StorageFile oldest = null;
                var b = await folder.GetFilesAsync();
                foreach (var c in b)
                {
                    if (oldest == null || c.DateCreated < oldest.DateCreated) oldest = c;
                }
                if (oldest != null && b.Count > 30) await oldest.DeleteAsync();
            }
            catch { }
        }

        public void Stop()
        {
            registeredLoggers.Remove(this);
            StopInternal();
        }

        private void StopInternal()
        {
            stopped = true;
        }

        public event EventHandler ReinitRequested;

        //

        private void Write(string type, string line)
        {
            if (!stopped)
                File.AppendAllText($"{folder.Path}\\{FileName}", $"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss K")} [{type}] {line}\n");
        }

        public void Verbose(string message, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            Debug.WriteLine($"VERBOSE: {message}");
            string c = GetCaller(caller);
            Write("VRB", $"[{c}]: {message}");
        }

        public void Info(string message, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            Debug.WriteLine($"INFO: {message}");
            string c = GetCaller(caller);
            Write("INF", $"[{c}]: {message}");
        }

        public void Warn(string message, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            Debug.WriteLine($"WARN: {message}");
            string c = GetCaller(caller);
            Write("WRN", $"[{c}]: {message}");
        }

        public void Error(string message, Exception ex = null, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            string exc = ex != null ? $" HResult: {ex.HResult.ToString("x8")}" : "";
            Debug.WriteLine($"ERROR: {message}.{exc}");
            string c = GetCaller(caller);
            Write("ERR", $"[{c}]: {message}{exc}");
        }

        public void Critical(string message, Exception ex = null, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            string exc = ex != null ? $" HResult: {ex.HResult.ToString("x8")}\nMessage: {ex.Message.Trim()}" : "";
            Debug.WriteLine($"CRITICAL: {message}.{exc}");
            string c = GetCaller(caller);
            Write("ERC", $"[{c}]: {message}{exc}");
        }

        public void Verbose(string message, ValueSet fields, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            Debug.WriteLine($"VERBOSE: {message}");
            string c = GetCaller(caller);
            Write("VRB", $"[{c}]: {message} {ParseFields(fields)}");
        }

        public void Info(string message, ValueSet fields, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            Debug.WriteLine($"INFO: {message}");
            string c = GetCaller(caller);
            Write("INF", $"[{c}]: {message} {ParseFields(fields)}");
        }

        public void Warn(string message, ValueSet fields, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            Debug.WriteLine($"WARN: {message}");
            string c = GetCaller(caller);
            Write("WRN", $"[{c}]: {message} {ParseFields(fields)}");
        }

        public void Error(string message, ValueSet fields, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            Debug.WriteLine($"ERROR: {message}");
            string c = GetCaller(caller);
            Write("ERR", $"[{c}]: {message} {ParseFields(fields)}");
        }

        public void Critical(string message, ValueSet fields, [CallerMemberName] string caller = Constants.LogUnknownMethod)
        {
            Debug.WriteLine($"CRITICAL: {message}");
            string c = GetCaller(caller);
            Write("ERC", $"[{c}]: {message} {ParseFields(fields)}");
        }

        private string GetCaller(string method)
        {
#if DEBUG
            int count = 3;
            int index = 2;
#else
            int count = 5;
            int index = 4;
#endif

            StackTrace st = new StackTrace();
            if (st.FrameCount >= count)
            {
                StackFrame fr = st.GetFrame(index);
                if (fr.HasMethod())
                {
                    var m = fr.GetMethod();
                    return m.IsConstructor ? $"{m.Name} constructor" : $"{m.DeclaringType.Name}.{m.Name}";
                }
            }
            return $"UnknownClass.{method}";
        }

        private object ParseFields(ValueSet fields)
        {
            string s = String.Empty;
            foreach (var f in fields)
            {
                s += $"{f.Key} = {f.Value}; ";
            }
            return String.IsNullOrEmpty(s) ? s : $"({s.Trim()})";
        }
    }
}

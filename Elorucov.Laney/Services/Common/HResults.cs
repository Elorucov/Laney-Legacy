using System;

namespace Elorucov.Laney.Services.Common {
    public class AdvancedErrorInfo {
        public string Info { get; set; }
        public string AdvancedInfo { get; set; }
        public Exception Exception { get; set; }
    }

    public class HResults {
        public static AdvancedErrorInfo GetAdvancedErrorInfo(Exception ex) {
            switch (ex.HResult) {
                case -2147012889: return new AdvancedErrorInfo { Info = Locale.Get("network_error_no_internet"), AdvancedInfo = Locale.Get("network_error_descr"), Exception = ex };
                case -2147012865: return new AdvancedErrorInfo { Info = Locale.Get("network_error"), AdvancedInfo = Locale.Get("network_error_descr"), Exception = ex };
                default: return new AdvancedErrorInfo { Info = $"{Locale.Get("global_error")} (0x{ex.HResult.ToString("x8")})", AdvancedInfo = ex.Message, Exception = ex };
            }
        }
    }
}
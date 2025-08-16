using Windows.Storage;

namespace LaneyWidgets {
    internal class AppParameters {
        private static ApplicationDataContainer adc = ApplicationData.Current.LocalSettings;

        const string id = "id";
        const string at = "at";

        public static long UserID {
            get { return adc.Values[id] != null && adc.Values[id] is long ? (long)adc.Values[id] : 0; }
            set { adc.Values[id] = value; }
        }

        public static string AccessToken {
            get { return adc.Values[at] != null ? adc.Values[at].ToString() : null; }
            set { adc.Values[at] = value; }
        }
    }
}

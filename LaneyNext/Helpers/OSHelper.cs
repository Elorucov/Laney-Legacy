using Windows.Foundation.Metadata;
using Windows.System.Profile;

namespace Elorucov.Laney.Helpers
{
    public class OSHelper
    {
        public static string GetVersion()
        {
            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = (v & 0x000000000000FFFFL);
            return $"{v1}.{v2}.{v3}.{v4}";
        }

        public static ulong GetBuild()
        {
            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            return (v & 0x00000000FFFF0000L) >> 16;
        }

        public static bool IsAPIContractPresent(ushort major)
        {
            return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", major);
        }

        public static bool IsDesktop { get { return AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop"; } }
    }
}

using VK.VKUI.Popups;

namespace Elorucov.Laney.Helpers
{
    public class Utils
    {
        public static async void ShowUnderConstructionInfo()
        {
            await new Alert
            {
                Header = "Under construction",
                Text = "Do not send a bug report about under-construction functions.",
                PrimaryButtonText = Core.Locale.Get("close")
            }.ShowAsync();
        }

        public static bool CheckFlag(int sumflag, int flag)
        {
            int[] flags = new int[] { 1, 2, 4, 8, 32, 64, 128, 65536, 131072, 262144 };
            foreach (int f in flags)
            {
                if ((sumflag & f) == flag) return true;
            }
            return false;
        }
    }
}

using System.Globalization;
using Windows.UI;

namespace Elorucov.Laney.Helpers.UI
{
    public class ColorHelper
    {
        public static Color ParseFromHex(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

            return Color.FromArgb(255, r, g, b);
        }
    }
}

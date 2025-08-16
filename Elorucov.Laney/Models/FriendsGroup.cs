using Elorucov.VkAPI.Objects;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Models {
    public class FriendsGroup {
        public string Name { get; private set; }
        public string Icon { get; private set; }
        public FontFamily IconFontFamily { get; private set; }
        public ObservableCollection<User> Items { get; set; }

        public FriendsGroup(string name, string icon, FontFamily iconFontFamily, ObservableCollection<User> Users, bool sort = false) {
            Name = name;
            Icon = icon;
            IconFontFamily = iconFontFamily;
            Items = new ObservableCollection<User>();

            foreach (var u in Users) {
                if (u.Deactivated == DeactivationState.No && u.CanWritePrivateMessage == 1) Items.Add(u);
            }

            if (sort) Items = SortUsers(Items);
        }

        private ObservableCollection<User> SortUsers(ObservableCollection<User> Users) {
            return new ObservableCollection<User>(Users.OrderBy(z => z.FirstName));
        }
    }
}
using System;

namespace Elorucov.Laney.Models {
    public class SettingPageEntry {
        public Type Page { get; private set; }
        public string Title { get; private set; }
        public string Icon { get; private set; }

        public SettingPageEntry(Type page, string title, string icon) {
            Page = page;
            Title = title;
            Icon = icon;
        }
    }
}

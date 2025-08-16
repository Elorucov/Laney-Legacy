using Elorucov.Laney.Services;
using System;

namespace Elorucov.Laney.Models {
    public class UICommand {
        public char Icon { get; set; }
        public string Label { get; set; }
        public bool IsDestructive { get; private set; }
        public RelayCommand Action { get; private set; }

        public UICommand(char icon, string label, bool isDestructive, Action<object> action) {
            Icon = icon;
            Label = label;
            IsDestructive = isDestructive;
            Action = new RelayCommand(action);
        }
    }
}

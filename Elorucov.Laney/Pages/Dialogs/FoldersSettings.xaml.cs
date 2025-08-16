using Elorucov.Laney.Pages.Dialogs.Folders;
using Elorucov.Toolkit.UWP.Controls;
using System;
using Windows.UI.Xaml.Media.Animation;

namespace Elorucov.Laney.Pages.Dialogs {
    public sealed partial class FoldersSettings : Modal {
        public FoldersSettings() {
            this.InitializeComponent();
            MainFrame.Navigate(typeof(MainSettings), null, new SuppressNavigationTransitionInfo());
        }

        public FoldersSettings(int folderId, string name) {
            this.InitializeComponent();
            MainFrame.Navigate(typeof(FolderEditor), new Tuple<int, string, Action>(folderId, name, () => Hide()), new SuppressNavigationTransitionInfo());
        }
    }
}
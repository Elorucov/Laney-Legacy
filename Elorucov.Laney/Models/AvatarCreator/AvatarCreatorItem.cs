using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Models.AvatarCreator {
    public enum RenderMode { InGridViewItem, InCanvas }

    public interface IAvatarCreatorItem {
        Task<FrameworkElement> RenderAsync(RenderMode mode, bool extraFlag);
    }

    public class AvatarCreatorItemCollection {
        public string Name { get; private set; }
        public ObservableCollection<IAvatarCreatorItem> Items { get; private set; }
        public Uri PreviewImageUri { get; private set; }

        public AvatarCreatorItemCollection(string name, IEnumerable<IAvatarCreatorItem> items, Uri previewImageUri = null) {
            Name = name;
            Items = new ObservableCollection<IAvatarCreatorItem>(items);
            PreviewImageUri = previewImageUri;
        }
    }
}

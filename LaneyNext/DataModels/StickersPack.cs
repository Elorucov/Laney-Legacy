using ELOR.VKAPILib.Objects;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;

namespace Elorucov.Laney.DataModels
{
    public class StickersPack
    {
        private string _title;
        private Uri _preview;
        private DataTemplate _icon;
        private ObservableCollection<Sticker> _stickers = new ObservableCollection<Sticker>();

        public string Title { get { return _title; } set { _title = value; } }
        public Uri Preview { get { return _preview; } set { _preview = value; } }
        public DataTemplate Icon { get { return _icon; } set { _icon = value; } }
        public ObservableCollection<Sticker> Stickers { get { return _stickers; } set { _stickers = value; } }
    }
}

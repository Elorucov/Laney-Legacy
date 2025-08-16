using Elorucov.VkAPI.Objects;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Elorucov.Laney.Models {
    public enum StickerFlyoutItemType {
        GraffitiMenu, StickerPackView, UGCStickerPackView, EmojiPicker
    }

    public class StickerFlyoutItem : INotifyPropertyChanged {
        private string _title;
        private string _hint;
        private Uri _previewImage;
        private char _glyph;
        private string _path = "m 0, 0 z";
        private StickerFlyoutItemType _type;
        private ObservableCollection<Sticker> _stickers;
        private ObservableCollection<UGCSticker> _chatStickers;
        private ObservableCollection<Document> _graffities;

        public string Title { get { return _title; } set { _title = value; OnPropertyChanged(); } }
        public string Hint { get { return _hint; } set { _hint = value; OnPropertyChanged(); } }
        public Uri PreviewImage { get { return _previewImage; } set { _previewImage = value; OnPropertyChanged(); } }
        public char Glyph { get { return _glyph; } set { _glyph = value; OnPropertyChanged(); } }
        public string Path { get { return _path; } set { _path = value; OnPropertyChanged(); } }
        public StickerFlyoutItemType Type { get { return _type; } set { _type = value; OnPropertyChanged(); } }
        public ObservableCollection<Sticker> Stickers { get { return _stickers; } set { _stickers = value; OnPropertyChanged(); } }
        public ObservableCollection<UGCSticker> ChatStickers { get { return _chatStickers; } set { _chatStickers = value; OnPropertyChanged(); } }
        public ObservableCollection<Document> Graffities { get { return _graffities; } set { _graffities = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
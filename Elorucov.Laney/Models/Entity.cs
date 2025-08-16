using Elorucov.Laney.Services;
using Elorucov.VkAPI.Objects;
using System;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Models {
    public class Entity<T> {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public Uri Image { get; set; }
        public T Object { get; set; }
        public DependencyObject ExtraButtonIcon { get; set; }
        public RelayCommand ExtraButtonCommand { get; set; }

        public Entity() { }

        public Entity(long id, string title, string subtitle, Uri image) {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            Image = image;
        }

        public Entity(T obj, long id, string title, string subtitle = null, Uri image = null) {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            Image = image;
            Object = obj;
        }
    }

    public class Entity : Entity<object> {
        public Entity() : base() { }

        public Entity(long id, string title, string subtitle, Uri image = null) : base(id, title, subtitle, image) { }
    }

    // Required for properly bind from XAML.
    public class ChatMemberEntity : Entity<ChatMember> {
        public ChatMemberEntity(ChatMember obj, long id, string title, string subtitle, Uri image = null) : base(obj, id, title, subtitle, image) { }
    }
}
using Elorucov.Laney.Helpers;
using System;
using Windows.UI.Xaml;

namespace Elorucov.Laney.DataModels
{
    public class Entity<T>
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public Uri Image { get; set; }
        public T Object { get; set; }
        public DependencyObject ExtraButtonIcon { get; set; }
        public RelayCommand ExtraButtonCommand { get; set; }

        public Entity() { }

        public Entity(int id, string title, string subtitle, Uri image)
        {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            Image = image;
        }
    }

    public class Entity : Entity<object>
    {
        public Entity() : base()
        {
        }

        public Entity(int id, string title, string subtitle, Uri image) : base(id, title, subtitle, image)
        {
        }
    }
}
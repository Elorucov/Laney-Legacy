using Elorucov.Laney.Services.Logger;
using Elorucov.VkAPI.Objects;
using System;
using Windows.Foundation;

namespace Elorucov.Laney.Models {
    public class GalleryItem {
        public long OwnerId { get; private set; }
        public string Description { get; private set; }
        public DateTime Date { get; private set; }
        public Size Size { get; private set; }
        public Uri Source { get; private set; }
        public IPreview OriginalObject { get; private set; }

        public GalleryItem(Photo photo) {
            if (photo == null) {
                Log.Warn($"Attempting to create GalleryItem without photo!");
                return;
            }
            OwnerId = photo.OwnerId;
            Description = photo.Text;
            Date = photo.Date;
            Size = photo.MaximalSizedPhoto.Size;
            Source = photo.MaximalSizedPhoto.Uri;
            OriginalObject = photo;
        }

        public GalleryItem(Document doc) {
            if (doc == null) {
                Log.Warn($"Attempting to create GalleryItem without doc!");
                return;
            }
            OwnerId = doc.OwnerId;
            Description = doc.Title;
            Date = doc.DateTime;
            Size = doc.Preview.Photo.MaximalSizedPhoto.Size;
            Source = doc.Uri;
            OriginalObject = doc;
        }
    }
}
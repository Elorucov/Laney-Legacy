using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;

namespace Elorucov.Laney.DataModels
{
    public class PhotoViewerItem
    {
        public Uri Path { get; private set; }
        public int OwnerId { get; private set; }
        public string OwnerName { get; private set; }
        public Uri OwnerAvatar { get; private set; }
        public string Description { get; private set; }
        public string PublishTime { get; private set; }
        public AttachmentBase Attachment { get; private set; }

        public PhotoViewerItem(AttachmentBase attachment)
        {
            if (attachment is Photo p)
            {
                Path = p.MaximalSizedPhoto.Uri;
                OwnerId = p.OwnerId;
                Description = p.Text;
                PublishTime = p.Date.ToTimeAndDate();
            }
            else if (attachment is Document d && (d.Type == DocumentType.Image || d.Type == DocumentType.GIF))
            {
                Path = d.Uri;
                OwnerId = d.OwnerId;
                Description = d.Title;
                PublishTime = d.DateTime.ToTimeAndDate();
            }
            else
            {
                throw new ArgumentException("Supporting only photo and image/GIF document");
            }

            Attachment = attachment;
            if (OwnerId > 0)
            {
                User u = CacheManager.GetUser(OwnerId);
                OwnerName = u.FullName;
                OwnerAvatar = u.Photo;
            }
            else if (OwnerId < 0)
            {
                Group g = CacheManager.GetGroup(OwnerId);
                OwnerName = g.Name;
                OwnerAvatar = g.Photo;
            }
        }
    }
}

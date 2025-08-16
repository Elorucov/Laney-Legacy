using System;
using Windows.Foundation;

namespace Elorucov.VkAPI.Objects {
    public interface IPreview {
        Uri PreviewImageUri { get; }
        Size PreviewImageSize { get; }
    }
}
using System;

namespace Elorucov.Laney.DataModels
{
    public class VideoSource
    {
        public int Resolution { get; private set; }
        public Uri Source { get; private set; }

        public VideoSource(int resolution, Uri source)
        {
            Resolution = resolution;
            Source = source;
        }
    }
}
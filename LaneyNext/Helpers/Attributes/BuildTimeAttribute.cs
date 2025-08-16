using System;

namespace Elorucov.Laney.Helpers.Attributes
{
    public class BuildTimeAttribute : Attribute
    {
        internal int BuildTimeUnix = 0;
        public BuildTimeAttribute(int buildTimeUnix)
        {
            BuildTimeUnix = buildTimeUnix;
        }
    }
}

using System;

namespace Elorucov.Laney.Models {
    public class TwoStringTuple : Tuple<string, string> {
        public TwoStringTuple(string item1, string item2) : base(item1, item2) { }
    }
}

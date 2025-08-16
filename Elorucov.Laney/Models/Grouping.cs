using System.Collections.Generic;

namespace Elorucov.Laney.Models {
    internal class Grouping<T> {
        public char Icon { get; set; }
        public string Key { get; set; }

        public IEnumerable<T> Items { get; private set; }

        public Grouping(IEnumerable<T> items, string key = null) {
            Items = items;
            Key = key;
        }
    }
}

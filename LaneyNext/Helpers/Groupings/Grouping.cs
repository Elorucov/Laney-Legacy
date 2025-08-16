using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Elorucov.Laney.Helpers.Groupings
{
    public class Grouping<TKey, TElement> : ThreadSafeObservableCollection<TElement>, IGrouping<TKey, TElement>, IEnumerable<TElement>
    {
        public TKey Key
        {
            get;
        }

        public DataTemplate IconTemplate { get; private set; }

        public string SemanticZoomGlyph { get; private set; }
        public FontFamily SemanticZoomGlyphFontFamily { get; private set; }

        public Grouping(TKey key)
        {
            Key = key;
        }

        public Grouping(TKey key, IEnumerable<TElement> items)
            : this(key)
        {
            foreach (TElement item in items)
            {
                Add(item);
            }
        }

        public Grouping(TKey key, string semanticZoomGlyph, string semanticZoomGlyphFontFamily = null) : this(key)
        {
            SemanticZoomGlyph = semanticZoomGlyph;
            SemanticZoomGlyphFontFamily = new FontFamily(String.IsNullOrEmpty(semanticZoomGlyphFontFamily) ? "Segoe UI" : semanticZoomGlyphFontFamily);
        }

        public Grouping(TKey key, IEnumerable<TElement> items, string semanticZoomGlyph, string semanticZoomGlyphFontFamily = null) : this(key, items)
        {
            SemanticZoomGlyph = semanticZoomGlyph;
            SemanticZoomGlyphFontFamily = new FontFamily(String.IsNullOrEmpty(semanticZoomGlyphFontFamily) ? "Segoe UI" : semanticZoomGlyphFontFamily);
        }

        public Grouping(TKey key, IEnumerable<TElement> items, DataTemplate iconTemplate) : this(key, items)
        {
            IconTemplate = iconTemplate;
        }
    }
}

using Elorucov.Laney.Brushes;
using Elorucov.Laney.Core;
using System;
using System.Collections.Generic;
using VK.VKUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.DataModels
{
    public class BackgroundPickerItem
    {
        public Brush PreviewBrush { get; private set; }
        public string Label { get; private set; }
        public DataTemplate Icon { get; private set; }
        public ElementTheme Theme { get; private set; } = ElementTheme.Default;
        public int BackgroundType { get; private set; } // 3 — need for filepicker
        public string Background { get; private set; }
        public bool BackgroundImageStretch { get; private set; }

        public static BackgroundPickerItem GetForColorType()
        {
            int t = Settings.ChatBackgroundType;
            string color = t == 1 ? Settings.ChatBackground : Constants.DefaultChatBackgroundColor;
            return new BackgroundPickerItem
            {
                PreviewBrush = new SolidColorBrush(Helpers.UI.ColorHelper.ParseFromHex(color)),
                Label = Locale.Get("appearance_bkgpicker_color"),
                Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon28PaletteOutline),
                BackgroundType = 1,
                Background = color,
                Theme = ElementTheme.Dark
            };
        }

        public static List<BackgroundPickerItem> GetItems()
        {
            List<BackgroundPickerItem> list = new List<BackgroundPickerItem> {
                new BackgroundPickerItem {
                    Label = Locale.Get("appearance_bkgpicker_without_background"),
                    BackgroundType = 0,
                },
                // GetForColorType(),
                new BackgroundPickerItem {
                    Label = Locale.Get("appearance_bkgpicker_file"),
                    Icon = VKUILibrary.GetIconTemplate(VK.VKUI.Controls.VKIconName.Icon28PictureOutline),
                    BackgroundType = 3
                },
            };

            foreach (var i in ThemeManager.PreInstalledBackgrounds)
            {
                Brush brush;
                if (i.Value)
                {
                    brush = new ImageBrush { ImageSource = new BitmapImage(new Uri(i.Key)), Stretch = Stretch.UniformToFill };
                }
                else
                {
                    brush = new TiledBrush { Source = new Uri(i.Key) };
                }

                list.Add(new BackgroundPickerItem
                {
                    PreviewBrush = brush,
                    Theme = ElementTheme.Dark,
                    BackgroundType = 2,
                    Background = i.Key,
                    BackgroundImageStretch = i.Value
                });
            }

            return list;
        }
    }
}

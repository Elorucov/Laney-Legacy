using System;
using VK.VKUI;
using VK.VKUI.Controls;
using Windows.UI.Xaml;

namespace Elorucov.Laney.DataModels
{
    public class SettingsMenuItem
    {
        public string CategoryId { get; private set; }
        public string Title { get; private set; }
        public DataTemplate Icon { get; private set; }
        public Type Page { get; private set; }

        public SettingsMenuItem(string categoryId, string title, VKIconName icon, Type page)
        {
            CategoryId = categoryId;
            Title = title;
            Icon = VKUILibrary.GetIconTemplate(icon);
            Page = page;
        }
    }
}

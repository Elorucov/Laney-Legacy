using System.Collections.Generic;
using VK.VKUI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Elorucov.Laney.Views.Settings
{
    public class SettingsPageBase : Page
    {
        public static readonly DependencyProperty CategoryIdProperty = DependencyProperty.Register(
                   "CategoryId", typeof(string), typeof(SettingsPageBase), new PropertyMetadata(default(object)));

        public string CategoryId
        {
            get { return (string)GetValue(CategoryIdProperty); }
            set { SetValue(CategoryIdProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
                   "Title", typeof(string), typeof(SettingsPageBase), new PropertyMetadata(default(object)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public List<PageHeaderButton> RightButtons { get; } = new List<PageHeaderButton>();

        public SettingsPageBase() : base()
        {
            Transitions.Add(new NavigationThemeTransition { DefaultNavigationTransitionInfo = new SuppressNavigationTransitionInfo() });
        }
    }
}
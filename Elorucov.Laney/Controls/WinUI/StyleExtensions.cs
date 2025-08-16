// https://github.com/CommunityToolkit/Windows/blob/main/components/SettingsControls/src/Helpers/StyleExtensions.cs

using System.Linq;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;

namespace Elorucov.Laney.Controls.WinUI {
    // Adapted from https://github.com/rudyhuyn/XamlPlus
    internal static class ResourceDictionaryExtensions {
        /// <summary>
        /// Copies  the <see cref="ResourceDictionary"/> provided as a parameter into the calling dictionary, includes overwriting the source location, theme dictionaries, and merged dictionaries.
        /// </summary>
        /// <param name="destination">ResourceDictionary to copy values to.</param>
        /// <param name="source">ResourceDictionary to copy values from.</param>
        internal static void CopyFrom(this ResourceDictionary destination, ResourceDictionary source) {
            if (source.Source != null) {
                destination.Source = source.Source;
            } else {
                // Clone theme dictionaries
                if (source.ThemeDictionaries != null) {
                    foreach (var theme in source.ThemeDictionaries) {
                        if (theme.Value is ResourceDictionary themedResource) {
                            var themeDictionary = new ResourceDictionary();
                            themeDictionary.CopyFrom(themedResource);
                            destination.ThemeDictionaries[theme.Key] = themeDictionary;
                        } else {
                            destination.ThemeDictionaries[theme.Key] = theme.Value;
                        }
                    }
                }

                // Clone merged dictionaries
                if (source.MergedDictionaries != null) {
                    foreach (var mergedResource in source.MergedDictionaries) {
                        var themeDictionary = new ResourceDictionary();
                        themeDictionary.CopyFrom(mergedResource);
                        destination.MergedDictionaries.Add(themeDictionary);
                    }
                }

                // Clone all contents
                foreach (var item in source) {
                    destination[item.Key] = item.Value;
                }
            }
        }
    }

    // Adapted from https://github.com/rudyhuyn/XamlPlus
    public static partial class StyleExtensions {
        // Used to distinct normal ResourceDictionary and the one we add.
        private sealed class StyleExtensionResourceDictionary : ResourceDictionary {
        }

        public static ResourceDictionary GetResources(Style obj) {
            return (ResourceDictionary)obj.GetValue(ResourcesProperty);
        }

        public static void SetResources(Style obj, ResourceDictionary value) {
            obj.SetValue(ResourcesProperty, value);
        }

        public static readonly DependencyProperty ResourcesProperty =
            DependencyProperty.RegisterAttached("Resources", typeof(ResourceDictionary), typeof(StyleExtensions), new PropertyMetadata(null, ResourcesChanged));

        private static void ResourcesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            if (!(sender is FrameworkElement frameworkElement)) {
                return;
            }

            var mergedDictionaries = frameworkElement.Resources?.MergedDictionaries;
            if (mergedDictionaries == null) {
                return;
            }

            var existingResourceDictionary =
                mergedDictionaries.FirstOrDefault(c => c is StyleExtensionResourceDictionary);
            if (existingResourceDictionary != null) {
                // Remove the existing resource dictionary
                mergedDictionaries.Remove(existingResourceDictionary);
            }

            if (e.NewValue is ResourceDictionary resource) {
                var clonedResources = new StyleExtensionResourceDictionary();
                clonedResources.CopyFrom(resource);
                mergedDictionaries.Add(clonedResources);
            }


            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) {
                // Only force if the style was applied after the control was loaded
                if (frameworkElement.IsLoaded) ForceControlToReloadThemeResources(frameworkElement);
            } else {
                ForceControlToReloadThemeResources(frameworkElement);
            }
        }

        private static void ForceControlToReloadThemeResources(FrameworkElement frameworkElement) {
            // To force the refresh of all resource references.
            // Note: Doesn't work when in high-contrast.
            var currentRequestedTheme = frameworkElement.RequestedTheme;
            frameworkElement.RequestedTheme = currentRequestedTheme == ElementTheme.Dark
                ? ElementTheme.Light
                : ElementTheme.Dark;
            frameworkElement.RequestedTheme = currentRequestedTheme;
        }
    }
}
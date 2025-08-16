using Elorucov.Laney.Services.Network;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Elorucov.Laney.Services.Converters {
    public class ReactionChipMembersConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is List<long> members) {
                ObservableCollection<UserAvatarItem> avatars = new ObservableCollection<UserAvatarItem>();
                foreach (long mid in members) {
                    var info = AppSession.GetNameAndAvatar(mid);
                    if (info != null) {
                        BitmapImage ava = new BitmapImage();
                        new Action(async () => { await ava.SetUriSourceAsync(info.Item3); })();
                        avatars.Add(new UserAvatarItem {
                            // Name = info.Item1,
                            Image = ava
                        });
                    } else {
                        Logger.Log.Warn($"ReactionChips: Member {mid} is not found in cache!");
                    }
                }
                return avatars;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }

    public class ReactionChipContentVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is List<long> members) {
                bool displayAvatars = members.Count == 0 || members.Count > 3;
                if (parameter != null) { // For counter
                    return !displayAvatars ? Visibility.Collapsed : Visibility.Visible;
                } else { // For avatars
                    return displayAvatars ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }
}

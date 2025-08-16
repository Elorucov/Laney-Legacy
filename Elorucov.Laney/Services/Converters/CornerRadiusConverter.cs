﻿// https://github.com/CommunityToolkit/Windows/blob/main/components/SettingsControls/src/Helpers/CornerRadiusConverter.cs

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Elorucov.Laney.Services.Converters {
    public class CornerRadiusConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is CornerRadius cornerRadius) {
                return new CornerRadius(0, 0, cornerRadius.BottomRight, cornerRadius.BottomLeft);
            } else {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return value;
        }
    }
}

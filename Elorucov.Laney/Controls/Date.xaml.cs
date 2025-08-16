using Elorucov.Laney.Services;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class Date : UserControl {
        long id = 0;
        public Date() {
            this.InitializeComponent();
            id = RegisterPropertyChangedCallback(DateTimeProperty, ChangedCallback);
            Unloaded += (a, b) => { if (id != 0) UnregisterPropertyChangedCallback(DateTimeProperty, id); };
        }

        public static readonly DependencyProperty DateTimeProperty = DependencyProperty.Register(
                   "DateTime", typeof(DateTime?), typeof(Date), new PropertyMetadata(default(object)));

        public DateTime? DateTime {
            get { return (DateTime?)GetValue(DateTimeProperty); }
            set { SetValue(DateTimeProperty, value); }
        }

        public event RoutedEventHandler Click;

        private void ChangedCallback(DependencyObject sender, DependencyProperty dp) {
            if (DateTime != null && DateTime > new DateTime(2006, 1, 1)) {
                Root.Visibility = Visibility.Visible;
                dt.Text = APIHelper.GetNormalizedDate(DateTime.Value);

                List<UIElement> receivers = new List<UIElement> { this };
                Services.UI.Shadow.TryDrawUsingThemeShadow(Root, ShadowContainer, receivers, 8);
            } else {
                Root.Visibility = Visibility.Collapsed;
            }
        }

        private void OnClick(object sender, RoutedEventArgs e) {
            Click?.Invoke(this, e);
        }
    }
}
using Elorucov.Laney.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class MentionsPicker : UserControl {
        long id = 0;

        public static DependencyProperty MentionsProperty = DependencyProperty.Register(nameof(Mentions), typeof(List<MentionItem>), typeof(MentionsPicker), new PropertyMetadata(null));
        public List<MentionItem> Mentions {
            get { return (List<MentionItem>)GetValue(MentionsProperty); }
            set { SetValue(MentionsProperty, value); }
        }

        public static DependencyProperty SearchDomainProperty = DependencyProperty.Register(nameof(SearchDomain), typeof(string), typeof(MentionsPicker), new PropertyMetadata(null));
        public string SearchDomain {
            get { return (string)GetValue(SearchDomainProperty); }
            set { SetValue(SearchDomainProperty, value); }
        }

        public event EventHandler<MentionItem> MentionPicked;

        public MentionsPicker() {
            this.InitializeComponent();
            id = RegisterPropertyChangedCallback(MentionsProperty, (a, b) => ShowMentions());
            Unloaded += (a, b) => UnregisterPropertyChangedCallback(MentionsProperty, id);
            Loaded += (a, b) => ShowMentions();
        }

        private void ShowMentions() {
            if (Mentions != null && Mentions.Count > 0) {
                Debug.WriteLine($"Mentions: {Mentions.Count}");
            }
            mentionslist.ItemsSource = Mentions;
        }

        public void FocusToList() {
            mentionslist.Focus(FocusState.Keyboard);
        }

        private void Hide(object sender, TappedRoutedEventArgs e) {
            Visibility = Visibility.Collapsed;
        }

        private void Mentionslist_ItemClick(object sender, ItemClickEventArgs e) {
            Visibility = Visibility.Collapsed;
            MentionPicked?.Invoke(this, e.ClickedItem as MentionItem);
        }
    }
}

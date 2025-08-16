using Elorucov.Laney.Services.UI;
using Elorucov.VkAPI.Objects;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пользовательский элемент управления" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls {
    public sealed partial class BotKeyboardControl : UserControl {
        public static readonly DependencyProperty KeyboardProperty = DependencyProperty.Register(
           "Keyboard", typeof(BotKeyboard), typeof(BotKeyboardControl), new PropertyMetadata(default(object)));

        public BotKeyboard Keyboard {
            get { return (BotKeyboard)GetValue(KeyboardProperty); }
            set { SetValue(KeyboardProperty, value); }
        }

        public static readonly DependencyProperty OwnerMessageIdProperty = DependencyProperty.Register(
            "OwnerMessageId", typeof(int), typeof(BotKeyboardControl), new PropertyMetadata(0));

        public int OwnerMessageId {
            get { return (int)GetValue(OwnerMessageIdProperty); }
            set { SetValue(OwnerMessageIdProperty, value); }
        }

        public event EventHandler<BotButtonAction> ButtonClicked;

        public BotKeyboardControl() {
            this.InitializeComponent();
            RegisterPropertyChangedCallback(KeyboardProperty, (a, b) => { DrawKeyboard((BotKeyboard)GetValue(b)); });
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            DrawKeyboard(Keyboard);
        }

        private void DrawKeyboard(BotKeyboard keyboard) {
            ButtonsArea.Children.Clear();
            ButtonsArea.Padding = new Thickness(0);

            if (keyboard == null || keyboard.Buttons == null) return;
            foreach (var row in keyboard.Buttons) {
                Grid g = new Grid();
                g.MaxWidth = 540;
                for (int i = 0; i < row.Count; i++) {
                    g.ColumnDefinitions.Add(new ColumnDefinition());
                    Button btn = VKButtonHelper.GenerateButton(OwnerMessageId, row[i], this, ButtonClicked, keyboard.Inline);
                    btn.Margin = new Thickness(3);
                    g.Children.Add(btn);
                    Grid.SetColumn(btn, i);
                }
                ButtonsArea.Children.Add(g);
            }
            ButtonsArea.Padding = new Thickness(3);
        }
    }
}

using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.ViewModels.Modals;
using Elorucov.Toolkit.UWP.Controls;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Modals
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class PollEditor : Modal
    {
        public PollEditor(Poll poll = null)
        {
            this.InitializeComponent();
            Title = Locale.Get(poll == null ? "polleditor_create" : "polleditor");
            DataContext = new PollEditorViewModel(this, poll);
        }

        private async void LimitedTimeChecked(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => LimitDate.Focus(FocusState.Programmatic));
            }
        }
    }
}

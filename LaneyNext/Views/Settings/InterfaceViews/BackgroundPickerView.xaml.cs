using Elorucov.Laney.Core;
using Elorucov.Laney.DataModels;
using Elorucov.Laney.ViewModels.Settings.Interface;
using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.Views.Settings.InterfaceViews
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class BackgroundPickerView : SettingsPageBase
    {
        public BackgroundPickerView()
        {
            this.InitializeComponent();
            Title = Locale.Get("appearance_conv_backgrounds");
            CategoryId = Constants.SettingsInterfaceId;
            DataContext = new BackgroundPickerViewModel();
        }

        private void ItemPicked(object sender, ItemClickEventArgs e)
        {
            BackgroundPickerItem item = e.ClickedItem as BackgroundPickerItem;
            switch (item.BackgroundType)
            {
                case 0: ThemeManager.ResetChatBackground(); break;
                case 1: break; // Color picker
                case 2: ThemeManager.ChangeChatBackground(item.BackgroundType, item.Background, item.BackgroundImageStretch); break;
                case 3: PickFromFiles(); break; // Gallery
            }
        }

        private async void PickFromFiles()
        {
            FileOpenPicker fop = new FileOpenPicker();
            fop.FileTypeFilter.Add(".jpg");
            fop.FileTypeFilter.Add(".png");
            fop.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fop.ViewMode = PickerViewMode.Thumbnail;

            StorageFile file = await fop.PickSingleFileAsync();
            if (file != null)
            {
                ThemeManager.ChangeChatBackgroundFromFileAsync(file);
            }
        }
    }
}

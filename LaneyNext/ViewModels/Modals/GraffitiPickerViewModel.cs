using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class GraffitiPickerViewModel : CommonViewModel
    {
        private ObservableCollection<Document> _graffities;
        private RelayCommand _fileCommand;
        private RelayCommand _drawCommand;

        public ObservableCollection<Document> Graffities { get { return _graffities; } set { _graffities = value; OnPropertyChanged(); } }
        public RelayCommand FileCommand { get { return _fileCommand; } set { _fileCommand = value; OnPropertyChanged(); } }
        public RelayCommand DrawCommand { get { return _drawCommand; } set { _drawCommand = value; OnPropertyChanged(); } }

        public GraffitiPickerViewModel()
        {
            LoadRecentGraffities();
            FileCommand = new RelayCommand((o) => Utils.ShowUnderConstructionInfo());
            DrawCommand = new RelayCommand((o) => Utils.ShowUnderConstructionInfo());
        }

        private async void LoadRecentGraffities()
        {
            Placeholder = null;
            IsLoading = true;

            try
            {
                List<Document> documents = await VKSession.Current.API.Messages.GetRecentGraffitiesAsync(32);
                Graffities = new ObservableCollection<Document>(documents);
            }
            catch (Exception ex)
            {
                PlaceholderViewModel.GetForException(ex, () => LoadRecentGraffities());
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void AttachGraffiti(Document graffiti)
        {
            VKSession.Current.SessionBase.SelectedConversation.MessageInput.Attach(graffiti);
        }
    }
}

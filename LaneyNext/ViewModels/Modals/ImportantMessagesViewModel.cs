using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.VKAPIExecute.Objects;
using System;
using System.Collections.ObjectModel;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class ImportantMessagesViewModel : CommonViewModel
    {
        private ObservableCollection<MessageViewModel> _messages = new ObservableCollection<MessageViewModel>();

        public ObservableCollection<MessageViewModel> Messages { get { return _messages; } set { _messages = value; OnPropertyChanged(); } }

        public ImportantMessagesViewModel()
        {
            Load();
        }

        public async void Load()
        {
            if (IsLoading) return;
            Placeholder = null;
            IsLoading = true;
            try
            {
                ImportantMessagesResponse resp = await VKSession.Current.Execute.GetImportantMessagesAsync(Messages.Count, 40);
                CacheManager.Add(resp.Profiles);
                CacheManager.Add(resp.Groups);
                resp.Messages.Items.ForEach(m => Messages.Add(new MessageViewModel(m)));
            }
            catch (Exception ex)
            {
                if (Messages.Count == 0)
                {
                    Placeholder = PlaceholderViewModel.GetForException(ex, () => Load());
                }
                else
                {
                    if (await ExceptionHelper.ShowErrorDialogAsync(ex)) Load();
                }
            }
            IsLoading = false;
        }
    }
}

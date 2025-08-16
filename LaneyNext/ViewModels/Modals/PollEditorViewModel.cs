using ELOR.VKAPILib.Objects;
using Elorucov.Laney.Core;
using Elorucov.Laney.Helpers;
using Elorucov.Laney.ViewModels.Controls;
using Elorucov.Toolkit.UWP.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Elorucov.Laney.ViewModels.Modals
{
    public class PollEditorViewModel : BaseViewModel
    {
        private bool _isCreating;
        private string _question;
        private ObservableCollection<EditableTextViewModel> _answers = new ObservableCollection<EditableTextViewModel>();
        private ObservableCollection<PollBackgroundViewModel> _pollBackgrounds = new ObservableCollection<PollBackgroundViewModel>();
        private PollBackgroundViewModel _selectedPollBackground;
        private bool _isAnonymous;
        private bool _isMultipleSelectionEnabled;
        private bool _unvotingDisabled;
        private bool _isVotingTimeLimited;
        private DateTimeOffset _limitDate = DateTimeOffset.Now.AddDays(1);
        private TimeSpan _limitTime = DateTime.Now.TimeOfDay;
        private bool _invalidQuestionInfoVisible;
        private bool _invalidAnswersInfoVisible;
        private bool _invalidExpDateInfoVisible;
        private RelayCommand _addAnswerCommand;
        private RelayCommand _deleteAnswerCommand;
        private RelayCommand _saveCommand;

        public bool IsCreating { get { return _isCreating; } set { _isCreating = value; OnPropertyChanged(); } }
        public string Question { get { return _question; } set { _question = value; OnPropertyChanged(); } }
        public ObservableCollection<EditableTextViewModel> Answers { get { return _answers; } set { _answers = value; OnPropertyChanged(); } }
        public ObservableCollection<PollBackgroundViewModel> PollBackgrounds { get { return _pollBackgrounds; } set { _pollBackgrounds = value; OnPropertyChanged(); } }
        public PollBackgroundViewModel SelectedPollBackground { get { return _selectedPollBackground; } set { _selectedPollBackground = value; OnPropertyChanged(); } }
        public bool IsAnonymous { get { return _isAnonymous; } set { _isAnonymous = value; OnPropertyChanged(); } }
        public bool IsMultipleSelectionEnabled { get { return _isMultipleSelectionEnabled; } set { _isMultipleSelectionEnabled = value; OnPropertyChanged(); } }
        public bool UnvotingDisabled { get { return _unvotingDisabled; } set { _unvotingDisabled = value; OnPropertyChanged(); } }
        public bool IsVotingTimeLimited { get { return _isVotingTimeLimited; } set { _isVotingTimeLimited = value; OnPropertyChanged(); } }
        public DateTimeOffset LimitDate { get { return _limitDate; } set { _limitDate = value; OnPropertyChanged(); } }
        public TimeSpan LimitTime { get { return _limitTime; } set { _limitTime = value; OnPropertyChanged(); } }
        public bool InvalidQuestionInfoVisible { get { return _invalidQuestionInfoVisible; } set { _invalidQuestionInfoVisible = value; OnPropertyChanged(); } }
        public bool InvalidAnswersInfoVisible { get { return _invalidAnswersInfoVisible; } set { _invalidAnswersInfoVisible = value; OnPropertyChanged(); } }
        public bool InvalidExpDateInfoVisible { get { return _invalidExpDateInfoVisible; } set { _invalidExpDateInfoVisible = value; OnPropertyChanged(); } }
        public RelayCommand AddAnswerCommand { get { return _addAnswerCommand; } set { _addAnswerCommand = value; OnPropertyChanged(); } }
        public RelayCommand DeleteAnswerCommand { get { return _deleteAnswerCommand; } set { _deleteAnswerCommand = value; OnPropertyChanged(); } }
        public RelayCommand SaveCommand { get { return _saveCommand; } set { _saveCommand = value; OnPropertyChanged(); } }

        Modal View;

        public PollEditorViewModel(Modal view, Poll poll = null)
        {
            View = view;
            if (poll == null)
            {
                Answers.Add(new EditableTextViewModel());
                Answers.Add(new EditableTextViewModel());
            }
            else
            {
                // TODO: Edit poll
            }

            AddAnswerCommand = new RelayCommand(o =>
            {
                if (Answers.Count == 10) return;
                Answers.Add(new EditableTextViewModel());
            });
            DeleteAnswerCommand = new RelayCommand(o =>
            {
                if (Answers.Count == 1) return;
                Answers.Remove(o as EditableTextViewModel);
            });
            SaveCommand = new RelayCommand(o => CreateAsync());

            GetBackgrounds();
        }

        private async void GetBackgrounds()
        {
            await Task.Delay(1);
            PollBackgrounds.Add(new PollBackgroundViewModel(new PollBackground() { Type = PollBackgroundType.Unknown }));
            SelectedPollBackground = PollBackgrounds.First();
            List<PollBackground> bkgs = await VKSession.Current.API.Polls.GetBackgroundsAsync();
            bkgs.ForEach(b => PollBackgrounds.Add(new PollBackgroundViewModel(b, Windows.UI.Xaml.ElementTheme.Dark)));
        }

        private async void CreateAsync()
        {
            if (IsCreating) return;

            InvalidQuestionInfoVisible = false;
            InvalidAnswersInfoVisible = false;
            InvalidExpDateInfoVisible = false;

            List<string> answers = (from a in Answers where !String.IsNullOrEmpty(a.Text) select a.Text).ToList();
            string ans = String.Join(",", answers);

            DateTimeOffset dt = LimitDate.Date.Date;
            dt = dt.AddTicks(LimitTime.Ticks);
            long limitunixtime = IsVotingTimeLimited ? dt.ToUnixTimeSeconds() : 0;
            long currentunixtime = DateTimeOffset.Now.ToUnixTimeSeconds();
            long maxlimitunixtime = currentunixtime + 2592000; // 2 592 000 = 30 days in unixtime

            bool ok = true;
            if (String.IsNullOrEmpty(Question))
            {
                ok = false;
                InvalidQuestionInfoVisible = true;
            }

            if (String.IsNullOrEmpty(ans))
            {
                ok = false;
                InvalidAnswersInfoVisible = true;
            }

            if (IsVotingTimeLimited)
            {
                if (limitunixtime < currentunixtime || limitunixtime > maxlimitunixtime)
                {
                    ok = false;
                    InvalidExpDateInfoVisible = true;
                }
            }

            if (!ok) return;
            IsCreating = true;

            try
            {
                int sid = VKSession.Current.SessionId;
                Poll p = await VKSession.Current.API.Polls.CreateAsync(Question, answers,
                    IsAnonymous, IsMultipleSelectionEnabled, UnvotingDisabled,
                    limitunixtime, SelectedPollBackground.Id, VKSession.Current.SessionId);
                View.Hide(p);
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync(ex);
                IsCreating = false;
            }
        }
    }
}
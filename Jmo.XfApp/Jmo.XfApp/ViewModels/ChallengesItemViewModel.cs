using Jmo.Common.Helpers;
using Jmo.Common.Models;
using Jmo.XfApp.Views;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Navigation;

namespace Jmo.XfApp.ViewModels
{
    public class ChallengesItemViewModel : QuestionResponse
    {
        private readonly INavigationService _navigationService;
        private DelegateCommand _selectQuestionCommand;

        public ChallengesItemViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public DelegateCommand SelectQuestionCommand => _selectQuestionCommand ?? (_selectQuestionCommand = new DelegateCommand(SelectQuestionAsync));

        private async void SelectQuestionAsync()
        {
            var parameters = new NavigationParameters
            {
                { "question", this }
            };
            Settings.Question = JsonConvert.SerializeObject(this);
            await _navigationService.NavigateAsync(nameof(ChallengesPage), parameters);
        }
    }

}

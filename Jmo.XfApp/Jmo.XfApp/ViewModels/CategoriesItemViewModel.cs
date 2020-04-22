using Jmo.Common.Models;
using Jmo.XfApp.Views;
using Prism.Commands;
using Prism.Navigation;

namespace Jmo.XfApp.ViewModels
{
    public class CategoriesItemViewModel : CategoryResponse
    {
        private readonly INavigationService _navigationService;
        private DelegateCommand _selectCategoryCommand;

        public CategoriesItemViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public DelegateCommand SelectCategoryCommand => _selectCategoryCommand ?? (_selectCategoryCommand = new DelegateCommand(SelectCategoryAsync));

        private async void SelectCategoryAsync()
        {
            var parameters = new NavigationParameters
            {
                { "category", this }
            };

            await _navigationService.NavigateAsync(nameof(ChallengesPage), parameters);
        }
    }

}

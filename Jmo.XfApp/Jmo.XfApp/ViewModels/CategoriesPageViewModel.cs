using Jmo.Common.Models;
using Jmo.Common.Services;
using Prism.Navigation;
using System.Collections.Generic;
using System.Linq;

namespace Jmo.XfApp.ViewModels
{
    public class CategoriesPageViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IApiService _apiService;
        private List<CategoriesItemViewModel> _categories;

        public CategoriesPageViewModel(
            INavigationService navigationService,
            IApiService apiService) : base(navigationService)
        {
            _navigationService = navigationService;
            _apiService = apiService;
            Title = "Brain Games Challenges";
            LoadCategoriesAsync();
        }

        public List<CategoriesItemViewModel> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private async void LoadCategoriesAsync()
        {
            string url = App.Current.Resources["UrlAPI"].ToString();
            Response response = await _apiService.GetListAsync<CategoryResponse>(
                url,
                "/api",
                "/Categories");

            if (!response.IsSuccess)
            {
                await App.Current.MainPage.DisplayAlert(
                    "Error",
                    response.Message,
                    "Accept");
                return;
            }

            var categories = (List<CategoryResponse>)response.Result;

            Categories = categories
                .Select(t => new CategoriesItemViewModel(_navigationService)
                {
                    Id = t.Id,
                    ImageFullPath = t.ImageFullPath,
                    Name = t.Name
                })
                .ToList();

        }
    }
}

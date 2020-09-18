using Jmo.Common.Helpers;
using Jmo.Common.Models;
using Jmo.Common.Services;
using Newtonsoft.Json;
using Prism.Navigation;
using System.Collections.Generic;
using System.Linq;

namespace Jmo.XfApp.ViewModels
{
    public class ChallengesPageViewModel : ViewModelBase
    {

        private CategoryResponse _category;

        private readonly INavigationService _navigationService;
        private readonly IApiService _apiService;
        private List<ChallengesItemViewModel> _questions;

        public ChallengesPageViewModel(INavigationService navigationService , IApiService apiService) : base(navigationService)
        {
            _navigationService = navigationService;
            _apiService = apiService;
            Title = "Challenges";
            LoadQuestionsAsync();
        }

        public CategoryResponse Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public List<ChallengesItemViewModel> Questions
        {
            get => _questions;
            set => SetProperty(ref _questions, value);
        }

        private async void LoadQuestionsAsync()
        {

            //string url = App.Current.Resources["UrlAPI"].ToString();
            //Response response = await _apiService.GetListAsync<QuestionResponse>(
            //    url,
            //    "/api",
            //    "/Questions", _category.Id);

            //if (!response.IsSuccess)
            //{
            //    await App.Current.MainPage.DisplayAlert(
            //        "Error",
            //        response.Message,
            //        "Accept");
            //    return;
            //}
            _category = JsonConvert.DeserializeObject<CategoryResponse>(Settings.Question);
            //  var questions = (List<QuestionResponse>)response.Result;
            var questions = new List<QuestionResponse>();
            questions.AddRange(_category.Questions);
            //foreach (var item in _category.Questions)
            //{
            //    questions.Add(item);
            //}
            Questions = questions
                .Select(t => new ChallengesItemViewModel(_navigationService)
                {
                    Id = t.Id,
                    ImageFullPath = t.ImageFullPath,
                    Questionant = t.Questionant,
                    CategoryId= t.CategoryId,
                    Choises = t.Choises
                })
                .ToList();

        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            if (parameters.ContainsKey("category"))
            {
                Category = parameters.GetValue<CategoryResponse>("category");
                Title = Category.Name;
            }
        }


    }
}

using System;
using pBrainTrain.App.Models;
using pBrainTrain.App.Services;
using pBrainTrain.App.ViewModels;

namespace pBrainTrain.App
{

    using Views;
    using Xamarin.Forms;

    public partial class App : Application
    {
        #region Attributes
        private readonly DataService dataService;
        #endregion
        public App()
        {
            InitializeComponent();
            dataService=new DataService();

            LoadParameters();

            var user = dataService.First<User>(false);

            if (user != null && user.IsRemembered && user.TokenExpires > DateTime.Now)
            {
                var mainViewModel = MainViewModel.GetInstance();
                mainViewModel.CurrentUser = user;
               mainViewModel.RegisterDevice();
                // MainPage = new MasterPage();
                this.MainPage = new NavigationPage(new MainPage());
            }
            else
            {
                this.MainPage = new NavigationPage(new LoginPage());
            }

             // new pBrainTrain.App.MainPage();
        }

        private void LoadParameters()
        {
            var urlBase = Current.Resources["UrlBase"].ToString();

            var parameters = dataService.First<Parameter>(false);
            if (parameters == null)
            {
                parameters = new Parameter
                {
                    UrlBase = urlBase,
                };

                dataService.Insert(parameters);
            }
            else
            {
                parameters.UrlBase = urlBase;
                dataService.Update(parameters);
            }
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}

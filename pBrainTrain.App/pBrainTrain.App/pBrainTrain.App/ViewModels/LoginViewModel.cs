using pBrainTrain.App.Models;

namespace pBrainTrain.App.ViewModels
{
    using Helpers;
    using System.Windows.Input;
    using GalaSoft.MvvmLight.Command;
    using Services;
    using Xamarin.Forms;

    public class LoginViewModel : BaseViewModel
    {
        #region Services
        private ApiService apiService;
        private DialogService dialogService;
        private DataService dataService;
        #endregion

        #region Attributes
        private string email;
        private string password;
        private bool isRunning;
        private bool isEnabled;
        #endregion

        #region Constructors
        public LoginViewModel()
        {
            this.apiService = new ApiService();
            this.dialogService = new DialogService();
            this.dataService = new DataService();
            this.IsRemembered = true;
            this.IsEnabled = true;

            this.Email = "sgermosen@outlook.com";
            this.Password = "123456";
        }
        #endregion

        #region Properties

        public string Email
        {
            get { return this.email; }
            set { SetValue(ref this.email, value); }
        }

        public string Password
        {
            get { return this.password; }
            set { SetValue(ref this.password, value); }
        }

        public bool IsRunning
        {
            get { return this.isRunning; }
            set { SetValue(ref this.isRunning, value); }
        }

        public bool IsRemembered { get; set; }

        public bool IsEnabled
        {
            get { return this.isEnabled; }
            set { SetValue(ref this.isEnabled, value); }
        }


        #endregion

        #region Commands

        public ICommand LoginCommand => new RelayCommand(Login);

        private async void Login()
        {
            if (string.IsNullOrEmpty(this.Email))
            {
                //await Application.Current.MainPage.DisplayAlert(
                //    Languages.Error,
                //    Languages.EmailValidation,
                //    Languages.Accept);
                await dialogService.ShowMessage(Languages.Error,
                      Languages.EmailValidation);
                return;
            }

            if (string.IsNullOrEmpty(this.Password))
            {
                //await Application.Current.MainPage.DisplayAlert(
                //    Languages.Error,
                //    Languages.PasswordValidation,
                //    Languages.Accept);
                await dialogService.ShowMessage(Languages.Error,
                    Languages.PasswordValidation);
                return;
            }
            this.IsRunning = true;
            this.IsEnabled = false;

            var connection = await this.apiService.CheckConnection();

            if (!connection.IsSuccess)
            {
                this.IsRunning = false;
                this.IsEnabled = true;
                //await Application.Current.MainPage.DisplayAlert(
                //    Languages.Error,
                //    connection.Message,
                //    Languages.Accept);
                await dialogService.ShowMessage(Languages.Error,
                    connection.Message);
                return;
            }
            var parameters = dataService.First<Parameter>(false);

            var token = await this.apiService.GetToken(parameters.UrlBase, this.Email, this.Password);

            if (token == null)
            {
                this.IsRunning = false;
                this.IsEnabled = true;
                //await Application.Current.MainPage.DisplayAlert(
                //    Languages.Error,
                //    Languages.SomethingWrong,
                //    Languages.Accept);
                await dialogService.ShowMessage(Languages.Error,
                    Languages.SomethingWrong);
                return;

            }

            if (string.IsNullOrEmpty(token.AccessToken))
            {
                this.IsRunning = false;
                this.IsEnabled = true;
                await dialogService.ShowMessage(Languages.Error,
                    token.ErrorDescription);

                return;
            }

            var response = await apiService.GetUserByEmail(
                parameters.UrlBase,
                "/api",
                "/Users/GetUserByEmail",
                token.TokenType,
                token.AccessToken,
                token.UserName);

            if (!response.IsSuccess)
            {
                IsRunning = false;
                IsEnabled = true;
                await dialogService.ShowMessage(Languages.Error,
                    response.Message);
                return;
            }
            var user = (User)response.Result;

            user.AccessToken = token.AccessToken;
            user.TokenType = token.TokenType;
            user.TokenExpires = token.Expires;
            user.IsRemembered = IsRemembered;
            user.Password = Password;
            dataService.DeleteAllAndInsert(user.UserType);
            dataService.DeleteAllAndInsert(user);



            var mainViewModel = MainViewModel.GetInstance();
            mainViewModel.Token = token;
            mainViewModel.CurrentUser = user;
            mainViewModel.SetCurrentUser(user);

            //Todo: set new instance of the ViewModel from my mainPage


            this.IsRunning = false;
            this.IsEnabled = true;

            this.Email = string.Empty;
            this.Password = string.Empty;

            mainViewModel.RegisterDevice();

            await Application.Current.MainPage.Navigation.PushAsync(new MainPage());
        }


        #endregion


    }
}

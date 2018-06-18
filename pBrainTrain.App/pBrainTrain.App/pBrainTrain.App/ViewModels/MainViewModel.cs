using pBrainTrain.App.Models;

namespace pBrainTrain.App.ViewModels
{
    public class MainViewModel
    {
        #region Singleton       

        private static MainViewModel instance;

        public static MainViewModel GetInstance()
        {
            return instance ?? new MainViewModel();
            //if (instance == null)
            //{
            //    return new MainViewModel();
            //}

            //return instance;
        }


        #endregion

        #region Constructors
        public MainViewModel()
        {
            instance = this;
            this.Login = new LoginViewModel();

        }
        #endregion

        #region Properties

        public TokenResponse Token { get; set; }


        #endregion

        #region ViewModels

        public LoginViewModel Login { get; set; }

        #endregion
    }
}

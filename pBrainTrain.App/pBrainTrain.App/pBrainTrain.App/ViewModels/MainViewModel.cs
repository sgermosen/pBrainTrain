using System.ComponentModel;
using pBrainTrain.App.Interfaces;
using pBrainTrain.App.Models;
using Xamarin.Forms;

namespace pBrainTrain.App.ViewModels
{
    public class MainViewModel:INotifyPropertyChanged
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

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Attributes

        private User currentUser;

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

        public User CurrentUser
        {
            set {
                if (currentUser == value) return;
                currentUser = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentUser"));
            }
            get {
                return currentUser;
            }
        }

        #endregion

        #region ViewModels

        public LoginViewModel Login { get; set; }

        #endregion

        #region Methots

        public void SetCurrentUser(User user)
        {
            CurrentUser = user;
        }
        public void RegisterDevice()
        {
            var register = DependencyService.Get<IRegisterDevice>();
            register.RegisterDevice();
        }

        #endregion
    }
}

namespace pBrainTrain.App.Helpers
{
    using Interfaces;
    using Xamarin.Forms;
    using Resources;

    public static class Languages
    {
        static Languages()
        {
            var ci = DependencyService.Get<ILocalize>().GetCurrentCultureInfo();
            Resource.Culture = ci;
            DependencyService.Get<ILocalize>().SetLocale(ci);

        }

        public static string Accept => Resource.Accept;
        public static string EmailValidation => Resource.EmailValidation;

        public static string Error => Resource.Error;

        public static string EmailPlaceHolder => Resource.EmailPlaceHolder;

        public static string Search => Resource.Search;

        public static string Rememberme => Resource.Rememberme;

        public static string PasswordValidation => Resource.PasswordValidation;

        public static string SomethingWrong => Resource.SomethingWrong;

        public static string Login => Resource.Login;

        public static string EMail => Resource.EMail;

        public static string Password => Resource.Password;

        public static string PasswordPlaceHolder => Resource.PasswordPlaceHolder;

        public static string Forgot => Resource.Forgot;

        public static string Register => Resource.Register;
        public static string Yes => Resource.Yes;
        public static string No => Resource.No;

        //public static string EmailValidation
        //{
        //    get { return Resource.EmailValidation; }
        //}

        //public static string Error
        //{
        //    get { return Resource.Error; }
        //}

        //public static string EmailPlaceHolder
        //{
        //    get { return Resource.EmailPlaceHolder; }
        //}
    }
}

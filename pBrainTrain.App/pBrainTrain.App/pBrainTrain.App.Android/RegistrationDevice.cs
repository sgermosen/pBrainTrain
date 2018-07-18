using Xamarin.Forms;

[assembly: Dependency(typeof(pBrainTrain.App.Droid.RegistrationDevice))]
namespace pBrainTrain.App.Droid
{
  //  using Gcm.Client;
    using Xamarin.Forms;
    using Android.Util;
    using Interfaces;

    public class RegistrationDevice : IRegisterDevice
    {
        #region Methods
        public void RegisterDevice()
        {
            var mainActivity = MainActivity.GetInstance();
            //GcmClient.CheckDevice(mainActivity);
            //GcmClient.CheckManifest(mainActivity);

            Log.Info("MainActivity", "Registering...");
          //  GcmClient.Register(mainActivity, Constants.SenderId);
        }
        #endregion
    }
}
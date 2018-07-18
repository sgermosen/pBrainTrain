
using Xamarin.Forms;

[assembly: Dependency(typeof(pBrainTrain.App.iOS.Config))]
namespace pBrainTrain.App.iOS
{
    using System;
    using SQLite.Net.Interop;
    using Interfaces;
    public class Config : IConfig
    {
        private string _directoryDb;
        private ISQLitePlatform _platform;

        public string DirectoryDb
        {
            get {
                if (!string.IsNullOrEmpty(_directoryDb)) return _directoryDb;
                var directory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                _directoryDb = System.IO.Path.Combine(directory, "..", "Library");

                return _directoryDb;
            }
        }

        public ISQLitePlatform Platform => _platform ?? (_platform = new SQLite.Net.Platform.XamarinIOS.SQLitePlatformIOS());
    }
}

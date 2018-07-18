using pBrainTrain.App.Helpers;

namespace pBrainTrain.App.Services
{
    using System.Threading.Tasks;

    public class DialogService
    {
        public async Task ShowMessage(string title, string message)
        {
            await App.Current.MainPage.DisplayAlert(title, message, Languages.Accept);
        }

        public async Task<bool> ShowConfirm(string title, string message)
        {
            return await App.Current.MainPage.DisplayAlert(title, message, Languages.Yes, Languages.No);
        }
    }
}

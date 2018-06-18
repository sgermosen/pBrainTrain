namespace pBrainTrain.App.Interfaces
{
    using System.Globalization;
    public interface ILocalize
    {
        CultureInfo GetCurrentCultureInfo();

        void SetLocale(CultureInfo ci);
    }
}

namespace pBrainTrain.App.Interfaces
{
    using SQLite.Net.Interop;
    public interface IConfig
    {
        string DirectoryDb { get; }

        ISQLitePlatform Platform { get; }
    }
}

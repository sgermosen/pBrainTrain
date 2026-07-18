using BrainTrain.App.Core;
using Plugin.Maui.Audio;

namespace BrainTrain.App.Services;

/// <summary>
/// Reproduce los loops y la campana de la sección Enfoque desde los assets
/// locales (Resources/Raw/focus). Los WAV están diseñados como loops perfectos.
/// </summary>
public sealed class FocusAudioPlayer(IAudioManager audioManager) : IFocusAudioPlayer, IDisposable
{
    private IAudioPlayer? _loop;

    public async Task StartLoopAsync(string assetName)
    {
        await StopAsync();
        var stream = await FileSystem.OpenAppPackageFileAsync(assetName);
        _loop = audioManager.CreatePlayer(stream);
        _loop.Loop = true;
        _loop.Volume = 0.85;
        _loop.Play();
    }

    public Task StopAsync()
    {
        if (_loop is not null)
        {
            _loop.Stop();
            _loop.Dispose();
            _loop = null;
        }
        return Task.CompletedTask;
    }

    public async Task PlayChimeAsync()
    {
        var stream = await FileSystem.OpenAppPackageFileAsync("focus/chime.wav");
        var chime = audioManager.CreatePlayer(stream);
        chime.PlaybackEnded += (_, _) => chime.Dispose();
        chime.Play();
    }

    public void Dispose() => _ = StopAsync();
}

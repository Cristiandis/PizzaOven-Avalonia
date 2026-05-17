using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace PizzaOven;

public static class PLUSMUSIC
{
    private static bool _initializing;
    private static MiniAudioEngine? _engine;
    private static AudioPlaybackDevice? _outputDevice;
    private static SoundPlayer? _bgmPlayer;
    private static FileStream? _loopStream;
    private static FileSystemWatcher? _bgMusicWatcher;

    public static bool UnfocusedMuteEnabled = true;
    public static bool MuteEnabled = true;
    public static string MusicFolder = "Default";

    private static SoundPlayer? _tutorialPlayer;
    private static Stream? _tutorialStream;
    private static bool _tutorialInitializing;

    public static void InitializeEngine()
    {
        if (_engine != null) return;
        _engine = new MiniAudioEngine();
        _outputDevice = _engine.InitializePlaybackDevice(null, AudioFormat.DvdHq);
        _outputDevice.Start();

        UnfocusedMuteEnabled = PLUSSavesystem.read_ini_bool("Audio", "UnfocusedMute", true);
        MuteEnabled = PLUSSavesystem.read_ini_bool("Audio", "Mute", true);
        MusicFolder = PLUSSavesystem.read_ini("Audio", "MusicFolder", "Default");
    }

    public static async Task InitializeAsync()
    {
        if (_initializing) return;
        _initializing = true;

        try
        {
            if (Global.ronnietutorial) return;

            MusicFolder = PLUSSavesystem.read_ini("Audio", "MusicFolder", "Default");

            Stop();
            InitializeEngine();

            var path = Path.Combine(Global.customassetsfolder, "Music", MusicFolder);
            var startFile = Path.Combine(path, "BGMusic_Start.mp3");
            var loopFile = Path.Combine(path, "BGMusic_Loop.mp3");

            if (File.Exists(startFile))
            {
                var tcs = new TaskCompletionSource<bool>();
                using var fs = new FileStream(startFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var provider = new StreamDataProvider(_engine!, fs);

                _bgmPlayer = new SoundPlayer(_engine!, _outputDevice!.Format, provider) { Volume = GetTargetVolume() };
                _bgmPlayer.PlaybackEnded += (s, e) => tcs.TrySetResult(true);

                _outputDevice.MasterMixer.AddComponent(_bgmPlayer);
                _bgmPlayer.Play();

                await Task.WhenAny(tcs.Task, Task.Delay(30000));

                _bgmPlayer.Stop();
                _outputDevice.MasterMixer.RemoveComponent(_bgmPlayer);
                _bgmPlayer.Dispose();
                _bgmPlayer = null;
            }

            if (File.Exists(loopFile))
            {
                _loopStream = new FileStream(loopFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var provider = new StreamDataProvider(_engine!, _loopStream);

                _bgmPlayer = new SoundPlayer(_engine!, _outputDevice!.Format, provider)
                {
                    IsLooping = true,
                    Volume = GetTargetVolume()
                };

                _outputDevice.MasterMixer.AddComponent(_bgmPlayer);
                _bgmPlayer.Play();
            }
        }
        finally
        {
            _initializing = false;
        }
    }

    public static async Task Play_TutorialMusic()
    {
        try
        {
            Stop_TutorialMusic();

            var audioUri = "avares://PizzaOven/OvenRonnie/TutorialMusic.wav";

            var resource = AssetLoader.Open(new Uri(audioUri));
            if (resource == null) return;

            _tutorialStream = new MemoryStream();
            await resource.CopyToAsync(_tutorialStream);
            _tutorialStream.Position = 0;
            resource.Dispose();

            InitializeEngine();

            var provider = new StreamDataProvider(_engine!, _tutorialStream);
            _tutorialPlayer = new SoundPlayer(_engine!, _outputDevice!.Format, provider)
            {
                IsLooping = true,
                Volume = float.TryParse(PLUSSavesystem.read_ini("Audio", "SoundVolume", "100"), out var v)
                    ? v / 100f
                    : 1f
            };

            _outputDevice.MasterMixer.AddComponent(_tutorialPlayer);
            _tutorialPlayer.Play();
        }
        catch (Exception ex)
        {
        }
    }

    public static void Stop_TutorialMusic()
    {
        if (_tutorialPlayer != null)
        {
            _tutorialPlayer.Stop();
            _outputDevice?.MasterMixer.RemoveComponent(_tutorialPlayer);
            _tutorialPlayer.Dispose();
            _tutorialPlayer = null;
        }

        _tutorialStream?.Dispose();
        _tutorialStream = null;
    }

    public static async Task FadeOutTutorialMusic(float durationSeconds = 2.0f)
    {
        if (_tutorialPlayer == null) return;

        var startVolume = _tutorialPlayer.Volume;
        var steps = 20;
        var stepMs = (int)(durationSeconds * 1000 / steps);

        for (var i = 0; i < steps; i++)
        {
            _tutorialPlayer.Volume = startVolume * (1.0f - (float)i / steps);
            await Task.Delay(stepMs);
        }

        Stop_TutorialMusic();
    }

    public static void Pause_TutorialMusic()
    {
        if (_tutorialPlayer == null) return;
        if (_tutorialPlayer.State == PlaybackState.Playing)
            _tutorialPlayer.Pause();
        else if (_tutorialPlayer.State == PlaybackState.Paused)
            _tutorialPlayer.Play();
    }

    public static void SetTutorialMusicPaused(bool paused)
    {
        if (_tutorialPlayer == null) return;
        if (paused && _tutorialPlayer.State == PlaybackState.Playing)
            _tutorialPlayer.Pause();
        else if (!paused && _tutorialPlayer.State == PlaybackState.Paused)
            _tutorialPlayer.Play();
    }

    public static void Stop()
    {
        if (_bgmPlayer != null)
        {
            _bgmPlayer.Stop();
            _outputDevice?.MasterMixer.RemoveComponent(_bgmPlayer);
            _bgmPlayer.Dispose();
            _bgmPlayer = null;
        }

        if (_tutorialPlayer != null)
        {
            _tutorialPlayer.Stop();
            _outputDevice?.MasterMixer.RemoveComponent(_tutorialPlayer);
            _tutorialPlayer.Dispose();
            _tutorialPlayer = null;
        }

        _outputDevice?.MasterMixer.Components.ToList()
            .ForEach(c => _outputDevice.MasterMixer.RemoveComponent(c));
        _loopStream?.Dispose();
        _loopStream = null;
        _tutorialStream?.Dispose();
        _tutorialStream = null;
    }

    public static void StartMusicWatcher()
    {
        var path = Path.Combine(Global.customassetsfolder, "Music");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        _bgMusicWatcher?.Dispose();
        _bgMusicWatcher = new FileSystemWatcher(path, "*.mp3")
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = true
        };
        _bgMusicWatcher.Changed += (s, e) =>
        {
            if (_initializing) return;
            Task.Run(() => InitializeAsync());
        };
    }

    public static void ApplyCurrentVolume(bool appIsActive)
    {
        var vol = UnfocusedMuteEnabled && !appIsActive ? 0f : GetTargetVolume();
        if (_bgmPlayer != null) _bgmPlayer.Volume = vol;
    }

    private static float GetTargetVolume()
    {
        if (MuteEnabled) return 0f;
        return float.TryParse(PLUSSavesystem.read_ini("Audio", "SoundVolume", "100"), out var v)
            ? v / 100f
            : 1f;
    }

    public static void PlayJingle()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                InitializeEngine();

                var audioUri = "avares://PizzaOven/OvenRonnie/RonnieJingle.wav";

                var resource = AssetLoader.Open(new Uri(audioUri));
                if (resource == null) return;

                var ms = new MemoryStream();
                await resource.CopyToAsync(ms);
                ms.Position = 0;
                resource.Dispose();

                var provider = new StreamDataProvider(_engine!, ms);
                var player = new SoundPlayer(_engine!, _outputDevice!.Format, provider)
                {
                    Volume = float.TryParse(PLUSSavesystem.read_ini("Audio", "SoundVolume", "100"), out var v)
                        ? v / 100f
                        : 1f
                };

                _outputDevice!.MasterMixer.AddComponent(player);
                player.Play();

                await Task.Delay(5000);

                player.Stop();
                _outputDevice.MasterMixer.RemoveComponent(player);
                player.Dispose();
                provider.Dispose();
                ms.Dispose();
            }
            catch (Exception ex)
            {
            }
        });
    }

    public static void Shutdown()
    {
        _bgMusicWatcher?.Dispose();
        Stop();
        _outputDevice?.Dispose();
        _engine?.Dispose();
    }
}
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using DiscordRPC;
using DiscordRPC.Logging;

namespace PizzaOven;

public static class POPRESENCE
{
    private static DiscordRpcClient? _client;

    public static void Initialize()
    {
        try
        {
            using var stream = Assembly.GetEntryAssembly()!
                .GetManifestResourceStream("PizzaOven.POSECRETS.json")!;
            using var reader = new StreamReader(stream);
            var appId = JsonSerializer
                .Deserialize<JsonElement>(reader.ReadToEnd())
                .GetProperty("discord_appid")
                .GetString();
            
            _client = new DiscordRpcClient(appId)
            {
                Logger = new ConsoleLogger { Level = LogLevel.Warning }
            };
            _client.Initialize();
            _client.SetPresence(new RichPresence
            {
                Details = "Managing Pizza Tower mods",
                State = "Pizza Oven Mod Manager",
                Assets = new Assets
                {
                    LargeImageKey = "pizzaoven",
                    LargeImageText = "Pizza Oven"
                },
                Timestamps = Timestamps.Now
            });
        }
        catch (Exception e)
        {
            Global.logger.WriteLine($"Discord RPC failed to initialize: {e.Message}", LoggerType.Warning);
        }
    }

    public static void Shutdown()
    {
        try
        {
            _client?.ClearPresence();
            _client?.Deinitialize();
            _client?.Dispose();
            _client = null;
        }
        catch { }
    }
}
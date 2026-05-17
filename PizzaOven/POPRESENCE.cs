using System;
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
            var appId = "1503088062647898222";
            
            _client = new DiscordRpcClient(appId)
            {
                Logger = new ConsoleLogger { Level = LogLevel.Warning }
            };
            _client.Initialize();
            _client.SetPresence(new RichPresence
            {
                Details = "PizzaOven but More",
                State = "Tool by SurfyCrescent97",
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
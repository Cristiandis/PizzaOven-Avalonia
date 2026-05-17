using System;
using System.Threading.Tasks;

namespace PizzaOven;

public class PLUSWait
{
    public static async Task WaitUntil(Func<bool> condition, int checkDelayMs = 16)
    {
        while (!condition())
            await Task.Delay(checkDelayMs);
    }

    public static async Task WaitSeconds(double seconds)
    {
        var ms = (int)(seconds * 1000);
        await Task.Delay(ms);
    }

    public static async Task<bool> WaitUntilOrTimeout(Func<bool> condition, double timeoutSeconds,
        int checkDelayMs = 16)
    {
        var elapsedMs = 0;
        var timeoutMs = (int)(timeoutSeconds * 1000);

        while (!condition())
        {
            if (elapsedMs >= timeoutMs)
                return false;

            await Task.Delay(checkDelayMs);
            elapsedMs += checkDelayMs;
        }

        return true;
    }
}
using System;
using System.Threading.Tasks;

namespace Soulstone.Utils;

internal static class FrameworkDispatcher
{
    public static Task RunAsync(Action action)
    {
        if (Plugin.Framework != null)
            return Plugin.Framework.RunOnFrameworkThread(action);
        action();
        return Task.CompletedTask;
    }
}

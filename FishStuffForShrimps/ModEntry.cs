using System.Diagnostics;
using FishStuffForShrimps.Features;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace FishStuffForShrimps;

public sealed partial class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif

    public const string ModId = "mushymato.FishStuffForShrimps";
    private static IMonitor mon = null!;
    internal static ModConfig config = null!;
    internal static readonly Harmony harmony = new(ModId);

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        mon = Monitor;
        config = helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        BobberBarFishIcon.Toggle();
        GuarenteedSpecificBait.Toggle();
        OnlyFishConsumesBait.Toggle();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        config.Register(Helper, ModManifest);
    }

    /// <summary>SMAPI static monitor Log wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon.Log(msg, level);
    }

    /// <summary>SMAPI static monitor LogOnce wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon.LogOnce(msg, level);
    }

    /// <summary>SMAPI static monitor Log wrapper, debug only</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    [Conditional("DEBUG")]
    internal static void LogDebug(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon.Log(msg, level);
    }
}

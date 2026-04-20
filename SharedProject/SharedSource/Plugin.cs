using Barotrauma.LuaCs.Compatibility;
using Barotrauma.LuaCs.Events;
using DSSIFactionCraft.Items.Components;
using HarmonyLib;
using MonoMod.Core.Utils;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft;

public partial class Plugin : IAssemblyPlugin
{
    // These are automatically assigned by the plugin service after the Constructor is called
#pragma warning disable CS8618
    public IConfigService ConfigService { get; set; }
    public IPluginManagementService PluginManagementService { get; set; }
    public ILoggerService _loggerService { get; set; }
    public IConsoleCommandsService ConsoleCommandsService { get; set; }
    public ISafeLuaUserDataService LuaUserDataService { get; set; }
    public IEventService _eventService { get; set; }
    public ILuaCsTimer _timerService { get; set; }
#pragma warning restore CS8618
    public static ILoggerService LoggerService = null!;
    public static IEventService EventService = null!;
    public static ILuaCsTimer TimerService = null!;

    public ContentPackage _package = null!;

    public Harmony? harmony;

    public void Initialize()
    {
        // When your plugin is loading, use this instead of the constructor for code relying on
        // the services above.
        LoggerService = _loggerService;
        EventService = _eventService;
        TimerService = _timerService;

        if (!PluginManagementService.TryGetPackageForPlugin<Plugin>(out _package))
        {
            _loggerService.LogError("Failed to find package!");
            return;
        }

        LoggerService.LogWarning("hehe");

        harmony = new("dfc");
        harmony.PatchAll();
    }

    public void OnLoadCompleted()
    {
        // After all plugins have loaded
        // Put code that interacts with other plugins here.
    }

    public void PreInitPatching()
    {
        // Called right after the constructor
    }

    public void Dispose()
    {
        // Cleanup your plugin!

        harmony?.UnpatchSelf();
        harmony = null;
    }
}
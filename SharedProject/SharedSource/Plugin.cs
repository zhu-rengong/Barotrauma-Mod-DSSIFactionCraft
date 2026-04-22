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
    //public IEventService _eventService { get; set; }
#pragma warning restore CS8618
    public static ILoggerService LoggerService = null!;
    public static IEventService EventService = null!;

    public ContentPackage _package = null!;

    public Harmony? harmony;

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void Initialize()
    {
        // When your plugin is loading, use this instead of the constructor for code relying on
        // the services above.
        LoggerService = _loggerService;
        EventService = LuaCsSetup.Instance.EventService;

        if (!PluginManagementService.TryGetPackageForPlugin<Plugin>(out _package))
        {
            _loggerService.LogError("Failed to find package!");
            return;
        }

        harmony = new("dfc");
        harmony.PatchAll();
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void OnLoadCompleted()
    {
        // After all plugins have loaded
        // Put code that interacts with other plugins here.
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PreInitPatching()
    {
        // Called right after the constructor
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void Dispose()
    {
        // Cleanup your plugin!

        harmony?.UnpatchSelf();
        harmony = null;
    }
}
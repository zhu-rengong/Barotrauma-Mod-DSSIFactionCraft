using MoonSharp.Interpreter;

namespace DSSIFactionCraft
{
    public partial class LuaAccessHelper
    {
        public const string FIELD_NAME_OVERRIDE_RESPAWN_MANAGER = "OverrideRespawnManager";

        public static DynValue OverrideRespawnManager => LuaCsSetup.Instance.Lua.Globals.RawGet("DFC", FIELD_NAME_OVERRIDE_RESPAWN_MANAGER);
        public static DynValue DeathTime => LuaCsSetup.Instance.Lua.Globals.RawGet("DFC", "Loaded", "_deathTime");
        public static DynValue AllowRespawn => LuaCsSetup.Instance.Lua.Globals.RawGet("DFC", "Loaded", "allowRespawn");
    }
}

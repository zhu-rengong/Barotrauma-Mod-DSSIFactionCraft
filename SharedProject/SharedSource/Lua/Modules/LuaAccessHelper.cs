using MoonSharp.Interpreter;

namespace DSSIFactionCraft
{
    public partial class LuaAccessHelper
    {
        public static DynValue Loaded => LuaCsSetup.Instance.Lua.Globals.RawGet("DFC", "Loaded");
        public static DynValue Factions => LuaCsSetup.Instance.Lua.Globals.RawGet("DFC", "Loaded", "factions");
        public static DynValue JoinedFaction => LuaCsSetup.Instance.Lua.Globals.RawGet("DFC", "Loaded", "_joinedFaction");
    }
}

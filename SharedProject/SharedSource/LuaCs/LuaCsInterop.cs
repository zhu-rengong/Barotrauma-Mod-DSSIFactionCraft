
using Barotrauma;
using Barotrauma.Networking;
using System;
using Barotrauma.Items.Components;
using HarmonyLib;
using MoonSharp.Interpreter;
using static Barotrauma.Networking.RespawnManager;
using System.Linq;

namespace DSSIFactionCraft.Networking
{
    public partial class LuaCsInterop
    {
        public static Closure? GetLocalizedText
        {
            get
            {
                if (LuaCsSetup.Instance.Lua.Globals.RawGet(["Lub", "Localization"]) is DynValue { Type: DataType.Table } l10n
                    && l10n.Table.MetaTable is Table metaTable
                    && metaTable.RawGet("__call") is DynValue { Type: DataType.Function } __call)
                {
                    return __call.Function;
                }
                return default;
            }
        }
    }
}

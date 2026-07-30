
using Barotrauma;
using Barotrauma.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using Barotrauma.Items.Components;
using HarmonyLib;
using MoonSharp.Interpreter;
using static Barotrauma.Networking.RespawnManager;
using System.Collections.Immutable;
using FarseerPhysics.Collision;
using Barotrauma.Extensions;

namespace DSSIFactionCraft.Networking
{
    public class DfcRespawnManager
    {
        public class FactionState
        {
            public readonly string Identifier;
            public DateTime RespawnTime;
            public bool RespawnCountdownStarted;

            public FactionState(string factionIdentifier)
            {
                Identifier = factionIdentifier;
            }

            public bool AllowRespawn => LuaAccessHelper.Factions is DynValue { Type: DataType.Table } factions
                && factions.Table.RawGet(Identifier) is DynValue { Type: DataType.Table } faction
                && faction.Table.RawGet("allowRespawn") is DynValue { Type: DataType.Boolean, Boolean: true };

            public float RespawnIntervalMultiplier => LuaAccessHelper.Factions is DynValue { Type: DataType.Table } factions
                && factions.Table.RawGet(Identifier) is DynValue { Type: DataType.Table } faction
                && faction.Table.RawGet("respawnIntervalMultiplier") is DynValue { Type: DataType.Number } respawnIntervalMultiplier
                    ? Convert.ToSingle(respawnIntervalMultiplier.Number)
                    : 1.0f;

            public Closure? GetRespawnLimitPerTime => LuaAccessHelper.Factions is DynValue { Type: DataType.Table } factions
                && factions.Table.RawGet(Identifier) is DynValue { Type: DataType.Table } faction
                && faction.Table.Get("getRespawnLimitPerTime") is DynValue { Type: DataType.Function } getRespawnLimitPerTime
                    ? getRespawnLimitPerTime.Function
                    : null;

            public int NumberOfParticipatorsByKeyEvenIfNil => LuaAccessHelper.Factions is DynValue { Type: DataType.Table } factions
                && factions.Table.RawGet(Identifier) is DynValue { Type: DataType.Table } faction
                && faction.Table.Get("tryGetParticipatorsByKeyEvenIfNil") is DynValue { Type: DataType.Function } tryGetParticipatorsByKeyEvenIfNil
                && tryGetParticipatorsByKeyEvenIfNil.Function.Call(faction) is DynValue { Type: DataType.Table } participators
                    ? participators.Table.Length
                    : -1;

            public Closure? OverrideStartRespawningMessage => LuaAccessHelper.Factions is DynValue { Type: DataType.Table } factions
                && factions.Table.RawGet(Identifier) is DynValue { Type: DataType.Table } faction
                && faction.Table.Get("overrideStartRespawningMessage") is DynValue { Type: DataType.Function } overrideStartRespawningMessage
                    ? overrideStartRespawningMessage.Function
                    : null;

            public Closure? OverrideRespawnedMessage => LuaAccessHelper.Factions is DynValue { Type: DataType.Table } factions
                && factions.Table.RawGet(Identifier) is DynValue { Type: DataType.Table } faction
                && faction.Table.Get("overrideRespawnedMessage") is DynValue { Type: DataType.Function } overrideRespawnedMessage
                    ? overrideRespawnedMessage.Function
                    : null;
        }

        public readonly Dictionary<string, FactionState> factionStates = new Dictionary<string, FactionState>();

        public readonly List<DynValue> respawnedClientAccountIds = new();

        public DfcRespawnManager() { }

        public void Update()
        {
            if (GameMain.GameSession is { RoundDuration: < 3.0f }) { return; }

            if (LuaAccessHelper.Factions is DynValue { Type: DataType.Table } factions)
            {
                foreach (var key in factions.Table.Keys)
                {
                    if (key is DynValue { Type: DataType.String })
                    {
                        string identifier = key.String;
                        if (!factionStates.ContainsKey(identifier))
                        {
                            factionStates.Add(identifier, new FactionState(identifier));
                        }
                    }
                }
            }

            if (factionStates.Any()
                && LuaAccessHelper.DeathTime.Table is Table deathTimeTable
                && LuaAccessHelper.JoinedFaction.Table is Table joinedFactionTable)
            {
                foreach (var factionState in factionStates.Values)
                {
                    if (!factionState.AllowRespawn) { continue; }

                    bool shouldStartCountdown = deathTimeTable.Pairs.Any(WaitForRespawn);

                    bool WaitForRespawn(TablePair deathTimePair)
                    {
                        return deathTimePair.Key is DynValue { Type: DataType.String } clientAccountId
                            //&& deathTimePair.Value is DynValue { Type: DataType.Number } deathTime
                            && joinedFactionTable.RawGet(clientAccountId.String) is DynValue { Type: DataType.Table } faction
                            && faction.Table.RawGet("identifier") is DynValue { Type: DataType.String } identifier
                            && identifier.String == factionState.Identifier;
                    }

                    if (factionState.RespawnCountdownStarted)
                    {
                        if (!shouldStartCountdown)
                        {
                            factionState.RespawnCountdownStarted = false;
                        }
                    }
                    else
                    {
                        if (shouldStartCountdown)
                        {
                            factionState.RespawnCountdownStarted = true;
                            factionState.RespawnTime = DateTime.Now + new TimeSpan(0, 0, 0, 0, (int)(GameMain.Server.ServerSettings.RespawnInterval * 1000.0f * factionState.RespawnIntervalMultiplier));

                            float timeLeft = MathF.Ceiling((float)(factionState.RespawnTime - DateTime.Now).TotalSeconds);

                            string? respawnText = null;

                            if (factionState.OverrideStartRespawningMessage is Closure overrideStartRespawningMessage)
                            {
                                respawnText = overrideStartRespawningMessage.Call(
                                    DynValue.NewString(factionState.Identifier), // the identifier of this faction
                                    DynValue.NewString(GetFactionDisplayName()), // the display name of this faction
                                    DynValue.NewNumber(timeLeft) // the time left in seconds until respawn
                                ) is DynValue { Type: DataType.String } result
                                    ? result.String
                                    : null;
                            }
                            else
                            {
                                respawnText = TextManager.GetWithVariables(
                                    "dfc.respawningin",
                                    ("[faction]", GetFactionDisplayName()),
                                    ("[time]", ToolBox.SecondsToReadableTime(timeLeft))
                                ).Value;
                            }

                            if (respawnText is not null)
                            {
                                GameMain.Server.SendChatMessage(respawnText, ChatMessageType.Server);
                            }
                        }
                    }

                    if (factionState.RespawnCountdownStarted && DateTime.Now > factionState.RespawnTime)
                    {
                        factionState.RespawnCountdownStarted = false;

                        bool respawned = false;
                        respawnedClientAccountIds.Clear();

                        void TryRespawn(TablePair deathTimePair)
                        {
                            if (WaitForRespawn(deathTimePair))
                            {
                                string clientAccountId = deathTimePair.Key.String;
                                deathTimeTable.Remove(clientAccountId);
                                respawnedClientAccountIds.Add(DynValue.NewString(clientAccountId));
                                respawned = true;
                            }
                        }

                        if (factionState.GetRespawnLimitPerTime is Closure getRespawnLimitPerTime
                            && getRespawnLimitPerTime.Call(
                                DynValue.NewString(factionState.Identifier), // the identifier of this faction
                                DynValue.NewNumber(deathTimeTable.Pairs.Count(WaitForRespawn)), // how many dead players in this faction
                                DynValue.NewNumber(factionState.NumberOfParticipatorsByKeyEvenIfNil) // how many players in this faction, even if they are not dead
                            ) is DynValue { Type: DataType.Number } respawnLimitPerTime
                            && Math.Truncate(respawnLimitPerTime.Number) == respawnLimitPerTime.Number
                            && Convert.ToInt32(respawnLimitPerTime.Number) is int limit
                            && limit > -1)
                        {
                            TablePair[] pairs = deathTimeTable.Pairs.ToArray();
                            Array.Sort(pairs, (p1, p2) =>
                            {
                                if (deathTimeTable.RawGet(p1.Value) is DynValue { Type: DataType.Number } dt1
                                    && deathTimeTable.RawGet(p2.Value) is DynValue { Type: DataType.Number } dt2)
                                {
                                    return dt1.Number.CompareTo(dt2.Number);
                                }

                                return 0;
                            });

                            pairs.Take(limit).ForEach(TryRespawn);
                        }
                        else
                        {
                            deathTimeTable.Pairs.ForEachMod(TryRespawn);
                        }

                        string? respawnText = null;

                        if (factionState.OverrideRespawnedMessage is Closure overrideRespawnedMessage)
                        {
                            respawnText = overrideRespawnedMessage.Call(
                                DynValue.NewString(factionState.Identifier), // the identifier of this faction
                                DynValue.NewString(GetFactionDisplayName()), // the display name of this faction
                                DynValue.NewTable(overrideRespawnedMessage.OwnerScript, respawnedClientAccountIds.ToArray()) // the account IDs of the clients that have respawned
                            ) is DynValue { Type: DataType.String } result
                                ? result.String
                                : null;
                        }
                        else if (respawned)
                        {
                            IEnumerable<string> clientNames = respawnedClientAccountIds
                                .Select(accountId =>
                                    GameMain.Server.ConnectedClients.FirstOrDefault(client =>
                                        client.AccountId.TryUnwrap(out var clientAccountId)
                                            && clientAccountId.StringRepresentation == accountId.String)
                                    is Client client
                                        ? NetworkMember.ClientLogName(client)
                                        : null)
                                .OfType<string>();

                            respawnText = TextManager.GetWithVariables("dfc.respawned",
                                ("[faction]", GetFactionDisplayName()),
                                ("[clientNames]", string.Join(',', clientNames))
                            ).Value;
                        }

                        if (respawnText is not null)
                        {
                            GameMain.Server.SendChatMessage(respawnText, ChatMessageType.Server);
                        }
                    }

                    string GetFactionDisplayName()
                    {
                        if (LuaCsInterop.GetLocalizedText is Closure getLocalizedText
                            && getLocalizedText.Call(
                                DynValue.Nil,
                                DynValue.NewTable(
                                    getLocalizedText.OwnerScript,
                                    DynValue.NewString("FactionDisplayName"),
                                    DynValue.NewString(factionState.Identifier)
                                )) is DynValue { Type: DataType.Table } result
                            && result.Table.RawGet("altvalue") is DynValue { Type: DataType.String } factionDisplayName)
                        {
                            return factionDisplayName.String;
                        }

                        return factionState.Identifier;
                    }
                }
            }
        }
    }
}

using Barotrauma;
using Barotrauma.Items.Components;
using Barotrauma.LuaCs.Events;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSSIFactionCraft.Items.Components
{
    internal partial class DfcScriptWifiInitializer : ItemComponent
    {
        public class EventItemCreated : IEventItemCreated
        {
            public void OnItemCreated(Item item)
            {
                wifiInitializerScripts.RemoveAll(script => script.item.Removed);
                if (!wifiInitializerScripts.Any()) { return; }
                EnqueueWifiInitialization(item);
            }
        }

        public class EventRoundStarted : IEventRoundStarted
        {
            public void OnRoundStart()
            {
                Item.ItemList.ForEach(EnqueueWifiInitialization);
            }
        }

        private static void EnqueueWifiInitialization(Item item)
        {
            if (item.GetComponent<WifiComponent>() is WifiComponent wifi)
            {
                wifiInitializerScripts.ForEach(script =>
                {
                    script.queueMessages.Enqueue(new(item, wifi));
                    script.IsActive = true;
                });
            }
        }

        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        [InGameEditable(MaxValueFloat = int.MaxValue, MinValueFloat = 1f, ValueStep = 1f, DecimalCount = 0), Serialize("1000,9999", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public Vector2 WifiChannelRange { get; set; }

        [InGameEditable, Serialize(false, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool AlwaysCalculateRandomChannel { get; set; } = true;

        public int? LastRandomChannel { get; set; }

        [InGameEditable, Serialize(CharacterTeamType.None, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public CharacterTeamType TeamID { get; set; }

        private string matcherPatternIncludes;
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string MatcherPatternIncludes
        {
            get => matcherPatternIncludes;
            set
            {
                matcherPatternIncludes = value;
                if (!IsMultiplayerClient) { UpdateMatcherPattern(); }
            }
        }

        private string matcherPatternExcludes;
        [InGameEditable, Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public string MatcherPatternExcludes
        {
            get => matcherPatternExcludes;
            set
            {
                matcherPatternExcludes = value;
                if (!IsMultiplayerClient) { UpdateMatcherPattern(); }
            }
        }

        public string[][] matcherIncludes = Array.Empty<string[]>();
        public string[][] matcherExcludes = Array.Empty<string[]>();

        public void UpdateMatcherPattern()
        {
            matcherIncludes = ItemUtils.ParseMatcherPattern(matcherPatternIncludes);
            matcherExcludes = ItemUtils.ParseMatcherPattern(matcherPatternExcludes);
        }

        private static List<DfcScriptWifiInitializer> wifiInitializerScripts;

        static DfcScriptWifiInitializer()
        {
            if (IsMultiplayerClient) { return; }

            wifiInitializerScripts = new();

            Plugin.EventService.Subscribe<IEventItemCreated>(new EventItemCreated());
            Plugin.EventService.Subscribe<IEventRoundStarted>(new EventRoundStarted());
        }

        readonly record struct EventMessage(Item SpawnedItem, WifiComponent Wifi);

        private readonly Queue<EventMessage> queueMessages = new();

        private IList<Item> regionIncludes;
        private IList<Item> regionExcludes;

        public DfcScriptWifiInitializer(Item item, ContentXElement element) : base(item, element)
        {
            IsActive = false;
            if (IsMultiplayerClient) { return; }

            wifiInitializerScripts.Add(this);
            UpdateMatcherPattern();
        }

        public override void Update(float deltaTime, Camera cam)
        {
            if (IsMultiplayerClient) { IsActive = false; return; }

            while (queueMessages.TryDequeue(out EventMessage message))
            {
                if (message.SpawnedItem.Removed) { continue; }
                regionIncludes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionincluded");
                regionExcludes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionexcluded");
                if (RegionUtils.Contains(message.SpawnedItem.WorldPosition, regionIncludes, regionExcludes)
                    && ItemUtils.Matches(message.SpawnedItem, matcherIncludes, matcherExcludes))
                {
                    if (AlwaysCalculateRandomChannel || !LastRandomChannel.HasValue)
                    {
                        int minChannel = Math.Min((int)WifiChannelRange.X, (int)WifiChannelRange.Y),
                            maxChannel = Math.Max((int)WifiChannelRange.X, (int)WifiChannelRange.Y);
                        int randomChannel = Rand.Range(minChannel, maxChannel, Rand.RandSync.Unsynced);
                        LastRandomChannel = randomChannel;
                    }

                    var sp_wificomponent_channel = message.Wifi.SerializableProperties["channel"];
                    sp_wificomponent_channel.SetValue(message.Wifi, LastRandomChannel.Value);
                    var sp_wificomponent_teamid = message.Wifi.SerializableProperties["teamid"];
                    sp_wificomponent_teamid.SetValue(message.Wifi, TeamID);
#if SERVER
                    GameMain.NetworkMember.CreateEntityEvent(message.SpawnedItem, new Item.ChangePropertyEventData(sp_wificomponent_channel, message.Wifi));
#endif
                }

            }
            IsActive = false;
        }
    }
}

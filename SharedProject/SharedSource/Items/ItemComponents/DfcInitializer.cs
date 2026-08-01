using System.Diagnostics;
using Barotrauma;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcInitializer : ItemComponent
    {
        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.", description: "Allows players to join during the round.")]
        public bool AllowMidRoundJoin { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.", description: "Allows players to respawn after being eliminated.")]
        public bool AllowRespawn { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.", description: "Auto-selects the only available option.")]
        public bool AutoParticipateWhenNoChoices { get; set; }

        [InGameEditable]
        [Serialize("", IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.",
            description: "How to decide the final selection mode. Available values are \"Manual\", \"Random\", \"ManualThenRandom\", \"Vote\" and fallback to mod settings if empty.")]
        public string SelectionModeDecideWay { get; set; }

        public DfcInitializer(Item item, ContentXElement element) : base(item, element) { }
    }
}
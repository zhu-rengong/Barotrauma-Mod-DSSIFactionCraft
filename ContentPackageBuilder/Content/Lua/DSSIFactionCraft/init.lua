if not (SERVER or Game.IsSingleplayer) then return end

---@string
local path = table.pack(...)[1]

local log = require "utilbelt.logger" ("DFC")
local l10n = require "utilbelt.l10n"

Hook.Add("loaded", function()
    DFC = {}

    DFC.Path = path

    ---@type Barotrauma.ContentPackage
    local packageName = "DSSIFactionCraft"

    do
        local result = { trygetpackage(packageName) }
        if result[1] then
            DFC.Package = result[2]
        else
            log(("Not found package named %q"):format(packageName), 'e')
        end
    end

    do
        local settingName = "SelectionModeDecideWay"
        local result = { ConfigService.TryGetConfig(SettingList.String, DFC.Package, settingName) }
        if result[1] then
            DFC.SelectionModeDecideWay = result[2]
        else
            log(("Not found setting named %q"):format(settingName), 'e')
        end
    end

    LuaUserData.RegisterType [[DSSIFactionCraft.Items.Components.DfcNewSpawnPointSet]]
    LuaUserData.RegisterType [[DSSIFactionCraft.Items.Components.DfcNewFaction]]
    LuaUserData.RegisterType [[DSSIFactionCraft.Items.Components.DfcNewJob]]
    LuaUserData.RegisterType [[DSSIFactionCraft.Items.Components.DfcNewGear]]
    DFC.Components = {
        DfcNewSpawnPointSet = LuaUserData.CreateStatic [[DSSIFactionCraft.Items.Components.DfcNewSpawnPointSet]],
        DfcNewFaction = LuaUserData.CreateStatic [[DSSIFactionCraft.Items.Components.DfcNewFaction]],
        DfcNewJob = LuaUserData.CreateStatic [[DSSIFactionCraft.Items.Components.DfcNewJob]],
        DfcNewGear = LuaUserData.CreateStatic [[DSSIFactionCraft.Items.Components.DfcNewGear]],
    }

    DFC.OverrideRespawnManager = false

    LuaUserData.RegisterType [[DSSIFactionCraft.CharacterUtils]]
    local CharacterUtils = LuaUserData.CreateStatic [[DSSIFactionCraft.CharacterUtils]]
    ---@type fun(character: Barotrauma.Character, tags: string[])
    DFC.AddCharacterTags = CharacterUtils.AddTags
    ---@type fun(character: Barotrauma.Character) : string[]
    DFC.GetCharacterTags = CharacterUtils.GetTags

    LuaUserData.RegisterType [[DSSIFactionCraft.XMLExtensions]]
    local XMLExtensions = LuaUserData.CreateStatic [[DSSIFactionCraft.XMLExtensions]]
    ---@type fun(submarineElement : System.Xml.Linq.XElement, xpath : string) : System.Xml.Linq.XElement[]
    DFC.XMLExtensions = {
        XPathSelectElements = XMLExtensions.XPathSelectElements
    }

    DFC.Path = path

    l10n.loadlangs(DFC.Path .. "/Lua/DSSIFactionCraft/localizations")

    require "DSSIFactionCraft.classes.taggable"
    require "DSSIFactionCraft.classes.participatory"
    require "DSSIFactionCraft.classes.spawnpointset"
    require "DSSIFactionCraft.classes.gear"
    require "DSSIFactionCraft.classes.job"
    require "DSSIFactionCraft.classes.faction"
    require "DSSIFactionCraft.classes.dfc"

    require "DSSIFactionCraft.utils"

    Hook.Add("dssi.inject.after", "DFC",
        ---@param submarineInfo Barotrauma.SubmarineInfo
        function(submarineInfo)
            local dfc = DFC.Loaded
            if dfc == nil then
                local dfc_initializer = DFC.XMLExtensions.XPathSelectElements(
                    submarineInfo.SubmarineElement, [[ //Item[@identifier="dfc_initializer"]/DfcInitializer ]])[1]
                if dfc_initializer then
                    dfc = New 'dfc' ()
                    for attribute in dfc_initializer.Attributes() do
                        local attributeName = attribute.Name.LocalName:lower()
                        local attributeValue = attribute.Value:lower()
                        if attributeName == "allowrespawn" then
                            dfc.allowRespawn = attributeValue == "true"
                        elseif attributeName == "allowmidroundjoin" then
                            dfc.allowMidRoundJoin = attributeValue == "true"
                        elseif attributeName == "autoparticipatewhennochoices" then
                            dfc.autoParticipateWhenNoChoices = attributeValue == "true"
                        end
                    end
                    dfc:initialize()
                end
            end
        end
    )

    if SERVER then
        Hook.Add("item.readPropertyChange", "DFC",
            ---@param item Barotrauma.Item
            function(item)
                if item.HasTag("dfc_mapdevtool") then
                    return true;
                end
            end)
    end

    print("initialized dfc successfully!")
end)

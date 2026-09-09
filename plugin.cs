using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

// Compatible namespaces for Lumina 7 / Dalamud v15
using Lumina.Excel.Sheets;
using ItemRow = Lumina.Excel.Sheets.Item;
using TerritoryRow = Lumina.Excel.Sheets.TerritoryType;
using StatusRow = Lumina.Excel.Sheets.Status;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Spiritbond Tracker";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    public readonly WindowSystem WindowSystem;
    private readonly SpiritbondWindow mainWindow;
    private readonly SettingsWindow settingsWindow;
    public readonly PluginConfig Config;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IObjectTable objectTable,
        IClientState clientState,
        ICondition condition,
        IFramework framework,
        IDataManager dataManager,
        IChatGui chatGui)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;

        var configPath = Path.Combine(pluginInterface.GetPluginConfigDirectory(), "config.json");
        PluginConfig config;
        try
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<PluginConfig>(json) ?? new PluginConfig();
            }
            else
            {
                config = new PluginConfig();
            }
        }
        catch
        {
            config = new PluginConfig();
        }
        this.Config = config;
        this.WindowSystem = new WindowSystem("SpiritbondTracker");
        this.mainWindow = new SpiritbondWindow(pluginInterface, commandManager, objectTable, clientState, condition, dataManager, chatGui, config, this);
        this.settingsWindow = new SettingsWindow(pluginInterface, config, this.mainWindow);

        this.WindowSystem.AddWindow(this.mainWindow);
        this.WindowSystem.AddWindow(this.settingsWindow);

        this.commandManager.AddHandler("/spiritbond", new Dalamud.Game.Command.CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Spiritbond Tracker window"
        });

        this.pluginInterface.UiBuilder.Draw += DrawUI;
        this.pluginInterface.UiBuilder.OpenMainUi += ToggleMainWindow;
        this.pluginInterface.UiBuilder.OpenConfigUi += ToggleSettingsWindow;
    }

    public void SaveConfig()
    {
        try
        {
            string dir = pluginInterface.GetPluginConfigDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "config.json");
            string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    public void Dispose()
    {
        this.pluginInterface.UiBuilder.Draw -= DrawUI;
        this.pluginInterface.UiBuilder.OpenMainUi -= ToggleMainWindow;
        this.pluginInterface.UiBuilder.OpenConfigUi -= ToggleSettingsWindow;

        this.commandManager.RemoveHandler("/spiritbond");
        this.WindowSystem.RemoveAllWindows();
    }

    private void OnCommand(string command, string args)
    {
        this.mainWindow.Toggle();
    }

    private void ToggleMainWindow()
    {
        this.mainWindow.Toggle();
    }

    public void ToggleSettingsWindow()
    {
        this.settingsWindow.Toggle();
    }

    private void DrawUI()
    {
        this.WindowSystem.Draw();
    }
}

public class PluginConfig
{
    public bool HideInCutscenes { get; set; } = true;
    public bool HideInPvP { get; set; } = true;
    public bool HideUntilReady { get; set; } = false;
    public Vector2 CompactWindowSize { get; set; } = new Vector2(260, 260);
    public Vector2 FullWindowSize { get; set; } = new Vector2(880, 620);

    public bool CompactMode { get; set; } = false;
    public bool ShowBuffSynergy { get; set; } = true;
    public bool ShowEligibilityColumn { get; set; } = true;
    public bool ShowItemLevel { get; set; } = true;
    public bool SendChatNotification { get; set; } = true;

    public bool ShowCappedSlotsCounter { get; set; } = false;
    public bool WarnExpiringBuffs { get; set; } = true;
    public bool ShowDutyAdvisor { get; set; } = true;
    public bool EcoMode { get; set; } = false;
    public bool AutoEcoFieldOps { get; set; } = true;
    public bool EnableAnimations { get; set; } = true;

    public float UiScale { get; set; } = 1.0f;
    public bool HighContrastMode { get; set; } = false;
    public int UiLayoutStyleIndex { get; set; } = 0;
    public int ColorThemeIndex { get; set; } = 0;
    public int HistoryViewMode { get; set; } = 1;
}

public class SettingsWindow : Window
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly PluginConfig config;
    private readonly SpiritbondWindow mainWindow;

    public SettingsWindow(IDalamudPluginInterface pluginInterface, PluginConfig config, SpiritbondWindow mainWindow)
        : base("Spiritbond Tracker Settings###SpiritbondTrackerSettings")
    {
        this.pluginInterface = pluginInterface;
        this.config = config;
        this.mainWindow = mainWindow;

        this.AllowPinning = true;
        this.AllowClickthrough = true;

        this.Size = new Vector2(380, 520);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 250),
            MaximumSize = new Vector2(1000, 1200)
        };
    }

    public void SaveConfig()
    {
        try
        {
            string dir = pluginInterface.GetPluginConfigDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "config.json");
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    public override void Draw()
    {
        bool changed = false;

        if (ImGui.CollapsingHeader("Display & Layout", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool compactMode = config.CompactMode;
            if (ImGui.Checkbox("Compact Mode (Icon Grid)", ref compactMode)) { config.CompactMode = compactMode; changed = true; }

            bool showBuffSynergy = config.ShowBuffSynergy;
            if (ImGui.Checkbox("Show Buff Synergy Header", ref showBuffSynergy)) { config.ShowBuffSynergy = showBuffSynergy; changed = true; }

            bool showEligibilityColumn = config.ShowEligibilityColumn;
            if (ImGui.Checkbox("Show iLvl Eligibility Column", ref showEligibilityColumn)) { config.ShowEligibilityColumn = showEligibilityColumn; changed = true; }

            bool showItemLevel = config.ShowItemLevel;
            if (ImGui.Checkbox("Show Item Level next to Name", ref showItemLevel)) { config.ShowItemLevel = showItemLevel; changed = true; }

            bool showCappedSlotsCounter = config.ShowCappedSlotsCounter;
            if (ImGui.Checkbox("Show Capped Slots Counter", ref showCappedSlotsCounter)) { config.ShowCappedSlotsCounter = showCappedSlotsCounter; changed = true; }

            bool showDutyAdvisor = config.ShowDutyAdvisor;
            if (ImGui.Checkbox("Show Smart Duty Advisor (Roulette & Randomizer)", ref showDutyAdvisor)) { config.ShowDutyAdvisor = showDutyAdvisor; changed = true; }

            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader("Smart Hide Conditions"))
        {
            bool hideInCutscenes = config.HideInCutscenes;
            if (ImGui.Checkbox("Hide in Cutscenes", ref hideInCutscenes)) { config.HideInCutscenes = hideInCutscenes; changed = true; }

            bool hideInPvP = config.HideInPvP;
            if (ImGui.Checkbox("Hide in PvP (Wolves' Den, Frontline)", ref hideInPvP)) { config.HideInPvP = hideInPvP; changed = true; }

            bool hideUntilReady = config.HideUntilReady;
            if (ImGui.Checkbox("Hide until any gear reaches 100%", ref hideUntilReady)) { config.HideUntilReady = hideUntilReady; changed = true; }

            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader("Alerts & Notifications"))
        {
            bool sendChatNotification = config.SendChatNotification;
            if (ImGui.Checkbox("Send Chat Alert on 100% Capped", ref sendChatNotification)) { config.SendChatNotification = sendChatNotification; changed = true; }

            bool warnExpiringBuffs = config.WarnExpiringBuffs;
            if (ImGui.Checkbox("Warn about expiring Spiritbond buffs (< 2 mins)", ref warnExpiringBuffs)) { config.WarnExpiringBuffs = warnExpiringBuffs; changed = true; }

            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader("Appearance & Themes"))
        {
            bool highContrastMode = config.HighContrastMode;
            if (ImGui.Checkbox("High Contrast Mode", ref highContrastMode)) { config.HighContrastMode = highContrastMode; changed = true; }

            float uiScale = config.UiScale;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat("UI Font Scale", ref uiScale, 1.0f, 1.5f, "%.1fx"))
            {
                config.UiScale = uiScale;
                changed = true;
            }

            int themeIndex = config.ColorThemeIndex;
            string[] themes = { "Classic Blue", "Neon Cyan", "Warm Amber", "Matrix Emerald" };
            ImGui.SetNextItemWidth(200f);
            if (ImGui.Combo("Color Theme", ref themeIndex, themes, themes.Length))
            {
                config.ColorThemeIndex = themeIndex;
                changed = true;
            }

            int layoutStyleIndex = config.UiLayoutStyleIndex;
            string[] layouts = { "Modern Dark (Default)", "Classic FF I-VI (Retro RPG)", "Mac OS X (Aquatic Minimal)", "Xbox 360 (Blade & Neon)" };
            ImGui.SetNextItemWidth(200f);
            if (ImGui.Combo("Element Style", ref layoutStyleIndex, layouts, layouts.Length))
            {
                config.UiLayoutStyleIndex = layoutStyleIndex;
                changed = true;
            }

            ImGui.Spacing();
        }

        if (ImGui.CollapsingHeader("Performance & History"))
        {
            bool ecoMode = config.EcoMode;
            if (ImGui.Checkbox("Enable Eco Mode / Low-Overhead Safe Mode", ref ecoMode)) { config.EcoMode = ecoMode; changed = true; }

            bool autoEcoFieldOps = config.AutoEcoFieldOps;
            if (ImGui.Checkbox("Auto-enable Eco Mode in Field Ops (Eureka, Bozja, Occult)", ref autoEcoFieldOps)) { config.AutoEcoFieldOps = autoEcoFieldOps; changed = true; }

            bool enableAnimations = config.EnableAnimations;
            if (ImGui.Checkbox("Enable UI Animations", ref enableAnimations)) { config.EnableAnimations = enableAnimations; changed = true; }

            int historyViewMode = config.HistoryViewMode;
            string[] historyModes = { "Minimal (Compact list)", "Advanced Cards (Detailed duty & job breakdown)" };
            ImGui.SetNextItemWidth(200f);
            if (ImGui.Combo("History Layout", ref historyViewMode, historyModes, historyModes.Length))
            {
                config.HistoryViewMode = historyViewMode;
                changed = true;
            }

            ImGui.Spacing();
        }

        if (changed)
        {
            SaveConfig();
        }

        ImGui.Separator();
        if (ImGui.Button("Close Settings"))
        {
            this.Toggle();
        }
    }
}

public class SpiritbondWindow : Window
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IObjectTable objectTable;
    private readonly IClientState clientState;
    private readonly ICondition condition;
    private readonly IDataManager dataManager;
    private readonly IChatGui chatGui;
    private readonly PluginConfig config;
    private readonly Plugin pluginInstance;

    private bool isHistoryWindowVisible = false;
    private bool isStatsWindowVisible = false;
    private string historySearchFilter = string.Empty;
    private string historyCategoryFilter = "All";
    
    private string cachedRecommendedDuty = string.Empty;
    private float lastCheckedAvgIvl = 0f;
    private readonly Random randomRoller = new();
    private int ecoModeFrameCounter = 0;
    private bool wasAutoEcoActive = false;

    private Dictionary<int, bool> notifiedCappedSlots = new();

    private List<GearDisplayInfo> cachedGearList = new();
    private DateTime lastGearRefresh = DateTime.MinValue;
    private readonly Dictionary<uint, (string Name, uint ItemLevel)> itemDataCache = new();

    private bool cachedHasMedicatedBuff = false;
    private bool cachedHasManualBuff = false;
    private bool cachedHasFcBuff = false;
    private bool cachedHasFoodBuff = false;
    private bool cachedExpiringBuff = false;
    private float cachedMedicatedRemaining = 0f;
    private float cachedManualRemaining = 0f;
    private float cachedAvgIvl = 0f;
    private int cachedCappedCount = 0;
    private List<string> debugDetectedStatuses = new();
    private bool wasInExpiryWindow = false;

    // /item commands issued in the same frame do not both execute reliably.
    // When Apply All Buffs is used, start the potion first and issue the
    // manual after the item-use lock / command processing delay has passed.
    private bool pendingSquadronManualUse = false;
    private DateTime pendingSquadronManualUseAt = DateTime.MinValue;

    // Medicated is shared by Superior Spiritbonding Potion and crafting/gathering consumables.
    // StatusList does not expose the source item, so any fresh Medicated effect is protected.
    private const float MedicatedOverwriteProtectionSeconds = 300f;
    private const uint MedicatedStatusId = 49;
    private const uint FcSpiritbondStatusId = 361;
    private const uint SquadronManualStatusId = 1083;
    private const uint SuperiorSpiritbondPotionBaseItemId = 27960;
    private const uint SquadronSpiritbondingManualItemId = 14951;

    private static bool IsExpiringTimedBuff(float remainingTime)
        => remainingTime > 0f && remainingTime < 120f;

    // Disciples of the Land (gathering) and Disciples of the Hand (crafting) job abbreviations.
    // Spiritbonding potions are meant for combat-job gear progression; on these jobs (or while a
    // crafting/gathering consumable is active) the potion button must not fire.
   

    private static readonly HashSet<string> CraftingJobAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"
    };

   private bool IsCraftingClassActive()
{
    if (objectTable.LocalPlayer == null) return false;

    var classJobObj = objectTable.LocalPlayer.ClassJob.Value;
    string abbr = classJobObj.Abbreviation.ToString();

    return CraftingJobAbbreviations.Contains(abbr);
}

    private static readonly (int Index, string Name, string Category)[] Slots =
    {
        (0, "Main hand", "Weapon"),
        (1, "Off hand", "Weapon"),
        (2, "Head", "Armor"),
        (3, "Body", "Armor"),
        (4, "Hands", "Armor"),
        (6, "Legs", "Armor"),
        (7, "Feet", "Armor"),
        (8, "Earrings", "Accessory"),
        (9, "Necklace", "Accessory"),
        (10, "Bracelets", "Accessory"),
        (11, "Ring 1", "Accessory"),
        (12, "Ring 2", "Accessory"),
    };

    private Dictionary<int, ushort> dutyStartSpiritbond = new();
    private Dictionary<int, ushort> previousSlotSpiritbond = new();
    private Dictionary<int, uint> trackedItemIds = new();
    private string lastTrackedDuty = string.Empty;
    private string currentSessionId = string.Empty;

    public class GearDisplayInfo
    {
        public int SlotIndex;
        public string SlotName = string.Empty;
        public string Category = string.Empty;
        public string ItemName = string.Empty;
        public uint ItemLevel;
        public float CurrentPercent;
        public float GainedInDuty;
        public string Eligibility = "Optimal";
        public Vector4 EligibilityColor = new(0.2f, 1.0f, 0.2f, 1.0f);
    }

    private bool ShouldHideWindow()
    {
        if (!clientState.IsLoggedIn || 
            condition[ConditionFlag.BetweenAreas] || 
            condition[ConditionFlag.BetweenAreas51])
        {
            return true;
        }

        if (config.HideInCutscenes && (
            condition[ConditionFlag.OccupiedInCutSceneEvent] || 
            condition[ConditionFlag.WatchingCutscene78]))
        {
            return true;
        }

        if (config.HideInPvP && clientState.IsPvP)
        {
            return true;
        }

        if (config.HideUntilReady && !cachedGearList.Any(g => g.CurrentPercent >= 100f))
        {
            return true;
        }

        return false;
    }

    public class HistoryRecord
    {
        public string SessionId { get; set; } = string.Empty;
        public string DutyName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string JobName { get; set; } = "Unknown";
        public uint ItemLevel { get; set; }
        public float Gained { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private List<HistoryRecord> completedHistory = new();
    private string historyFilePath => Path.Combine(pluginInterface.GetPluginConfigDirectory(), "spiritbond_history.json");

    public SpiritbondWindow(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IObjectTable objectTable,
        IClientState clientState,
        ICondition condition,
        IDataManager dataManager,
        IChatGui chatGui,
        PluginConfig config,
        Plugin pluginInstance)
        : base("Spiritbond Tracker###SpiritbondTrackerMain", ImGuiWindowFlags.NoScrollbar)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.objectTable = objectTable;
        this.clientState = clientState;
        this.condition = condition;
        this.dataManager = dataManager;
        this.chatGui = chatGui;
        this.config = config;
        this.pluginInstance = pluginInstance;

        this.AllowPinning = true;
        this.AllowClickthrough = true;

        this.Size = config.CompactMode ? config.CompactWindowSize : config.FullWindowSize;
        this.SizeCondition = ImGuiCond.FirstUseEver;

        LoadHistory();
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(historyFilePath))
            {
                string json = File.ReadAllText(historyFilePath);
                var data = JsonSerializer.Deserialize<List<HistoryRecord>>(json);
                if (data != null) completedHistory = data;
            }
        }
        catch { completedHistory = new List<HistoryRecord>(); }
    }

    private void SaveHistory()
    {
        try
        {
            string dir = pluginInterface.GetPluginConfigDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(completedHistory, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(historyFilePath, json);
        }
        catch { }
    }

    private void ExportHistoryToCsv()
    {
        try
        {
            string dir = pluginInterface.GetPluginConfigDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string csvPath = Path.Combine(dir, "spiritbond_export.csv");
            using var writer = new StreamWriter(csvPath);
            writer.WriteLine("SessionId,DutyName,ItemName,Category,JobName,ItemLevel,GainedPercent,Timestamp");
            foreach (var h in completedHistory)
            {
                writer.WriteLine($"\"{h.SessionId}\",\"{h.DutyName}\",\"{h.ItemName}\",\"{h.Category}\",\"{h.JobName}\",{h.ItemLevel},{h.Gained:F2},{h.Timestamp:yyyy-MM-dd HH:mm:ss}");
            }

            chatGui.Print($"[Spiritbond Tracker] History exported successfully to: {csvPath}");
        }
        catch (Exception ex)
        {
            chatGui.Print($"[Spiritbond Tracker] Failed to export CSV: {ex.Message}");
        }
    }

    private readonly struct InventoryItemLocation
    {
        public uint ItemId { get; }
        public InventoryType InventoryType { get; }
        public uint Slot { get; }

        public InventoryItemLocation(uint itemId, InventoryType inventoryType, uint slot)
        {
            ItemId = itemId;
            InventoryType = inventoryType;
            Slot = slot;
        }
    }

    private unsafe bool TryFindItemInNormalInventory(uint baseItemId, out InventoryItemLocation location)
    {
        location = default;
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) return false;

        InventoryType[] inventoryTypes =
        {
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4,
        };

        foreach (var inventoryType in inventoryTypes)
        {
            var container = inventoryManager->GetInventoryContainer(inventoryType);
            if (container == null) continue;

            for (int slot = 0; slot < container->Size; slot++)
            {
                var item = container->GetInventorySlot(slot);
                if (item == null || item->ItemId == 0) continue;

               // InventoryItem.ItemId zawiera bazowy ID itemu.
// Jakość HQ jest przechowywana osobno w ItemFlags.
if (item->ItemId != baseItemId)
{
    continue;
}

uint actionItemId = item->ItemId;

// UseItem wymaga ID akcji HQ, czyli bazowego ID + 1 000 000.
if (item->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality))
{
    actionItemId += 1_000_000;
}

location = new InventoryItemLocation(
    actionItemId,
    inventoryType,
    (uint)slot
);

return true;
            }
        }

        return false;
    }

   private unsafe bool TryUseInventoryItem(
    InventoryItemLocation item)
{
    var inventoryContext = AgentInventoryContext.Instance();

    if (inventoryContext == null)
    {
        chatGui.Print(
            "[Spiritbond Tracker] Inventory context is unavailable."
        );

        return false;
    }

    // UseItem samo odnajduje właściwy stack w normalnym inventory.
    // item.ItemId zachowuje wariant normalny albo HQ:
    // normal: 27960, HQ: 1027960.
    long result = inventoryContext->UseItem(item.ItemId);

    // Dla tej funkcji 0 oznacza poprawne przekazanie użycia itemu.
    return result == 0;
}

  private unsafe bool TryUseSpiritbondPotion()
{
    if (!TryFindItemInNormalInventory(
        SuperiorSpiritbondPotionBaseItemId,
        out var potion))
    {
        chatGui.Print(
            "[Spiritbond Tracker] Superior Spiritbond Potion was not found in normal inventory."
        );

        return false;
    }

    return TryUseInventoryItem(potion);
}


    

   private unsafe bool TryUseSquadronManual()
{
    if (!TryFindItemInNormalInventory(
        SquadronSpiritbondingManualItemId,
        out var manual))
    {
        chatGui.Print(
            "[Spiritbond Tracker] Squadron Spiritbonding Manual was not found in normal inventory."
        );

        return false;
    }

    return TryUseInventoryItem(manual);
}
       

    public override unsafe void Update()
    {
        if (objectTable.LocalPlayer == null) return;
        
        if (pendingSquadronManualUse &&
            DateTime.UtcNow >= pendingSquadronManualUseAt)
    {
            pendingSquadronManualUse = false;
            TryUseSquadronManual();
    }

        if (config.AutoEcoFieldOps)
        {
            bool isFieldOps = currentDutyName.Contains("Eureka", StringComparison.OrdinalIgnoreCase) ||
                currentDutyName.Contains("Bozja", StringComparison.OrdinalIgnoreCase) ||
                currentDutyName.Contains("Zadnor", StringComparison.OrdinalIgnoreCase) ||
                currentDutyName.Contains("Occult", StringComparison.OrdinalIgnoreCase) ||
                currentDutyName.Contains("Field Ops", StringComparison.OrdinalIgnoreCase);

            if (isFieldOps && !config.EcoMode)
            {
                config.EcoMode = true;
                wasAutoEcoActive = true;
                chatGui.Print("[Spiritbond Tracker] Field Ops detected: Eco-mode automatically enabled.");
            }
            else if (!isFieldOps && wasAutoEcoActive)
            {
                config.EcoMode = false;
                wasAutoEcoActive = false;
                chatGui.Print("[Spiritbond Tracker] Left Field Ops: Eco-mode automatically disabled.");
            }
        }

        if (config.EcoMode)
        {
            ecoModeFrameCounter++;
            if (ecoModeFrameCounter < 60) return;
            ecoModeFrameCounter = 0;
        }

        inDuty = condition[ConditionFlag.BoundByDuty];

        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) return;

        var equippedContainer = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
        if (equippedContainer == null) return;

        uint territoryId = clientState.TerritoryType;
        var territory = dataManager.GetExcelSheet<TerritoryRow>()?.GetRowOrDefault(territoryId);

        if (territory.HasValue)
        {
            var placeName = territory.Value.PlaceName.Value.Name.ToString();
            currentDutyName = !string.IsNullOrEmpty(placeName) ? placeName : $"Territory #{territoryId}";

            var cfc = territory.Value.ContentFinderCondition.Value;
            if (cfc.RowId != 0)
            {
                currentDutyLevel = cfc.ItemLevelSync > 0 ? cfc.ItemLevelSync :
                    (cfc.ItemLevelRequired > 0 ? cfc.ItemLevelRequired : cfc.ClassJobLevelRequired);
            }
        }
        else
        {
            currentDutyName = "Overworld / Field Ops";
            currentDutyLevel = 0;
        }

        string sessionKey = $"{currentDutyName}_{inDuty}_{territoryId}";
        if (lastTrackedDuty != sessionKey)
        {
            SaveSessionToHistory();
            dutyStartSpiritbond.Clear();
            previousSlotSpiritbond.Clear();
            notifiedCappedSlots.Clear();
            lastTrackedDuty = sessionKey;
            currentSessionId = $"{currentDutyName} ({DateTime.Now:yyyy-MM-dd HH:mm:ss})";
        }

        foreach (var slot in Slots)
        {
            if (slot.Index >= equippedContainer->Size) continue;

            var item = equippedContainer->GetInventorySlot(slot.Index);
            if (item == null || item->ItemId == 0) continue;

            ushort currentSb = (ushort)item->SpiritbondOrCollectability;
            bool itemChanged = trackedItemIds.TryGetValue(slot.Index, out uint oldItemId) && oldItemId != item->ItemId;

            if (itemChanged)
            {
                // A gearset swap may reuse the same slot for an unrelated item.
                // Its old baseline must never be used for the new item.
                dutyStartSpiritbond.Remove(slot.Index);
                previousSlotSpiritbond.Remove(slot.Index);
                notifiedCappedSlots.Remove(slot.Index);
            }
            trackedItemIds[slot.Index] = item->ItemId;

            if (!dutyStartSpiritbond.ContainsKey(slot.Index))
                dutyStartSpiritbond[slot.Index] = currentSb;

            if (!previousSlotSpiritbond.TryGetValue(slot.Index, out ushort previousSb))
            {
                // First observation is a baseline, never a cap notification.
                previousSlotSpiritbond[slot.Index] = currentSb;
                continue;
            }

            if (config.SendChatNotification &&
                previousSb < 10000 &&
                currentSb >= 10000 &&
                !notifiedCappedSlots.ContainsKey(slot.Index))
            {
                notifiedCappedSlots[slot.Index] = true;
                chatGui.Print($"[Spiritbond Tracker] Slot {slot.Name} reached 100% spiritbond! Ready for extraction.");
            }

            previousSlotSpiritbond[slot.Index] = currentSb;
        }

        // Update loop running every 250 ms
        if ((DateTime.UtcNow - lastGearRefresh).TotalMilliseconds >= 250)
        {
            cachedGearList = GetCurrentGearList();
            cachedCappedCount = cachedGearList.Count(g => g.CurrentPercent >= 100f);
            cachedAvgIvl = GetAverageEquippedItemLevel();

            cachedHasMedicatedBuff = false;
            cachedHasManualBuff = false;
            cachedHasFcBuff = false;
            cachedHasFoodBuff = false;
            cachedExpiringBuff = false;
            cachedMedicatedRemaining = 0f;
            cachedManualRemaining = 0f;
            debugDetectedStatuses.Clear();

            if (objectTable.LocalPlayer != null)
            {
                var statusSheet = dataManager.GetExcelSheet<StatusRow>();
                if (statusSheet != null)
                {
                    foreach (var status in objectTable.LocalPlayer.StatusList)
                    {
                        if (status.StatusId == 0) continue;
                        var row = statusSheet.GetRowOrDefault(status.StatusId);
                        if (!row.HasValue) continue;

                        string bName = row.Value.Name.ToString();
                        string bDesc = row.Value.Description.ToString();
                        string bNameLower = bName.ToLowerInvariant();
                        string bDescLower = bDesc.ToLowerInvariant();
                        debugDetectedStatuses.Add($"Raw: '{bName}' (ID: {status.StatusId}, Rem: {status.RemainingTime:F1}s)");

                        if (bNameLower.Contains("well fed") &&
                            (bDescLower.Contains("spiritbond") || bDescLower.Contains("spiritbonding")))
                        {
                            cachedHasFoodBuff = true;
                            debugDetectedStatuses.Add($"Food: '{bName}' (ID: {status.StatusId})");
                        }
                        // FC buff is display-only: it does not satisfy a required buff, trigger
                        // a refresh, contribute to synergy, or produce an expiry warning.
                        else if (status.StatusId == FcSpiritbondStatusId)
                        {
                            cachedHasFcBuff = true;
                            debugDetectedStatuses.Add($"FC Spiritbond Action (display only): '{bName}' (ID: {status.StatusId}, Rem: {status.RemainingTime:F1}s)");
                        }
                        // The actual Squadron Spiritbonding Manual status is ID 1038.
                        else if (status.StatusId == SquadronManualStatusId)
                        {
                            cachedHasManualBuff = status.RemainingTime > 0f;
                            cachedManualRemaining = status.RemainingTime;
                            debugDetectedStatuses.Add($"Squadron Manual: '{bName}' (ID: {status.StatusId}, Rem: {status.RemainingTime:F1}s)");

                            if (config.WarnExpiringBuffs && IsExpiringTimedBuff(status.RemainingTime))
                                cachedExpiringBuff = true;
                        }
                        // Medicated is source-agnostic: do not claim it is a spiritbond potion.
                        else if (status.StatusId == MedicatedStatusId)
                        {
                            cachedHasMedicatedBuff = status.RemainingTime > 0f;
                            cachedMedicatedRemaining = status.RemainingTime;
                            debugDetectedStatuses.Add($"Medicated (source unknown): '{bName}' (ID: {status.StatusId}, Rem: {status.RemainingTime:F1}s)");
                        }
                    }
                }
            }

            // Only the verified Squadron Manual can generate an expiry warning, once per entry
            // into the final two-minute window. FC and Medicated are deliberately excluded.
            bool isInExpiryWindow = cachedExpiringBuff && config.WarnExpiringBuffs;
            if (isInExpiryWindow && !wasInExpiryWindow)
            {
                chatGui.Print("[Spiritbond Tracker] WARNING: Squadron Spiritbonding Manual expires in less than 2 minutes!");
            }
            wasInExpiryWindow = isInExpiryWindow;

            lastGearRefresh = DateTime.UtcNow;
        }
    }

    public unsafe void SaveSessionToHistory()
    {
        if (string.IsNullOrEmpty(currentSessionId) || currentDutyName.Contains("Overworld")) return;
        if (dutyStartSpiritbond.Count == 0) return;

        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) return;
        var equippedContainer = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
        if (equippedContainer == null) return;

        string currentJobName = "Unknown";
        if (objectTable.LocalPlayer != null)
        {
            var classJobObj = objectTable.LocalPlayer.ClassJob.Value;
            string jobStr = classJobObj.Name.ToString();
            if (!string.IsNullOrEmpty(jobStr))
            {
                currentJobName = jobStr;
            }
            else
            {
                string abbr = classJobObj.Abbreviation.ToString();
                if (!string.IsNullOrEmpty(abbr)) currentJobName = abbr;
            }
        }

        bool addedAny = false;
        foreach (var slot in Slots)
        {
            if (slot.Index >= equippedContainer->Size) continue;
            var item = equippedContainer->GetInventorySlot(slot.Index);
            if (item == null || item->ItemId == 0) continue;

            ushort currentSb = (ushort)item->SpiritbondOrCollectability;
            if (dutyStartSpiritbond.TryGetValue(slot.Index, out var startSb))
            {
                int diff = currentSb - startSb;
                if (diff > 0)
                {
                    if (!itemDataCache.TryGetValue(item->ItemId, out var cachedData))
                    {
                        var excelItem = dataManager.GetExcelSheet<ItemRow>()?.GetRowOrDefault(item->ItemId);
                        string realName = excelItem.HasValue ? excelItem.Value.Name.ToString() : $"Unknown ({item->ItemId})";
                        uint iLvl = excelItem.HasValue ? excelItem.Value.LevelItem.RowId : 0;
                        cachedData = (realName, iLvl);
                        itemDataCache[item->ItemId] = cachedData;
                    }

                    completedHistory.Add(new HistoryRecord
                    {
                        SessionId = currentSessionId,
                        DutyName = currentDutyName,
                        ItemName = cachedData.Name,
                        Category = slot.Category,
                        JobName = currentJobName,
                        ItemLevel = cachedData.ItemLevel,
                        Gained = diff / 100f,
                        Timestamp = DateTime.Now
                    });
                    addedAny = true;
                }
            }
        }

        if (addedAny) SaveHistory();
    }

    private unsafe List<GearDisplayInfo> GetCurrentGearList()
    {
        var list = new List<GearDisplayInfo>();
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) return list;

        var equippedContainer = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
        if (equippedContainer == null) return list;

        foreach (var slot in Slots)
        {
            if (slot.Index >= equippedContainer->Size) continue;

            var item = equippedContainer->GetInventorySlot(slot.Index);
            if (item == null || item->ItemId == 0) continue;

            ushort currentSb = (ushort)item->SpiritbondOrCollectability;
            ushort startSb = dutyStartSpiritbond.TryGetValue(slot.Index, out var sb) ? sb : currentSb;
            int diff = Math.Max(0, currentSb - startSb);
            float gainedInDuty = diff / 100f;

            if (!itemDataCache.TryGetValue(item->ItemId, out var cachedData))
            {
                var excelItem = dataManager.GetExcelSheet<ItemRow>()?.GetRowOrDefault(item->ItemId);
                string realName = excelItem.HasValue ? excelItem.Value.Name.ToString() : $"Unknown ({item->ItemId})";
                uint iLvl = excelItem.HasValue ? excelItem.Value.LevelItem.RowId : 0;
                cachedData = (realName, iLvl);
                itemDataCache[item->ItemId] = cachedData;
            }

            string eligibility = "Optimal";
            Vector4 eligColor = config.HighContrastMode ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f) : new Vector4(0.2f, 1.0f, 0.2f, 1.0f);

            if (gainedInDuty > 0f)
            {
                eligibility = "Active Gain";
                eligColor = config.HighContrastMode ? new Vector4(0.0f, 1.0f, 1.0f, 1.0f) : new Vector4(0.2f, 1.0f, 0.5f, 1.0f);
            }
            else if (currentDutyLevel > 0)
            {
                if (cachedData.ItemLevel > currentDutyLevel + 50)
                {
                    eligibility = "Too High";
                    eligColor = config.HighContrastMode ? new Vector4(1.0f, 0.0f, 0.0f, 1.0f) : new Vector4(1.0f, 0.2f, 0.2f, 1.0f);
                }
                else if (cachedData.ItemLevel > currentDutyLevel + 25)
                {
                    eligibility = "Reduced";
                    eligColor = config.HighContrastMode ? new Vector4(1.0f, 1.0f, 0.0f, 1.0f) : new Vector4(1.0f, 0.8f, 0.2f, 1.0f);
                }
            }

            list.Add(new GearDisplayInfo
            {
                SlotIndex = slot.Index,
                SlotName = slot.Name,
                Category = slot.Category,
                ItemName = cachedData.Name,
                ItemLevel = cachedData.ItemLevel,
                CurrentPercent = currentSb / 100f,
                GainedInDuty = gainedInDuty,
                Eligibility = eligibility,
                EligibilityColor = eligColor
            });
        }

        return list;
    }

    private unsafe float GetAverageEquippedItemLevel()
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) return 0f;
        var equippedContainer = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
        if (equippedContainer == null) return 0f;

        uint totalIvl = 0;
        int count = 0;
        foreach (var slot in Slots)
        {
            if (slot.Index >= equippedContainer->Size) continue;
            var item = equippedContainer->GetInventorySlot(slot.Index);
            if (item == null || item->ItemId == 0) continue;

            if (!itemDataCache.TryGetValue(item->ItemId, out var cachedData))
            {
                var excelItem = dataManager.GetExcelSheet<ItemRow>()?.GetRowOrDefault(item->ItemId);
                string realName = excelItem.HasValue ? excelItem.Value.Name.ToString() : $"Unknown ({item->ItemId})";
                uint iLvl = excelItem.HasValue ? excelItem.Value.LevelItem.RowId : 0;
                cachedData = (realName, iLvl);
                itemDataCache[item->ItemId] = cachedData;
            }

            totalIvl += cachedData.ItemLevel;
            count++;
        }

        return count > 0 ? (float)totalIvl / count : 0f;
    }

    private string GetRandomizedDutyRecommendation(float avgIvl, bool forceRoll = false)
    {
        if (Math.Abs(avgIvl - lastCheckedAvgIvl) > 1.5f || string.IsNullOrEmpty(cachedRecommendedDuty) || forceRoll)
        {
            lastCheckedAvgIvl = avgIvl;
            List<string> candidateDuties = new();

            if (avgIvl >= 650f)
            {
                candidateDuties.Add("⭐ [Expert Roulette Bonus] Latest Dawntrail Expert Dungeon");
                candidateDuties.Add("⭐ [Level 100 Roulette Bonus] Level 100 High-End Duty / Trial");
                candidateDuties.Add("⭐ [Alliance Raid Roulette Bonus] Level 100 Alliance Raid");
                candidateDuties.Add("Field Ops: Urqopacha / Living Memory FATE Farming");
            }
            else if (avgIvl >= 600f)
            {
                candidateDuties.Add("⭐ [Level 90-100 Roulette Bonus] High-Level Dungeon / Normal Raid");
                candidateDuties.Add("Level 90-100 Alliance Raids (Aglaia / Euphrosyne / Thaleia)");
            }
            else
            {
                candidateDuties.Add("⭐ [Leveling Roulette Bonus] Level-Appropriate High-Level Dungeon");
                candidateDuties.Add("Deep Dungeons (Palace of the Dead / Heaven-on-High / Eureka Orthos)");
            }

            candidateDuties.Add("Variant & Criterion Dungeons (Aloalo / Mount Rokkon)");
            candidateDuties.Add("Bozjan Southern Front / Zadnor");

            int index = randomRoller.Next(candidateDuties.Count);
            cachedRecommendedDuty = candidateDuties[index];
        }

        return cachedRecommendedDuty;
    }

    private uint currentDutyLevel = 0;
    private string currentDutyName = "Not in duty";
    private bool inDuty = false;

    private static FontAwesomeIcon GetSlotIcon(int slotIndex) => slotIndex switch
    {
        0 => FontAwesomeIcon.Gavel,
        1 => FontAwesomeIcon.ShieldAlt,
        2 => FontAwesomeIcon.HardHat,
        3 => FontAwesomeIcon.Tshirt,
        4 => FontAwesomeIcon.Mitten,
        6 => FontAwesomeIcon.UserInjured,
        7 => FontAwesomeIcon.ShoePrints,
        8 => FontAwesomeIcon.AssistiveListeningSystems,
        9 => FontAwesomeIcon.Gem,
        10 => FontAwesomeIcon.CircleNotch,
        11 => FontAwesomeIcon.Ring,
        12 => FontAwesomeIcon.Ring,
        _ => FontAwesomeIcon.Cubes
    };

    private void DrawCompactUI(Vector4 accentColor, float fontScale)
    {
        var gearList = cachedGearList;

        ImGui.TextColored(cachedCappedCount > 0 ? new Vector4(0.0f, 1.0f, 0.5f, 1.0f) : accentColor, $"Ready: {cachedCappedCount}/{gearList.Count}");
        ImGui.SameLine();
        if (ImGui.SmallButton("Extract##CompactExtract"))
        {
            unsafe
            {
                var am = ActionManager.Instance();
                if (am != null)
                {
                    am->UseAction(ActionType.GeneralAction, 14);
                }
            }
        }

        ImGui.Separator();

        var leftSide = gearList.Where(g => g.Category == "Weapon" || g.Category == "Armor").ToList();
        var rightSide = gearList.Where(g => g.Category == "Accessory").ToList();

        if (ImGui.BeginTable("CompactGearGrid", 2, ImGuiTableFlags.None))
        {
            ImGui.TableSetupColumn("LeftGear", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("RightGear", ImGuiTableColumnFlags.WidthStretch);

            int rows = Math.Max(leftSide.Count, rightSide.Count);

            for (int i = 0; i < rows; i++)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                if (i < leftSide.Count)
                {
                    DrawCompactSlotItem(leftSide[i], accentColor, fontScale);
                }

                ImGui.TableNextColumn();
                if (i < rightSide.Count)
                {
                    DrawCompactSlotItem(rightSide[i], accentColor, fontScale);
                }
            }

            ImGui.EndTable();
        }
    }

    private void DrawCompactSlotItem(GearDisplayInfo gear, Vector4 accentColor, float fontScale)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(GetSlotIcon(gear.SlotIndex).ToIconString());
        ImGui.PopFont();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{gear.SlotName}: {gear.ItemName} (i{gear.ItemLevel})\nGain: +{gear.GainedInDuty:F2}%\nStatus: {gear.Eligibility}");
        }

        ImGui.SameLine();

        float progress = Math.Clamp(gear.CurrentPercent / 100f, 0.0f, 1.0f);
        Vector4 barColor = gear.CurrentPercent >= 100f
            ? (config.HighContrastMode ? new Vector4(1f, 1f, 0f, 1f) : new Vector4(0.2f, 0.9f, 0.2f, 1.0f))
            : accentColor;

        string overlay = $"{gear.CurrentPercent:F0}%";
        if (gear.GainedInDuty > 0f) overlay += $" (+{gear.GainedInDuty:F1}%)";

        Vector2 barPos = ImGui.GetCursorScreenPos();
        Vector2 barSize = new Vector2(ImGui.GetContentRegionAvail().X, 16f * fontScale);

        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
        ImGui.ProgressBar(progress, barSize, "");
        ImGui.PopStyleColor();

        Vector2 textSize = ImGui.CalcTextSize(overlay);
        Vector2 textPos = new Vector2(
            barPos.X + 6.0f,
            barPos.Y + (barSize.Y - textSize.Y) * 0.5f
        );

        uint textColor = progress >= 0.18f
            ? ImGui.GetColorU32(new Vector4(0.02f, 0.02f, 0.02f, 1.0f))
            : ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddText(textPos, textColor, overlay);
        drawList.AddText(new Vector2(textPos.X + 0.5f, textPos.Y), textColor, overlay);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{gear.SlotName}: {gear.ItemName}\nSpiritbond: {gear.CurrentPercent:F2}%\nGain in Duty: +{gear.GainedInDuty:F2}%");
        }
    }

    public override bool DrawConditions()
    {
        if (ShouldHideWindow())
            return false;

        return base.DrawConditions();
    }

    public override void Draw()
    {
        if (config.UiScale > 1.0f)
        {
            ImGui.SetWindowFontScale(config.UiScale);
        }

        float fontScale = ImGui.GetFontSize() / 17.0f;

        var style = ImGui.GetStyle();
        Vector4 windowBgColor;
        Vector4 headerColor;
        Vector4 frameBgColor;

        switch (config.UiLayoutStyleIndex)
        {
            case 1: // Classic FF I-VI
                style.WindowRounding = 1.0f;
                style.FrameRounding = 0.0f;
                style.PopupRounding = 1.0f;
                style.FrameBorderSize = 2.0f;
                windowBgColor = new Vector4(0.04f, 0.06f, 0.18f, 0.98f);
                headerColor = new Vector4(0.08f, 0.12f, 0.35f, 1.0f);
                frameBgColor = new Vector4(0.02f, 0.03f, 0.10f, 1.0f);
                break;

            case 2: // Mac OS X
                style.WindowRounding = 8.0f;
                style.FrameRounding = 6.0f;
                style.PopupRounding = 6.0f;
                style.FrameBorderSize = 1.0f;
                windowBgColor = new Vector4(0.18f, 0.19f, 0.21f, 0.95f);
                headerColor = new Vector4(0.28f, 0.30f, 0.33f, 1.0f);
                frameBgColor = new Vector4(0.12f, 0.13f, 0.15f, 1.0f);
                break;

            case 3: // Xbox 360
                style.WindowRounding = 4.0f;
                style.FrameRounding = 3.0f;
                style.PopupRounding = 3.0f;
                style.FrameBorderSize = 1.0f;
                windowBgColor = new Vector4(0.08f, 0.08f, 0.08f, 0.98f);
                headerColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
                frameBgColor = new Vector4(0.04f, 0.04f, 0.04f, 1.0f);
                break;

            default: // Modern Dark
                style.WindowRounding = 8.0f;
                style.FrameRounding = 5.0f;
                style.PopupRounding = 5.0f;
                style.FrameBorderSize = 1.0f;
                windowBgColor = new Vector4(0.12f, 0.14f, 0.18f, 0.95f);
                headerColor = new Vector4(0.15f, 0.25f, 0.40f, 1.0f);
                frameBgColor = new Vector4(0.08f, 0.10f, 0.14f, 1.0f);
                break;
        }

        Vector4 accentColor = config.ColorThemeIndex switch
        {
            1 => new Vector4(0.2f, 0.9f, 1.0f, 1.0f),
            2 => new Vector4(1.0f, 0.6f, 0.2f, 1.0f),
            3 => new Vector4(0.2f, 1.0f, 0.4f, 1.0f),
            _ => new Vector4(1.0f, 0.84f, 0.0f, 1.0f)
        };

        if (config.HighContrastMode)
        {
            accentColor = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            windowBgColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            headerColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
            frameBgColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        }

        ImGui.PushStyleColor(ImGuiCol.WindowBg, windowBgColor);
        ImGui.PushStyleColor(ImGuiCol.Header, headerColor);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, frameBgColor);
        ImGui.PushStyleColor(ImGuiCol.Button, headerColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(headerColor.X + 0.15f, headerColor.Y + 0.15f, headerColor.Z + 0.15f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, accentColor with { W = 1.0f });

        string modeLabel = config.CompactMode ? "🗖 Expand" : "🗕 Compact";
        if (ImGui.Button(modeLabel))
        {
            if (config.CompactMode)
                config.CompactWindowSize = ImGui.GetWindowSize();
            else
                config.FullWindowSize = ImGui.GetWindowSize();

            config.CompactMode = !config.CompactMode;

            var targetSize = config.CompactMode 
                ? new Vector2(Math.Max(config.CompactWindowSize.X, 240), Math.Max(config.CompactWindowSize.Y, 260))
                : config.FullWindowSize;

            ImGui.SetWindowSize(targetSize);
            pluginInstance.SaveConfig();
        }

        if (config.CompactMode)
        {
            DrawCompactUI(accentColor, fontScale);
            ImGui.PopStyleColor(6);
            return;
        }

        if (config.ShowBuffSynergy)
        {
            ImGui.BeginChild("BuffCard", new Vector2(0, 42), true, ImGuiWindowFlags.NoScrollbar);
            
            bool hasConsumable = cachedHasMedicatedBuff || cachedHasManualBuff;

            // FC is shown in the tooltip only and never changes this result.
            if (hasConsumable)
            {
                ImGui.TextColored(config.HighContrastMode ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f) : new Vector4(0.2f, 1.0f, 0.4f, 1.0f), "✨ Synergy: OPTIMAL (Consumable Active)");
            }
            else
            {
                ImGui.TextColored(config.HighContrastMode ? new Vector4(1.0f, 0.0f, 0.0f, 1.0f) : new Vector4(1.0f, 0.2f, 0.2f, 1.0f), "❌ Synergy: NONE");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Spiritbond Buff Status:");
                ImGui.Separator();
                string medicatedTime = cachedHasMedicatedBuff ? $"({Math.Ceiling(cachedMedicatedRemaining / 60f):F0}m left)" : "";
                string manTime = cachedHasManualBuff ? $"({(int)(cachedManualRemaining / 60)}m left)" : "";
                ImGui.TextColored(cachedHasMedicatedBuff ? new Vector4(0, 1, 0, 1) : new Vector4(0.7f, 0.7f, 0.7f, 1), $"• Medicated (source unknown): {(cachedHasMedicatedBuff ? $"Active {medicatedTime}" : "None")}");
                ImGui.TextColored(cachedHasFcBuff ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1), $"• FC Buff (Company Action): {(cachedHasFcBuff ? "Active (+1 to +3)" : "Missing")}");
                ImGui.TextColored(cachedHasManualBuff ? new Vector4(0, 1, 0, 1) : new Vector4(0.7f, 0.7f, 0.7f, 1), $"• Manual (Squadron): {(cachedHasManualBuff ? $"Active {manTime}" : "None")}");
                ImGui.TextColored(cachedHasFoodBuff ? new Vector4(0, 1, 0, 1) : new Vector4(0.7f, 0.7f, 0.7f, 1), $"• Food (Optional): {(cachedHasFoodBuff ? "Active (+2)" : "None")}");
                
                ImGui.Separator();
                ImGui.TextDisabled("Detected Raw Statuses:");
                if (debugDetectedStatuses.Count > 0)
                {
                    foreach (var dbg in debugDetectedStatuses)
                    {
                        ImGui.TextDisabled($"  > {dbg}");
                    }
                }
                else
                {
                    ImGui.TextDisabled("  > None detected on player");
                }

                ImGui.EndTooltip();
            }

            if (cachedExpiringBuff)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1.0f, 0.3f, 0.3f, 1.0f), "⚠️ Expiring (< 2m)!");
            }

            // Single Smart Action Button
            //
            // Safety guard: never fire the Superior Spiritbonding Potion command if doing so
            // would overwrite a crafting/gathering consumable (they share the same Medicated
            // status slot) or if the player is currently on a Disciple of the Hand/Land job,
            // since the spiritbond potion has no purpose there and would just waste a charge
            // and clobber whatever craft/gathering buff is (or is about to be) active.
            bool isCraftingClass = IsCraftingClassActive();
            bool hasProtectedMedicated = cachedHasMedicatedBuff && cachedMedicatedRemaining > MedicatedOverwriteProtectionSeconds;
            bool blockSpiritbondPotion = isCraftingClass || hasProtectedMedicated;
            bool wouldNeedPotion = !cachedHasMedicatedBuff || cachedMedicatedRemaining < 600f;
            bool needsPotion = wouldNeedPotion && !blockSpiritbondPotion;
            bool potionBlockedWhileNeeded = wouldNeedPotion && blockSpiritbondPotion;

            bool needsManual = !cachedHasManualBuff || cachedManualRemaining < 600f;
            bool canUseAny = needsPotion || needsManual;

            ImGui.SameLine();
            if (!canUseAny)
            {
                ImGui.BeginDisabled();
                ImGui.SmallButton(potionBlockedWhileNeeded ? "🚫 Potion Blocked" : "✔️ Buffs Active (>10m)");
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered())
                {
                   if (potionBlockedWhileNeeded)
{
             string medicatedBlockReason = string.Empty;

if (hasProtectedMedicated)
{
    string medicatedMinutes =
        Math.Ceiling(cachedMedicatedRemaining / 60f).ToString("F0");

    medicatedBlockReason =
        "- A Medicated effect has " +
        medicatedMinutes +
        "m remaining (protected for >5m)\n";
}

string craftingBlockReason = isCraftingClass
    ? "- Current job is a Disciple of the Hand\n"
    : string.Empty;

ImGui.SetTooltip(
    "Superior Spiritbonding Potion was NOT applied because:\n" +
    medicatedBlockReason +
    craftingBlockReason +
    "Switch to a non-crafting job and wait until the protected Medicated effect has 5 minutes or less remaining."
);
}
else
{
    ImGui.SetTooltip("All Spiritbond consumables have more than 10 minutes remaining.");
}
                }
            }
            else
            {
                string btnLabel = needsPotion ? "🧪 Apply Potion" : "📖 Apply Manual";
                if (needsPotion && needsManual) btnLabel = "⚡ Apply All Buffs";

               if (ImGui.SmallButton(btnLabel))
{
    if (needsPotion)
    {
        TryUseSpiritbondPotion();
    }

    if (needsManual)
    {
        // Jeżeli używamy też potiona, manual musi poczekać, ponieważ
        // dwa /item w tym samym frame nie są wykonywane niezawodnie.
        if (needsPotion)
        {
            pendingSquadronManualUse = true;
            pendingSquadronManualUseAt = DateTime.UtcNow.AddSeconds(2.5);
        }
        else
        {
            TryUseSquadronManual();
        }
    }
}

                if (ImGui.IsItemHovered())
                {
                    string potionStatusText = needsPotion
                        ? "Needs refresh"
                        : (potionBlockedWhileNeeded
                            ? (isCraftingClass ? "Blocked (Disciple of the Hand/Land)" : $"Blocked (Medicated protected: {Math.Ceiling(cachedMedicatedRemaining / 60f):F0}m left)")
                            : "Protected (>10m)");

                    ImGui.SetTooltip($"Uses missing/expiring items (< 10m):\n" +
                                     $"- Potion: {potionStatusText}\n" +
                                     $"- Manual: {(needsManual ? "Needs refresh" : "Protected (>10m)")}");
                }
            }

            ImGui.EndChild();
        }

        if (config.ShowDutyAdvisor)
        {
            string recommendedDuty = GetRandomizedDutyRecommendation(cachedAvgIvl);

            ImGui.BeginChild("AdvisorCard", new Vector2(0, 50), true, ImGuiWindowFlags.NoScrollbar);
            ImGui.TextColored(accentColor, "💡 Smart Duty Advisor:");
            ImGui.SameLine();
            ImGui.Text(recommendedDuty);
            ImGui.SameLine();
            if (ImGui.Button("🔄 Roll"))
            {
                GetRandomizedDutyRecommendation(cachedAvgIvl, true);
            }

            ImGui.EndChild();
        }

        if (config.ShowCappedSlotsCounter)
        {
            var gearListPreview = cachedGearList;
            int totalCount = gearListPreview.Count;

            ImGui.BeginChild("CounterCard", new Vector2(0, 35), true, ImGuiWindowFlags.NoScrollbar);
            ImGui.Text($"Ready for Extraction (Capped Slots): ");
            ImGui.SameLine();
            ImGui.TextColored(cachedCappedCount == totalCount ? new Vector4(0.0f, 1.0f, 0.5f, 1.0f) : accentColor, $"{cachedCappedCount} / {totalCount}");
            ImGui.EndChild();
        }

        if (inDuty)
            ImGui.Text($"📍 Current Duty: {currentDutyName} (Est. iLvl: {currentDutyLevel})");
        else
            ImGui.Text($"📍 Location: {currentDutyName} (Not in a duty)");

        ImGui.Spacing();

        if (ImGui.Button("History")) isHistoryWindowVisible = !isHistoryWindowVisible;
        ImGui.SameLine();
        if (ImGui.Button("Statistics")) isStatsWindowVisible = !isStatsWindowVisible;
        ImGui.SameLine();
        if (ImGui.Button("Save Session"))
        {
            SaveSessionToHistory();
            chatGui.Print("[Spiritbond Tracker] Current session saved to history!");
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset Baseline"))
        {
            dutyStartSpiritbond.Clear();
            previousSlotSpiritbond.Clear();
            notifiedCappedSlots.Clear();
            trackedItemIds.Clear();
        }

        ImGui.SameLine();
        if (ImGui.Button("Extract Materia"))
        {
            unsafe
            {
                var am = ActionManager.Instance();
                if (am != null)
                {
                    am->UseAction(ActionType.GeneralAction, 14);
                }
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("⚙ Settings"))
        {
            pluginInstance.ToggleSettingsWindow();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text("Equipped Gear & Real-Time Progress in Current Duty:");

        ImGui.PushStyleColor(ImGuiCol.Header, headerColor);
        ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(frameBgColor.X * 1.2f, frameBgColor.Y * 1.2f, frameBgColor.Z * 1.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, frameBgColor);

        var gearList = cachedGearList;
        int columnCount = 4 + (config.ShowEligibilityColumn ? 1 : 0);
        float gainColWidth = 85f * fontScale;

        if (ImGui.BeginTable("GearTable", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 110f * fontScale);
            ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch);
            if (config.ShowEligibilityColumn)
                ImGui.TableSetupColumn("Eligibility", ImGuiTableColumnFlags.WidthFixed, 110f * fontScale);
            ImGui.TableSetupColumn("Spiritbond", ImGuiTableColumnFlags.WidthFixed, 100f * fontScale);
            ImGui.TableSetupColumn("Gain in Duty", ImGuiTableColumnFlags.WidthFixed, gainColWidth);
            ImGui.TableHeadersRow();

            foreach (var gear in gearList)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(gear.SlotName);

                ImGui.TableNextColumn();
                string displayName = config.ShowItemLevel ? $"{gear.ItemName} (i{gear.ItemLevel})" : gear.ItemName;
                ImGui.TextColored(accentColor, displayName);

                if (config.ShowEligibilityColumn)
                {
                    ImGui.TableNextColumn();
                    float gainAlpha = 1.0f;
                    if (config.EnableAnimations && gear.GainedInDuty > 0f)
                    {
                        gainAlpha = 0.7f + 0.3f * (float)Math.Sin(ImGui.GetTime() * 5.0f);
                    }

                    ImGui.TextColored(gear.EligibilityColor with { W = gainAlpha }, gear.Eligibility);
                }

                ImGui.TableNextColumn();
                if (gear.CurrentPercent >= 100f)
                {
                    float capAlpha = config.EnableAnimations ? (0.6f + 0.4f * (float)Math.Sin(ImGui.GetTime() * 8.0f)) : 1.0f;
                    ImGui.TextColored((config.HighContrastMode ? new Vector4(1.0f, 1.0f, 0.0f, 1.0f) : new Vector4(1.0f, 0.8f, 0.2f, 1.0f)) with { W = capAlpha }, "100% (Capped!)");
                }
                else
                {
                    ImGui.Text($"{gear.CurrentPercent:F2}%");
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(config.HighContrastMode ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f) : new Vector4(0.0f, 1.0f, 0.5f, 1.0f), gear.GainedInDuty > 0 ? $"+{gear.GainedInDuty:F2}%" : "0.00%");
            }

            ImGui.EndTable();
        }
        ImGui.PopStyleColor(3);
        ImGui.PopStyleColor(6);

        if (isHistoryWindowVisible)
        {
            ImGui.SetNextWindowSize(new Vector2(950, 520), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Spiritbond Past History", ref isHistoryWindowVisible, ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.TextColored(accentColor, "Duty & Session Progress History (Filtered)");
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.SetNextItemWidth(300f);
                ImGui.InputText("##HistorySearch", ref historySearchFilter, 100);
                ImGui.SameLine();
                ImGui.Text("🔍 Search (Duty / Item / Job)");

                ImGui.Spacing();
                string[] catFilters = { "All", "Weapon", "Armor", "Accessory" };
                int currentCatIndex = Array.IndexOf(catFilters, historyCategoryFilter);
                if (currentCatIndex < 0) currentCatIndex = 0;
                ImGui.SetNextItemWidth(200f);
                if (ImGui.Combo("Category Filter", ref currentCatIndex, catFilters, catFilters.Length))
                {
                    historyCategoryFilter = catFilters[currentCatIndex];
                }

                ImGui.Spacing();
                ImGui.Separator();

                var filteredRecords = completedHistory.AsEnumerable();
                if (!string.IsNullOrEmpty(historySearchFilter))
                {
                    filteredRecords = filteredRecords.Where(h => h.DutyName.Contains(historySearchFilter, StringComparison.OrdinalIgnoreCase) ||
                        h.ItemName.Contains(historySearchFilter, StringComparison.OrdinalIgnoreCase) ||
                        h.JobName.Contains(historySearchFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (historyCategoryFilter != "All")
                {
                    filteredRecords = filteredRecords.Where(h => h.Category.Equals(historyCategoryFilter, StringComparison.OrdinalIgnoreCase));
                }

                var grouped = filteredRecords.GroupBy(h => h.SessionId).OrderByDescending(g => g.Max(x => x.Timestamp));
                foreach (var group in grouped)
                {
                    float totalGroupGain = group.Sum(x => x.Gained);
                    string dutyTitle = group.First().DutyName;
                    string sessionDate = group.First().Timestamp.ToString("yyyy-MM-dd HH:mm");
                    string jobUsed = group.First().JobName;
                    string cardHeader = $"[Duty] {dutyTitle} ({jobUsed}) --> Total Gain: +{totalGroupGain:F2}% ({sessionDate})";

                    if (config.HistoryViewMode == 0)
                    {
                        if (ImGui.TreeNode(cardHeader))
                        {
                            foreach (var item in group)
                            {
                                ImGui.BulletText($"{item.ItemName} (i{item.ItemLevel}) [{item.Category}] on {item.JobName}: +{item.Gained:F2}%");
                            }

                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        if (ImGui.CollapsingHeader(cardHeader, ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            ImGui.Indent(10f);
                            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), $"Items progressed in this entry as [{jobUsed}]:");

                            // "yyyy-MM-dd HH:mm:ss" needs a fixed width; scale it with the UI font.
                            float dateColumnWidth = 190f * fontScale;
                            if (ImGui.BeginTable($"HistoryTable_{group.Key.GetHashCode()}", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollX))
                            {
                                ImGui.TableSetupColumn("Item Name", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 80f * fontScale);
                                ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, 70f * fontScale);
                                ImGui.TableSetupColumn("iLvl", ImGuiTableColumnFlags.WidthFixed, 50f * fontScale);
                                ImGui.TableSetupColumn("Gain", ImGuiTableColumnFlags.WidthFixed, 65f * fontScale);
                                ImGui.TableSetupColumn("Date & Time", ImGuiTableColumnFlags.WidthFixed, dateColumnWidth);
                                ImGui.TableHeadersRow();
                                foreach (var item in group)
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.TextColored(accentColor, item.ItemName);
                                    ImGui.TableNextColumn();
                                    ImGui.Text(item.Category);
                                    ImGui.TableNextColumn();
                                    ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), item.JobName);
                                    ImGui.TableNextColumn();
                                    ImGui.Text($"i{item.ItemLevel}");
                                    ImGui.TableNextColumn();
                                    ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.5f, 1.0f), item.Gained > 0 ? $"+{item.Gained:F2}%" : "0.00%");
                                    ImGui.TableNextColumn();
                                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                                }

                                ImGui.EndTable();
                            }

                            ImGui.Unindent(10f);
                            ImGui.Spacing();
                        }
                    }
                }

                if (!grouped.Any())
                    ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), "No history entries match your search/filter criteria.");

                ImGui.End();
            }
        }

        if (isStatsWindowVisible)
        {
            ImGui.SetNextWindowSize(new Vector2(650, 480), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Spiritbond Statistics", ref isStatsWindowVisible, ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.TextColored(accentColor, "Advanced Spiritbond Analytics & Top Performance");
                ImGui.Separator();

                if (ImGui.Button("Export History to CSV")) ExportHistoryToCsv();

                ImGui.Spacing();
                ImGui.Separator();

                var now = DateTime.Now;
                float todayGain = completedHistory.Where(h => h.Timestamp.Date == now.Date).Sum(h => h.Gained);
                float weekGain = completedHistory.Where(h => h.Timestamp >= now.AddDays(-7)).Sum(h => h.Gained);
                float monthGain = completedHistory.Where(h => h.Timestamp >= now.AddDays(-30)).Sum(h => h.Gained);

                ImGui.Text($"Today's Gain: "); ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.5f, 1.0f), $"+{todayGain:F2}%");

                ImGui.Text($"Last 7 Days Gain: "); ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.5f, 1.0f), $"+{weekGain:F2}%");

                ImGui.Text($"Last 30 Days Gain: "); ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.5f, 1.0f), $"+{monthGain:F2}%");

                ImGui.Spacing();
                ImGui.Separator();

                var topDuty = completedHistory
                    .GroupBy(h => h.DutyName)
                    .Select(g => new { Duty = g.Key, TotalGain = g.Sum(x => x.Gained), SessionsCount = g.Select(x => x.SessionId).Distinct().Count() })
                    .OrderByDescending(x => x.TotalGain)
                    .FirstOrDefault();

                if (topDuty != null)
                {
                    ImGui.Text($"Top Yielding Duty (History): "); ImGui.SameLine();
                    ImGui.TextColored(accentColor, $"{topDuty.Duty} (Total: +{topDuty.TotalGain:F2}% across {topDuty.SessionsCount} runs)");
                }
                else
                {
                    ImGui.Text("Top Yielding Duty (History): No data recorded yet");
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Text("Average Gain per Category:");

                var categoryAvg = completedHistory
                    .GroupBy(h => h.Category)
                    .Select(g => new { Category = g.Key, AvgGain = g.Average(x => x.Gained) });

                foreach (var cat in categoryAvg)
                {
                    ImGui.BulletText($"{cat.Category}: {cat.AvgGain:F2}% average gain per item instance");
                }

                ImGui.End();
            }
        }
    }
}
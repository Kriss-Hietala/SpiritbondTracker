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
        this.mainWindow.SaveSessionToHistory();

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
    private string historySessionTypeFilter = "All";
    private string historyPeriodFilter = "30d";
    private int historyPageIndex = 0;
    private const int HistoryPageSize = 25;

    private bool expandVisibleHistoryEntries = false;
    private bool collapseVisibleHistoryEntries = false;

    private bool isHistoryWindowVisible = false;
    private bool isStatsWindowVisible = false;
    private string historySearchFilter = string.Empty;
    private string historyCategoryFilter = "All";
    
    private string cachedRecommendedDuty = string.Empty;
    private float lastCheckedAvgIvl = 0f;
    private readonly Random randomRoller = new();
    private int ecoModeFrameCounter = 0;
    private bool wasAutoEcoActive = false;
    private const int SegmentInactivityMinutes = 15;
    private const int SegmentMinDurationMinutes = 3;

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

    private bool pendingSquadronManualUse = false;
    private DateTime pendingSquadronManualUseAt = DateTime.MinValue;

    private const float MedicatedOverwriteProtectionSeconds = 300f;
    private const uint MedicatedStatusId = 49;
    private const uint FcSpiritbondStatusId = 361;
    private const uint SquadronManualStatusId = 1083;
    private const uint SuperiorSpiritbondPotionBaseItemId = 27960;
    private const uint SquadronSpiritbondingManualItemId = 14951;

    private static bool IsExpiringTimedBuff(float remainingTime)
        => remainingTime > 0f && remainingTime < 120f;

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

    public enum ContentSessionKind { StandardDuty, FieldOperation, ExplorationZone, OpenWorld }

    public enum ActivitySegmentKind
    {
        Active,
        Inactive,
        JobChange,
    }

    public sealed class ActivitySegment
    {
        public ActivitySegmentKind Kind { get; set; }
        public string JobName { get; set; } = "Unknown";
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public float TotalGain { get; set; }
        public int ItemsProgressed { get; set; }
    }

    public sealed class SessionSummary
    {
        public string SessionId { get; set; } = string.Empty;
        public string ContentName { get; set; } = string.Empty;
        public string JobName { get; set; } = "Unknown";
        public ContentSessionKind Kind { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public int ItemsProgressed { get; set; }
        public float TotalGain { get; set; }
        public List<ActivitySegment> Segments { get; set; } = new();
    }

    public sealed class HistoryStore
    {
        public int SchemaVersion { get; set; } = 2;
        public List<HistoryRecord> ItemRecords { get; set; } = new();
        public List<SessionSummary> Sessions { get; set; } = new();
    }

    private const int MaxSavedSessions = 500;
    private List<HistoryRecord> completedHistory = new();
    private List<SessionSummary> completedSessions = new();
    private string legacyHistoryFilePath => Path.Combine(pluginInterface.GetPluginConfigDirectory(), "spiritbond_history.json");
    private string historyFilePath => Path.Combine(pluginInterface.GetPluginConfigDirectory(), "spiritbond_sessions.json");
    private string legacyBackupFilePath => Path.Combine(pluginInterface.GetPluginConfigDirectory(), "spiritbond_history.legacy.json");

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
                var store = JsonSerializer.Deserialize<HistoryStore>(File.ReadAllText(historyFilePath));
                if (store != null)
                {
                    completedHistory = store.ItemRecords ?? new List<HistoryRecord>();
                    completedSessions = store.Sessions ?? new List<SessionSummary>();
                    RebuildSessionSummaries();
                    SaveHistory();
                    return;
                }
            }
            if (File.Exists(legacyHistoryFilePath))
            {
                completedHistory = JsonSerializer.Deserialize<List<HistoryRecord>>(File.ReadAllText(legacyHistoryFilePath)) ?? new List<HistoryRecord>();
                if (!File.Exists(legacyBackupFilePath)) File.Copy(legacyHistoryFilePath, legacyBackupFilePath);
                RebuildSessionSummaries();
                SaveHistory();
                chatGui.Print($"[Spiritbond Tracker] History migrated: {completedSessions.Count} legacy sessions imported.");
            }
        }
        catch { completedHistory = new List<HistoryRecord>(); completedSessions = new List<SessionSummary>(); }
    }

    private static ContentSessionKind ClassifyContent(string name, bool isDuty)
    {
        if (name.Contains("Cosmic Exploration", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Sinus Ardorum", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Phaenna", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Oizys", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Auxesia", StringComparison.OrdinalIgnoreCase))
{
    return ContentSessionKind.ExplorationZone;
}
        if (name.Contains("Eureka", StringComparison.OrdinalIgnoreCase) || name.Contains("Bozja", StringComparison.OrdinalIgnoreCase) || name.Contains("Zadnor", StringComparison.OrdinalIgnoreCase) || name.Contains("Occult Crescent", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("South Horn", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("North Horn", StringComparison.OrdinalIgnoreCase)) return ContentSessionKind.FieldOperation;
        return isDuty ? ContentSessionKind.StandardDuty : ContentSessionKind.OpenWorld;
    }

private static string InferSessionJobName(IEnumerable<HistoryRecord> records)
{
    // Jeśli w historii są różne joby, wybierz ten, który pojawia się
    // najczęściej, preferując takie, które nie są "Unknown".
    var best = records
        .Select(r => string.IsNullOrEmpty(r.JobName) ? "Unknown" : r.JobName)
        .GroupBy(j => j)
        .Select(g => new { Job = g.Key, Count = g.Count() })
        .OrderByDescending(x => x.Count)
        .ThenBy(x => x.Job.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
        .FirstOrDefault();

    return best?.Job ?? "Unknown";
}
    private static string GetKindLabel(ContentSessionKind kind) => kind switch
    {
        ContentSessionKind.StandardDuty     => "Standard Duty",
        ContentSessionKind.FieldOperation   => "Field Ops",
        ContentSessionKind.ExplorationZone  => "Exploration",
        ContentSessionKind.OpenWorld        => "Open World",
        _                                   => "Unknown",
    };

   private void RebuildSessionSummaries()
{
    var grouped = completedHistory
        .GroupBy(x => x.SessionId)
        .OrderByDescending(g => g.Max(x => x.Timestamp));

    var summaries = new List<SessionSummary>();

    foreach (var g in grouped)
    {
        var first = g.First();

        // NOWE: wybierz "najbardziej typowy" job dla sesji
        string sessionJobName = InferSessionJobName(g);

        var summary = new SessionSummary
        {
            SessionId       = g.Key,
            ContentName     = first.DutyName,
            JobName         = sessionJobName,
            Kind            = ClassifyContent(first.DutyName, !first.DutyName.Contains("Overworld", StringComparison.OrdinalIgnoreCase)),
            StartedAt       = g.Min(x => x.Timestamp),
            EndedAt         = g.Max(x => x.Timestamp),
            ItemsProgressed = g.Count(),
            TotalGain       = g.Sum(x => x.Gained),
            Segments        = new List<ActivitySegment>(),
        };

        if (summary.Kind == ContentSessionKind.FieldOperation ||
            summary.Kind == ContentSessionKind.ExplorationZone)
        {
            BuildSegmentsForSession(summary, g.OrderBy(x => x.Timestamp).ToList());
        }

        summaries.Add(summary);
    }

    completedSessions = summaries;
}

    private void BuildSegmentsForSession(SessionSummary summary, List<HistoryRecord> records)
    {
        if (records.Count == 0) return;

        DateTime segStart = records[0].Timestamp;
        DateTime lastTime = records[0].Timestamp;
        string segJob = records[0].JobName;
        float segGain = 0f;
        int segItems = 0;

        void CloseSegment(ActivitySegmentKind kind, DateTime endTime)
        {
            var durationMinutes = (endTime - segStart).TotalMinutes;
            if (durationMinutes < SegmentMinDurationMinutes && kind == ActivitySegmentKind.Inactive)
                return;

            summary.Segments.Add(new ActivitySegment
            {
                Kind = kind,
                JobName = segJob,
                StartedAt = segStart,
                EndedAt = endTime,
                TotalGain = segGain,
                ItemsProgressed = segItems,
            });

            segStart = endTime;
            segGain = 0f;
            segItems = 0;
        }

        foreach (var r in records)
        {
            var gapMinutes = (r.Timestamp - lastTime).TotalMinutes;
            bool jobChanged = !string.Equals(r.JobName, segJob, StringComparison.OrdinalIgnoreCase);

            if (gapMinutes >= SegmentInactivityMinutes)
            {
                CloseSegment(ActivitySegmentKind.Active, lastTime);
                CloseSegment(ActivitySegmentKind.Inactive, r.Timestamp);
            }
            else if (jobChanged)
            {
                CloseSegment(ActivitySegmentKind.Active, lastTime);
                segJob = r.JobName;
            }

            segGain += r.Gained;
            segItems += 1;
            lastTime = r.Timestamp;
        }

        CloseSegment(ActivitySegmentKind.Active, lastTime);
    }

    private void ApplyHistoryRetention()
    {
        RebuildSessionSummaries();
        if (completedSessions.Count <= MaxSavedSessions) return;
        var keep = completedSessions.Take(MaxSavedSessions).Select(x => x.SessionId).ToHashSet();
        completedHistory = completedHistory.Where(x => keep.Contains(x.SessionId)).ToList();
        RebuildSessionSummaries();
    }

    private void SaveHistory()
    {
        try
        {
            string dir = pluginInterface.GetPluginConfigDirectory();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            ApplyHistoryRetention();
            File.WriteAllText(historyFilePath, JsonSerializer.Serialize(new HistoryStore { ItemRecords = completedHistory, Sessions = completedSessions }, new JsonSerializerOptions { WriteIndented = true }));
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

                if (item->ItemId != baseItemId) continue;

                uint actionItemId = item->ItemId;
                if (item->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality))
                {
                    actionItemId += 1_000_000;
                }

                location = new InventoryItemLocation(actionItemId, inventoryType, (uint)slot);
                return true;
            }
        }

        return false;
    }

    private unsafe bool TryUseInventoryItem(InventoryItemLocation item)
    {
        var inventoryContext = AgentInventoryContext.Instance();
        if (inventoryContext == null)
        {
            chatGui.Print("[Spiritbond Tracker] Inventory context is unavailable.");
            return false;
        }

        long result = inventoryContext->UseItem(item.ItemId);
        return result == 0;
    }

    private unsafe bool TryUseSpiritbondPotion()
    {
        if (!TryFindItemInNormalInventory(SuperiorSpiritbondPotionBaseItemId, out var potion))
        {
            chatGui.Print("[Spiritbond Tracker] Superior Spiritbond Potion was not found in normal inventory.");
            return false;
        }

        return TryUseInventoryItem(potion);
    }

    private unsafe bool TryUseSquadronManual()
    {
        if (!TryFindItemInNormalInventory(SquadronSpiritbondingManualItemId, out var manual))
        {
            chatGui.Print("[Spiritbond Tracker] Squadron Spiritbonding Manual was not found in normal inventory.");
            return false;
        }

        return TryUseInventoryItem(manual);
    }

    public override unsafe void Update()
    {
        if (objectTable.LocalPlayer == null) return;
        
        if (pendingSquadronManualUse && DateTime.UtcNow >= pendingSquadronManualUseAt)
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

string detectedDutyName = $"Territory #{territoryId}";
uint detectedDutyLevel = 0;

var territory = dataManager.GetExcelSheet<TerritoryRow>()?.GetRowOrDefault(territoryId);

if (territory.HasValue)
{
    string placeName = territory.Value.PlaceName.Value.Name.ToString();

    detectedDutyName = !string.IsNullOrEmpty(placeName)
        ? placeName
        : $"Territory #{territoryId}";

    var cfc = territory.Value.ContentFinderCondition.Value;

    if (cfc.RowId != 0)
    {
        // Dla heurystyki spiritbondu preferujemy wymagany iLvl wejścia.
        // iLvl sync jest używany wyłącznie jako fallback, gdy entry iLvl nie istnieje.
        detectedDutyLevel = cfc.ItemLevelRequired > 0
            ? cfc.ItemLevelRequired
            : (cfc.ItemLevelSync > 0
                ? cfc.ItemLevelSync
                : cfc.ClassJobLevelRequired);
    }
}
else
{
    detectedDutyName = "Overworld / Field Ops";
    detectedDutyLevel = 0;
}

string detectedSessionKey = $"{detectedDutyName}_{inDuty}_{territoryId}";

if (lastTrackedDuty != detectedSessionKey)
{
    // Ważne: zapis starej sesji następuje zanim nadpiszemy currentDutyName.
    // Dzięki temu opuszczona planeta Cosmic Exploration zapisze się jako Oizys,
    // Phaenna, Sinus Ardorum albo Auxesia.
    SaveSessionToHistory();

    dutyStartSpiritbond.Clear();
    previousSlotSpiritbond.Clear();
    notifiedCappedSlots.Clear();
    trackedItemIds.Clear();

    currentDutyName = detectedDutyName;
    currentDutyLevel = detectedDutyLevel;
    lastTrackedDuty = detectedSessionKey;

    currentSessionId =
        $"{currentDutyName} ({DateTime.Now:yyyy-MM-dd HH:mm:ss})";
}
else
{
    // Odśwież dane bieżącej strefy bez rozpoczynania nowej sesji.
    currentDutyName = detectedDutyName;
    currentDutyLevel = detectedDutyLevel;
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
                dutyStartSpiritbond.Remove(slot.Index);
                previousSlotSpiritbond.Remove(slot.Index);
                notifiedCappedSlots.Remove(slot.Index);
            }
            trackedItemIds[slot.Index] = item->ItemId;

            if (!dutyStartSpiritbond.ContainsKey(slot.Index))
                dutyStartSpiritbond[slot.Index] = currentSb;

            if (!previousSlotSpiritbond.TryGetValue(slot.Index, out ushort previousSb))
            {
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
                        else if (status.StatusId == FcSpiritbondStatusId)
                        {
                            cachedHasFcBuff = true;
                            debugDetectedStatuses.Add($"FC Spiritbond Action (display only): '{bName}' (ID: {status.StatusId}, Rem: {status.RemainingTime:F1}s)");
                        }
                        else if (status.StatusId == SquadronManualStatusId)
                        {
                            cachedHasManualBuff = status.RemainingTime > 0f;
                            cachedManualRemaining = status.RemainingTime;
                            debugDetectedStatuses.Add($"Squadron Manual: '{bName}' (ID: {status.StatusId}, Rem: {status.RemainingTime:F1}s)");

                            if (config.WarnExpiringBuffs && IsExpiringTimedBuff(status.RemainingTime))
                                cachedExpiringBuff = true;
                        }
                        else if (status.StatusId == MedicatedStatusId)
                        {
                            cachedHasMedicatedBuff = status.RemainingTime > 0f;
                            cachedMedicatedRemaining = status.RemainingTime;
                            debugDetectedStatuses.Add($"Medicated (source unknown): '{bName}' (ID: {status.StatusId}, Rem: {status.RemainingTime:F1}s)");
                        }
                    }
                }
            }

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
                eligibility = "Gaining";
                eligColor = config.HighContrastMode ? new Vector4(0.0f, 1.0f, 1.0f, 1.0f) : new Vector4(0.2f, 1.0f, 0.5f, 1.0f);
            }
            else if (currentDutyLevel > 0)
            {
               if (cachedData.ItemLevel >= currentDutyLevel + 70)
{
    eligibility = "No Gain";
    eligColor = config.HighContrastMode
        ? new Vector4(1.0f, 0.0f, 0.0f, 1.0f)
        : new Vector4(1.0f, 0.2f, 0.2f, 1.0f);
}
else if (cachedData.ItemLevel > currentDutyLevel + 35)
{
    eligibility = "Reduced";
    eligColor = config.HighContrastMode
        ? new Vector4(1.0f, 1.0f, 0.0f, 1.0f)
        : new Vector4(1.0f, 0.8f, 0.2f, 1.0f);
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
            case 1:
                style.WindowRounding = 1.0f;
                style.FrameRounding = 0.0f;
                style.PopupRounding = 1.0f;
                style.FrameBorderSize = 2.0f;
                windowBgColor = new Vector4(0.04f, 0.06f, 0.18f, 0.98f);
                headerColor = new Vector4(0.08f, 0.12f, 0.35f, 1.0f);
                frameBgColor = new Vector4(0.02f, 0.03f, 0.10f, 1.0f);
                break;

            case 2:
                style.WindowRounding = 8.0f;
                style.FrameRounding = 6.0f;
                style.PopupRounding = 6.0f;
                style.FrameBorderSize = 1.0f;
                windowBgColor = new Vector4(0.18f, 0.19f, 0.21f, 0.95f);
                headerColor = new Vector4(0.28f, 0.30f, 0.33f, 1.0f);
                frameBgColor = new Vector4(0.12f, 0.13f, 0.15f, 1.0f);
                break;

            case 3:
                style.WindowRounding = 4.0f;
                style.FrameRounding = 3.0f;
                style.PopupRounding = 3.0f;
                style.FrameBorderSize = 1.0f;
                windowBgColor = new Vector4(0.08f, 0.08f, 0.08f, 0.98f);
                headerColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
                frameBgColor = new Vector4(0.04f, 0.04f, 0.04f, 1.0f);
                break;

            default:
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
                            string medicatedMinutes = Math.Ceiling(cachedMedicatedRemaining / 60f).ToString("F0");
                            medicatedBlockReason = "- A Medicated effect has " + medicatedMinutes + "m remaining (protected for >5m)\n";
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
            ImGui.Begin("Spiritbond Past History###SpiritbondHistory", ImGuiWindowFlags.None);

            ImGui.TextColored(
                new Vector4(0.8f, 0.8f, 1.0f, 1.0f),
                "Spiritbond Past History"
            );
            ImGui.Separator();

            ImGui.SetNextItemWidth(300f);
            ImGui.InputText("##HistorySearch", ref historySearchFilter, 100);
            ImGui.SameLine();
            ImGui.Text("🔍 Search (Duty / Job)");

            ImGui.Spacing();

            string[] kindFilters = { "All", "Standard Duty", "Field Ops", "Exploration", "Open World" };
            int currentKindIndex = Array.IndexOf(kindFilters, historySessionTypeFilter);
            if (currentKindIndex < 0) currentKindIndex = 0;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.Combo("Content Type", ref currentKindIndex, kindFilters, kindFilters.Length))
            {
                historySessionTypeFilter = kindFilters[currentKindIndex];
            }

            string[] periodFilters = { "All", "30d", "7d", "Today" };
            int currentPeriodIndex = Array.IndexOf(periodFilters, historyPeriodFilter);
            if (currentPeriodIndex < 0) currentPeriodIndex = 0;
            ImGui.SetNextItemWidth(150f);
            ImGui.SameLine();
            if (ImGui.Combo("Period", ref currentPeriodIndex, periodFilters, periodFilters.Length))
            {
                historyPeriodFilter = periodFilters[currentPeriodIndex];
            }

            ImGui.Spacing();
            ImGui.Separator();

            var sessions = completedSessions.AsEnumerable();

            if (!string.IsNullOrEmpty(historySearchFilter))
            {
                sessions = sessions.Where(s =>
                    s.ContentName.Contains(historySearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    s.JobName.Contains(historySearchFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (historySessionTypeFilter != "All")
            {
                sessions = sessions.Where(s => GetKindLabel(s.Kind) == historySessionTypeFilter);
            }

           var historyNow = DateTime.Now;
                sessions = historyPeriodFilter switch
                {
                    "Today" => sessions.Where(s => s.EndedAt.Date == historyNow.Date),
                    "7d"    => sessions.Where(s => s.EndedAt >= historyNow.AddDays(-7)),
                    "30d"   => sessions.Where(s => s.EndedAt >= historyNow.AddDays(-30)),
                    _       => sessions,
                };

            sessions = sessions.OrderByDescending(s => s.EndedAt);

            int totalSessions = sessions.Count();
            int totalPages = (int)Math.Ceiling(totalSessions / (double)HistoryPageSize);
            historyPageIndex = Math.Clamp(historyPageIndex, 0, Math.Max(totalPages - 1, 0));
            sessions = sessions
                .Skip(historyPageIndex * HistoryPageSize)
                .Take(HistoryPageSize);

            var sessionList = sessions.ToList();

if (ImGui.SmallButton("Expand visible"))
{
    expandVisibleHistoryEntries = true;
    collapseVisibleHistoryEntries = false;
}

ImGui.SameLine();

if (ImGui.SmallButton("Collapse visible"))
{
    collapseVisibleHistoryEntries = true;
    expandVisibleHistoryEntries = false;
}

ImGui.SameLine();
ImGui.TextDisabled($"({sessionList.Count} shown)");

if (!sessionList.Any())
{
                ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f),
                    "No history entries match your search/filter criteria.");
            }
            else
            {
        
    foreach (var s in sessionList)
    {
        string kindLabel = GetKindLabel(s.Kind);

        // Liczenie czasu trwania sesji (hh:mm) – tu definiujemy zmienną duration
        string duration = s.EndedAt > s.StartedAt
            ? $"{(s.EndedAt - s.StartedAt):hh\\:mm}"
            : "—";

        // Ładniejszy opis joba: Mixed jobs zamiast Unknown
        string displayJob = s.JobName.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? "Mixed jobs"
            : s.JobName;

        string cardHeader =
            $"[{kindLabel}] {s.ContentName} ({displayJob}) " +
            $"→ +{s.TotalGain:F2}% · {s.ItemsProgressed} items · {duration} " +
            $"({s.EndedAt:yyyy-MM-dd HH:mm})";

        var items = completedHistory
            .Where(h => h.SessionId == s.SessionId)
            .OrderBy(h => h.Timestamp)
            .ToList();
            if (expandVisibleHistoryEntries)
            {
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);
            }
            else if (collapseVisibleHistoryEntries)
            {
            ImGui.SetNextItemOpen(false, ImGuiCond.Always);
            }


        if (config.HistoryViewMode == 0)
        {
            // Minimal – drzewko
            if (ImGui.TreeNode(cardHeader))
            {
                foreach (var item in items)
                {
                    ImGui.BulletText(
                        $"{item.ItemName} (i{item.ItemLevel}) [{item.Category}] " +
                        $"on {item.JobName}: +{item.Gained:F2}%"
                    );
                }

                ImGui.TreePop();
            }
            
        }
        else
        {
            // Advanced Cards – tabela
            if (ImGui.CollapsingHeader(cardHeader, ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent(10f);
    if ((s.Kind == ContentSessionKind.FieldOperation ||
     s.Kind == ContentSessionKind.ExplorationZone) &&
    s.Segments != null &&
    s.Segments.Count > 0)
{
    bool openSegmentsByDefault = s.Segments.Count <= 5;

    ImGui.TextColored(
        new Vector4(0.8f, 0.8f, 0.8f, 1.0f),
        $"Activity segments: {s.Segments.Count}" +
        (openSegmentsByDefault ? string.Empty : " (click to expand)"));

    ImGui.SetNextItemOpen(openSegmentsByDefault, ImGuiCond.FirstUseEver);

    if (ImGui.TreeNode($"Show activity segments##Segments_{s.SessionId.GetHashCode()}"))
    {
       float segmentTableHeight = Math.Min(
    220f * fontScale,
    28f * fontScale + (s.Segments.Count * 23f * fontScale)
);

if (ImGui.BeginTable(
    $"SegmentTable_{s.SessionId.GetHashCode()}",
    5,
    ImGuiTableFlags.Borders |
    ImGuiTableFlags.RowBg |
    ImGuiTableFlags.Resizable |
    ImGuiTableFlags.ScrollY,
    new Vector2(0f, segmentTableHeight)))
{
    ImGui.TableSetupColumn(
        "Type",
        ImGuiTableColumnFlags.WidthFixed,
        92f * fontScale);

    ImGui.TableSetupColumn(
        "Job",
        ImGuiTableColumnFlags.WidthFixed,
        90f * fontScale);

    ImGui.TableSetupColumn(
        "Duration",
        ImGuiTableColumnFlags.WidthFixed,
        82f * fontScale);

    ImGui.TableSetupColumn(
        "Total Gain",
        ImGuiTableColumnFlags.WidthFixed,
        95f * fontScale);

    ImGui.TableSetupColumn(
        "Items",
        ImGuiTableColumnFlags.WidthFixed,
        60f * fontScale);

    ImGui.TableSetupScrollFreeze(0, 1);
    ImGui.TableHeadersRow();

    foreach (var seg in s.Segments)
    {
        string segmentLabel = seg.Kind switch
        {
            ActivitySegmentKind.Active => "Active",
            ActivitySegmentKind.Inactive => "Inactive",
            ActivitySegmentKind.JobChange => "Job change",
            _ => "Unknown",
        };

        Vector4 segmentColor = seg.Kind switch
        {
            ActivitySegmentKind.Active =>
                new Vector4(0.25f, 0.95f, 0.55f, 1.0f),

            ActivitySegmentKind.Inactive =>
                new Vector4(1.0f, 0.75f, 0.25f, 1.0f),

            ActivitySegmentKind.JobChange =>
                new Vector4(0.35f, 0.75f, 1.0f, 1.0f),

            _ =>
                new Vector4(0.75f, 0.75f, 0.75f, 1.0f),
        };

        TimeSpan segmentDuration = seg.EndedAt > seg.StartedAt
            ? seg.EndedAt - seg.StartedAt
            : TimeSpan.Zero;

        string segmentDurationText = segmentDuration > TimeSpan.Zero
            ? $"{(int)segmentDuration.TotalMinutes:00}:{segmentDuration.Seconds:00}"
            : "—";

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextColored(segmentColor, segmentLabel);

        ImGui.TableNextColumn();
        ImGui.Text(
            string.IsNullOrWhiteSpace(seg.JobName)
                ? "Unknown"
                : seg.JobName);

        ImGui.TableNextColumn();
        ImGui.Text(segmentDurationText);

        ImGui.TableNextColumn();
        ImGui.TextColored(
            new Vector4(0.0f, 1.0f, 0.5f, 1.0f),
            seg.TotalGain > 0f
                ? $"+{seg.TotalGain:F2}%"
                : "0.00%");

        ImGui.TableNextColumn();
        ImGui.Text(seg.ItemsProgressed.ToString());
    }

    ImGui.EndTable();
}

        ImGui.TreePop();
    }

    ImGui.Separator();
}
                ImGui.TextColored(
                    new Vector4(0.8f, 0.8f, 0.8f, 1.0f),
                    $"Items progressed in this session as [{displayJob}]:"
                );

                float dateColumnWidth = 190f * fontScale;

               float historyTableHeight = Math.Min(
    260f * fontScale,
    28f * fontScale + (items.Count * 23f * fontScale)
);

if (ImGui.BeginTable(
    $"HistoryTable_{s.SessionId.GetHashCode()}",
    6,
    ImGuiTableFlags.Borders
    | ImGuiTableFlags.RowBg
    | ImGuiTableFlags.Resizable
    | ImGuiTableFlags.ScrollX
    | ImGuiTableFlags.ScrollY,
    new Vector2(0f, historyTableHeight)))
{
                    ImGui.TableSetupColumn(
                        "Item Name",
                        ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn(
                        "Category",
                        ImGuiTableColumnFlags.WidthFixed, 80f * fontScale);
                    ImGui.TableSetupColumn(
                        "Job",
                        ImGuiTableColumnFlags.WidthFixed, 70f * fontScale);
                    ImGui.TableSetupColumn(
                        "iLvl",
                        ImGuiTableColumnFlags.WidthFixed, 50f * fontScale);
                    ImGui.TableSetupColumn(
                        "Gain",
                        ImGuiTableColumnFlags.WidthFixed, 65f * fontScale);
                    ImGui.TableSetupColumn(
                        "Date & Time",
                        ImGuiTableColumnFlags.WidthFixed, dateColumnWidth);

                    ImGui.TableHeadersRow();

                    foreach (var item in items)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.TextColored(accentColor, item.ItemName);

                        ImGui.TableNextColumn();
                        ImGui.Text(item.Category);

                        ImGui.TableNextColumn();
                        ImGui.TextColored(
                            new Vector4(0.4f, 0.8f, 1.0f, 1.0f),
                            item.JobName);

                        ImGui.TableNextColumn();
                        ImGui.Text($"i{item.ItemLevel}");

                        ImGui.TableNextColumn();
                        ImGui.TextColored(
                            new Vector4(0.0f, 1.0f, 0.5f, 1.0f),
                            item.Gained > 0 ? $"+{item.Gained:F2}%" : "0.00%");

                        ImGui.TableNextColumn();
                        ImGui.TextColored(
                            new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                            item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                    }

                    ImGui.EndTable();
                }

                ImGui.Unindent(10f);
                ImGui.Spacing();
            }
        }      
   
    }
            expandVisibleHistoryEntries = false;
            collapseVisibleHistoryEntries = false;
            }        
    

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text($"Page {historyPageIndex + 1}/{Math.Max(totalPages, 1)}");
            ImGui.SameLine();
            if (ImGui.Button("Prev") && historyPageIndex > 0) historyPageIndex--;
            ImGui.SameLine();
            if (ImGui.Button("Next") && historyPageIndex < totalPages - 1) historyPageIndex++;

            ImGui.End();
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

                var statsNow = DateTime.Now;
                float todayGain = completedHistory.Where(h => h.Timestamp.Date == statsNow.Date).Sum(h => h.Gained);
                float weekGain  = completedHistory.Where(h => h.Timestamp >= statsNow.AddDays(-7)).Sum(h => h.Gained);
                float monthGain = completedHistory.Where(h => h.Timestamp >= statsNow.AddDays(-30)).Sum(h => h.Gained);

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
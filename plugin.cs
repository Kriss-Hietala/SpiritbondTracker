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

// Aliasy do Lumina
using ItemRow = Lumina.Excel.Sheets.Item;
using TerritoryRow = Lumina.Excel.Sheets.TerritoryType;
using StatusRow = Lumina.Excel.Sheets.Status;

namespace SpiritbondTracker
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Spiritbond Tracker";

        private readonly IDalamudPluginInterface pluginInterface;
        private readonly ICommandManager commandManager;
        public readonly WindowSystem WindowSystem;
        private readonly SpiritbondWindow mainWindow;
        private readonly SettingsWindow settingsWindow;

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

            this.WindowSystem = new WindowSystem("SpiritbondTracker");
            this.mainWindow = new SpiritbondWindow(pluginInterface, objectTable, clientState, condition, dataManager, chatGui, config, this);
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
        public bool ShowBuffSynergy { get; set; } = true;
        public bool ShowEligibilityColumn { get; set; } = true;
        public bool ShowItemLevel { get; set; } = true;
        public bool PlaySoundNotification { get; set; } = true;

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
        : base("Spiritbond Tracker Settings###SpiritbondTrackerSettings", ImGuiWindowFlags.AlwaysAutoResize)
        {
            this.pluginInterface = pluginInterface;
            this.config = config;
            this.mainWindow = mainWindow;

            this.AllowPinning = true;
            this.AllowClickthrough = true;
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
            ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), "Spiritbond Tracker Customization & Performance");
            ImGui.Separator();
            ImGui.Spacing();

            bool showBuffSynergy = config.ShowBuffSynergy;
            bool showEligibilityColumn = config.ShowEligibilityColumn;
            bool showItemLevel = config.ShowItemLevel;
            bool playSoundNotification = config.PlaySoundNotification;
            bool showCappedSlotsCounter = config.ShowCappedSlotsCounter;
            bool warnExpiringBuffs = config.WarnExpiringBuffs;
            bool showDutyAdvisor = config.ShowDutyAdvisor;
            bool ecoMode = config.EcoMode;
            bool autoEcoFieldOps = config.AutoEcoFieldOps;
            bool enableAnimations = config.EnableAnimations;
            bool highContrastMode = config.HighContrastMode;
            float uiScale = config.UiScale;
            int layoutStyleIndex = config.UiLayoutStyleIndex;
            int themeIndex = config.ColorThemeIndex;
            int historyViewMode = config.HistoryViewMode;

            bool changed = false;

            ImGui.Text("General Visibility & Features:");
            if (ImGui.Checkbox("Show Buff Synergy Header", ref showBuffSynergy)) { config.ShowBuffSynergy = showBuffSynergy; changed = true; }
            if (ImGui.Checkbox("Show iLvl Eligibility Column", ref showEligibilityColumn)) { config.ShowEligibilityColumn = showEligibilityColumn; changed = true; }
            if (ImGui.Checkbox("Show Item Level next to Name", ref showItemLevel)) { config.ShowItemLevel = showItemLevel; changed = true; }
            if (ImGui.Checkbox("Play Sound / Chat Alert on 100% Capped", ref playSoundNotification)) { config.PlaySoundNotification = playSoundNotification; changed = true; }
            if (ImGui.Checkbox("Show Capped Slots Counter", ref showCappedSlotsCounter)) { config.ShowCappedSlotsCounter = showCappedSlotsCounter; changed = true; }
            if (ImGui.Checkbox("Warn about expiring Spiritbond buffs (< 2 mins)", ref warnExpiringBuffs)) { config.WarnExpiringBuffs = warnExpiringBuffs; changed = true; }
            if (ImGui.Checkbox("Show Smart Duty Advisor (Roulette & Randomizer)", ref showDutyAdvisor)) { config.ShowDutyAdvisor = showDutyAdvisor; changed = true; }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("Performance & Animations:");
            if (ImGui.Checkbox("Enable Eco Mode / Low-Overhead Safe Mode", ref ecoMode)) { config.EcoMode = ecoMode; changed = true; }
            if (ImGui.Checkbox("Auto-enable Eco Mode in Field Ops (Eureka, Bozja, Occult)", ref autoEcoFieldOps)) { config.AutoEcoFieldOps = autoEcoFieldOps; changed = true; }
            if (ImGui.Checkbox("Enable UI Animations", ref enableAnimations)) { config.EnableAnimations = enableAnimations; changed = true; }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("History UI Style:");
            string[] historyModes = { "Minimal (Compact list)", "Advanced Cards (Detailed duty & job breakdown)" };
            ImGui.SetNextItemWidth(250f);
            if (ImGui.Combo("History Layout", ref historyViewMode, historyModes, historyModes.Length))
            {
                config.HistoryViewMode = historyViewMode;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("UI Appearance & Themes:");

            if (ImGui.Checkbox("High Contrast Mode", ref highContrastMode)) { config.HighContrastMode = highContrastMode; changed = true; }

            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat("UI Font Scale", ref uiScale, 1.0f, 1.5f, "%.1fx"))
            {
                config.UiScale = uiScale;
                changed = true;
            }

            string[] layouts = { "Modern Dark (Default)", "Classic FF I-VI (Retro RPG)", "Mac OS X (Aquatic Minimal)", "Xbox 360 (Blade & Neon)" };
            ImGui.SetNextItemWidth(240f);
            if (ImGui.Combo("UI Element Layout Style", ref layoutStyleIndex, layouts, layouts.Length))
            {
                config.UiLayoutStyleIndex = layoutStyleIndex;
                changed = true;
            }

            string[] themes = { "Classic Blue", "Neon Cyan", "Warm Amber", "Matrix Emerald" };
            ImGui.SetNextItemWidth(240f);
            if (ImGui.Combo("Color Theme Palette", ref themeIndex, themes, themes.Length))
            {
                config.ColorThemeIndex = themeIndex;
                changed = true;
            }

            if (changed)
            {
                SaveConfig();
            }

            ImGui.Spacing();
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
        private bool buffWarningSent = false;

        private string cachedRecommendedDuty = string.Empty;
        private float lastCheckedAvgIvl = 0f;
        private readonly Random randomRoller = new();
        private int ecoModeFrameCounter = 0;
        private bool wasAutoEcoActive = false;

        private Dictionary<int, bool> notifiedCappedSlots = new();

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
        private string lastTrackedDuty = string.Empty;

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

        public class HistoryRecord
        {
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

        public SpiritbondWindow(IDalamudPluginInterface pluginInterface, IObjectTable objectTable, IClientState clientState, ICondition condition, IDataManager dataManager, IChatGui chatGui, PluginConfig config, Plugin pluginInstance)
        : base("Spiritbond Tracker###SpiritbondTrackerMain", ImGuiWindowFlags.NoScrollbar)
        {
            this.pluginInterface = pluginInterface;
            this.objectTable = objectTable;
            this.clientState = clientState;
            this.condition = condition;
            this.dataManager = dataManager;
            this.chatGui = chatGui;
            this.config = config;
            this.pluginInstance = pluginInstance;

            this.AllowPinning = true;
            this.AllowClickthrough = true;

            this.Size = new Vector2(880, 620);
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
                writer.WriteLine("DutyName,ItemName,Category,JobName,ItemLevel,GainedPercent,Timestamp");
                foreach (var h in completedHistory)
                {
                    writer.WriteLine($"\"{h.DutyName}\",\"{h.ItemName}\",\"{h.Category}\",\"{h.JobName}\",{h.ItemLevel},{h.Gained:F2},{h.Timestamp:yyyy-MM-dd HH:mm:ss}");
                }
                chatGui.Print($"[Spiritbond Tracker] History exported successfully to: {csvPath}");
            }
            catch (Exception ex)
            {
                chatGui.Print($"[Spiritbond Tracker] Failed to export CSV: {ex.Message}");
            }
        }

        public override unsafe void Update()
        {
            if (objectTable.LocalPlayer == null) return;

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

            if (lastTrackedDuty != currentDutyName)
            {
                SaveSessionToHistory();
                dutyStartSpiritbond.Clear();
                previousSlotSpiritbond.Clear();
                notifiedCappedSlots.Clear();
                lastTrackedDuty = currentDutyName;
            }

            foreach (var slot in Slots)
            {
                if (slot.Index >= equippedContainer->Size) continue;

                var item = equippedContainer->GetInventorySlot(slot.Index);
                if (item == null || item->ItemId == 0) continue;

                ushort currentSb = (ushort)item->SpiritbondOrCollectability;

                if (!dutyStartSpiritbond.ContainsKey(slot.Index))
                {
                    dutyStartSpiritbond[slot.Index] = currentSb;
                    previousSlotSpiritbond[slot.Index] = currentSb;
                }

                if (currentSb >= 10000 && config.PlaySoundNotification)
                {
                    if (!notifiedCappedSlots.TryGetValue(slot.Index, out var notified) || !notified)
                    {
                        notifiedCappedSlots[slot.Index] = true;
                        chatGui.Print($"[Spiritbond Tracker] Slot {slot.Name} reached 100% spiritbond! Ready for extraction.");
                    }
                }
            }
        }

        public unsafe void SaveSessionToHistory()
        {
            if (string.IsNullOrEmpty(lastTrackedDuty) || lastTrackedDuty == "Overworld / Field Ops") return;
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
                        var excelItem = dataManager.GetExcelSheet<ItemRow>()?.GetRowOrDefault(item->ItemId);
                        string realItemName = excelItem.HasValue ? excelItem.Value.Name.ToString() : $"Unknown ({item->ItemId})";
                        uint itemLevel = excelItem.HasValue ? excelItem.Value.LevelItem.RowId : 0;

                        completedHistory.Add(new HistoryRecord
                        {
                            DutyName = lastTrackedDuty,
                            ItemName = realItemName,
                            Category = slot.Category,
                            JobName = currentJobName,
                            ItemLevel = itemLevel,
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
                int diff = currentSb - startSb;
                if (diff < 0) diff = 0;

                float gainedInDuty = diff / 100f;

                var excelItem = dataManager.GetExcelSheet<ItemRow>()?.GetRowOrDefault(item->ItemId);
                string realItemName = excelItem.HasValue ? excelItem.Value.Name.ToString() : $"Unknown ({item->ItemId})";
                uint itemLevel = excelItem.HasValue ? excelItem.Value.LevelItem.RowId : 0;

                string eligibility = "Optimal";
                Vector4 eligColor = config.HighContrastMode ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f) : new Vector4(0.2f, 1.0f, 0.2f, 1.0f);

                if (gainedInDuty > 0f)
                {
                    eligibility = "Active Gain";
                    eligColor = config.HighContrastMode ? new Vector4(0.0f, 1.0f, 1.0f, 1.0f) : new Vector4(0.2f, 1.0f, 0.5f, 1.0f);
                }
                else if (currentDutyLevel > 0)
                {
                    if (itemLevel > currentDutyLevel + 50)
                    {
                        eligibility = "Too High";
                        eligColor = config.HighContrastMode ? new Vector4(1.0f, 0.0f, 0.0f, 1.0f) : new Vector4(1.0f, 0.2f, 0.2f, 1.0f);
                    }
                    else if (itemLevel > currentDutyLevel + 25)
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
                    ItemName = realItemName,
                    ItemLevel = itemLevel,
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

                var excelItem = dataManager.GetExcelSheet<ItemRow>()?.GetRowOrDefault(item->ItemId);
                if (excelItem.HasValue)
                {
                    totalIvl += excelItem.Value.LevelItem.RowId;
                    count++;
                }
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

        public override void Draw()
        {
            if (config.UiScale > 1.0f)
            {
                ImGui.SetWindowFontScale(config.UiScale);
            }

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

            if (config.ShowBuffSynergy)
            {
                bool hasPotionBuff = false;
                bool hasManualOrFcBuff = false;
                bool expiringBuffDetected = false;

                if (objectTable.LocalPlayer != null)
                {
                    var statusSheet = dataManager.GetExcelSheet<StatusRow>();
                    if (statusSheet != null)
                    {
                        foreach (var status in objectTable.LocalPlayer.StatusList)
                        {
                            if (status.StatusId == 0) continue;
                            var row = statusSheet.GetRowOrDefault(status.StatusId);
                            if (row.HasValue)
                            {
                                string bName = row.Value.Name.ToString();
                                string bNameLower = bName.ToLower();
                                string bDescLower = row.Value.Description.ToString().ToLower();

                                if (config.WarnExpiringBuffs && status.RemainingTime > 0 && status.RemainingTime < 120f)
                                {
                                    if (bNameLower.Contains("spiritbond") || bDescLower.Contains("spiritbond") || status.StatusId == 450 || status.StatusId == 49)
                                    {
                                        expiringBuffDetected = true;
                                    }
                                }

                                if (bNameLower.Contains("spiritbond") || bDescLower.Contains("spiritbond") || status.StatusId == 49 || status.StatusId == 1003 || status.StatusId == 450)
                                {
                                    if (bNameLower.Contains("superior") || bNameLower.Contains("potion") || bDescLower.Contains("potion") || status.StatusId == 450)
                                        hasPotionBuff = true;
                                    else
                                        hasManualOrFcBuff = true;
                                }
                            }
                        }
                    }
                }

                if (expiringBuffDetected && !buffWarningSent && config.WarnExpiringBuffs)
                {
                    buffWarningSent = true;
                    chatGui.Print("[Spiritbond Tracker] WARNING: One of your Spiritbond buffs is expiring in less than 2 minutes!");
                }
                else if (!expiringBuffDetected)
                {
                    buffWarningSent = false;
                }

                ImGui.BeginChild("BuffCard", new Vector2(0, 42), true, ImGuiWindowFlags.NoScrollbar);
                if (hasPotionBuff && hasManualOrFcBuff)
                    ImGui.TextColored(config.HighContrastMode ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f) : new Vector4(0.2f, 1.0f, 0.2f, 1.0f), "✨ Buff Synergy: MAXIMIZED (Full Bonus Active)");
                else if (hasPotionBuff || hasManualOrFcBuff)
                    ImGui.TextColored(config.HighContrastMode ? new Vector4(1.0f, 1.0f, 0.0f, 1.0f) : new Vector4(1.0f, 0.8f, 0.2f, 1.0f), "⚡ Buff Synergy: PARTIAL (Add missing buff!)");
                else
                    ImGui.TextColored(config.HighContrastMode ? new Vector4(1.0f, 0.0f, 0.0f, 1.0f) : new Vector4(1.0f, 0.2f, 0.2f, 1.0f), "❌ Buff Synergy: NONE (Use Potion + Manual/FC!)");

                if (expiringBuffDetected)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(1.0f, 0.3f, 0.3f, 1.0f), "⚠️ Expiring soon (< 2m)!");
                }
                ImGui.EndChild();
            }

            if (config.ShowDutyAdvisor)
            {
                float avgIvl = GetAverageEquippedItemLevel();
                string recommendedDuty = GetRandomizedDutyRecommendation(avgIvl);

                ImGui.BeginChild("AdvisorCard", new Vector2(0, 50), true, ImGuiWindowFlags.NoScrollbar);
                ImGui.TextColored(accentColor, "💡 Smart Duty Advisor:");
                ImGui.SameLine();
                ImGui.Text(recommendedDuty);
                ImGui.SameLine();
                if (ImGui.Button("🔄 Roll"))
                {
                    GetRandomizedDutyRecommendation(avgIvl, true);
                }
                ImGui.EndChild();
            }

            if (config.ShowCappedSlotsCounter)
            {
                var gearListPreview = GetCurrentGearList();
                int cappedCount = gearListPreview.Count(g => g.CurrentPercent >= 100f);
                int totalCount = gearListPreview.Count;

                ImGui.BeginChild("CounterCard", new Vector2(0, 35), true, ImGuiWindowFlags.NoScrollbar);
                ImGui.Text($"Ready for Extraction (Capped Slots): ");
                ImGui.SameLine();
                ImGui.TextColored(cappedCount == totalCount ? new Vector4(0.0f, 1.0f, 0.5f, 1.0f) : accentColor, $"{cappedCount} / {totalCount}");
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
                notifiedCappedSlots.Clear();
            }
            ImGui.SameLine();
            if (ImGui.Button("Extract Materia")) chatGui.Print("/materiaextraction");
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

            var gearList = GetCurrentGearList();
            int columnCount = 4 + (config.ShowEligibilityColumn ? 1 : 0);

            if (ImGui.BeginTable("GearTable", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 95f);
                ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch);
                if (config.ShowEligibilityColumn)
                    ImGui.TableSetupColumn("Eligibility", ImGuiTableColumnFlags.WidthFixed, 105f);
                ImGui.TableSetupColumn("Spiritbond", ImGuiTableColumnFlags.WidthFixed, 95f);
                ImGui.TableSetupColumn("Gain in Duty", ImGuiTableColumnFlags.WidthFixed, 105f);
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
                ImGui.SetNextWindowSize(new Vector2(750, 520), ImGuiCond.FirstUseEver);
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

                    var grouped = filteredRecords.GroupBy(h => h.DutyName).OrderByDescending(g => g.Max(x => x.Timestamp));
                    foreach (var group in grouped)
                    {
                        float totalGroupGain = group.Sum(x => x.Gained);
                        string sessionDate = group.First().Timestamp.ToString("yyyy-MM-dd HH:mm");
                        string jobUsed = group.First().JobName;
                        string cardHeader = $"[Duty] {group.Key} ({jobUsed})  -->  Total Gain: +{totalGroupGain:F2}% ({sessionDate})";

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
                                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), $"Items progressed in this duty as [{jobUsed}]:");

                                if (ImGui.BeginTable($"HistoryTable_{group.Key.GetHashCode()}", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                                {
                                    ImGui.TableSetupColumn("Item Name", ImGuiTableColumnFlags.WidthStretch);
                                    ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 90f);
                                    ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, 90f);
                                    ImGui.TableSetupColumn("iLvl", ImGuiTableColumnFlags.WidthFixed, 55f);
                                    ImGui.TableSetupColumn("Gain", ImGuiTableColumnFlags.WidthFixed, 75f);
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
                                    }
                                    ImGui.EndTable();
                                }
                                ImGui.Unindent(10f);
                                ImGui.Spacing();
                            }
                        }
                        ImGui.Spacing();
                    }

                    if (!grouped.Any())
                        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), "No history entries match your search/filter criteria.");
                }
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
                    .Select(g => new { Duty = g.Key, TotalGain = g.Sum(x => x.Gained), SessionsCount = g.Select(x => x.Timestamp.Date).Distinct().Count() })
                    .OrderByDescending(x => x.TotalGain)
                    .FirstOrDefault();

                    if (topDuty != null)
                    {
                        ImGui.Text($"Top Yielding Duty (History): "); ImGui.SameLine();
                        ImGui.TextColored(accentColor, $"{topDuty.Duty} (Total: +{topDuty.TotalGain:F2}% across {topDuty.SessionsCount} sessions)");
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
                }
                ImGui.End();
            }
        }
    }
}

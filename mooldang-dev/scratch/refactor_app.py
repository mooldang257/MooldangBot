import re
import os

files_to_fix = [
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Common/Security/ChannelManagerAuthorizationHandler.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Admin/AdminProfileController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Avatar/AvatarSettingsController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Config/BotConfigController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Debug/DebugController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Overlay/MasterOverlayController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Overlay/OverlayPresetController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Roulette/RouletteController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Roulette/SoundLibraryController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Shared/PreferenceController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/Shared/SharedComponentController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Controllers/SongQueue/SonglistSettingsController.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Features/Admin/ChzzkCategorySyncService.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Features/Broadcast/SendPeriodicMessagesCommand.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Features/Identity/Commands/MergeDuplicateViewersCommand.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Features/Ledger/GenerateWeeklyStatsReportCommand.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Services/Auth/AuthService.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Services/OverlayNotificationService.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Services/Philosophy/BroadcastScribe.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Services/Philosophy/ChzzkChatService.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Services/Philosophy/ResonanceService.cs",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application/Services/SongBookExcelService.cs"
]

properties = [
    "CoreStreamerProfiles", "FuncSongListQueues", "FuncSongListOmakases", "SysAvatarSettings",
    "SysChzzkCategories", "SysChzzkCategoryAliases", "CoreGlobalViewers", "CoreViewerRelations",
    "FuncViewerPoints", "FuncViewerDonations", "FuncViewerDonationHistories", "FuncRouletteMain",
    "FuncRouletteItems", "SysPeriodicMessages", "FuncSongListSessions", "SysOverlayPresets",
    "SysStreamerPreferences", "SysBroadcastSessions", "LogBroadcastHistory", "FuncCmdUnified",
    "IamfScenarios", "IamfGenosRegistry", "IamfParhosCycles", "LogIamfVibrations",
    "IamfStreamerSettings", "SysStreamerKnowledges", "SysSharedComponents", "CoreStreamerManagers",
    "FuncSongBooks", "FuncSongMasterLibrary", "FuncSongStreamerLibrary", "FuncSongMasterStaging",
    "LogRouletteResults", "FuncRouletteSpins", "FuncSoundAssets", "LogPointTransactions",
    "LogPointDailySummaries", "LogRouletteStats", "LogCommandExecutions", "LogChatInteractions",
    "FuncSongQueues", "FuncStreamerOmakases"
]

# Simplified regex: match db.Prop where Prop is in the list
prop_pattern = re.compile(r'db\.(' + '|'.join(properties) + r')\b')

for file_path in files_to_fix:
    if not os.path.exists(file_path):
        print(f"Skipping {file_path} (not found)")
        continue
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Replace db.Prop with db.TableProp
    # Only replace if it's NOT already db.TableProp
    new_content = prop_pattern.sub(lambda m: f"db.Table{m.group(1)}", content)
    
    if new_content != content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f"Updated {file_path}")
    else:
        print(f"No changes needed for {file_path}")

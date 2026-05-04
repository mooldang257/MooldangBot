import re
import os

# Find all files in MooldangBot.Application
application_path = "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application"
all_cs_files = []
for root, dirs, files in os.walk(application_path):
    for file in files:
        if file.endswith(".cs"):
            all_cs_files.append(os.path.join(root, file))

# Mapping of old names to new Table-prefixed names in IAppDbContext
# These are cases where the name changed more significantly than just a prefix
mappings = {
    r'db\.TableFuncStreamerOmakases\b': 'db.TableFuncSongListOmakases',
    r'db\.FuncMasterSongLibraries\b': 'db.TableFuncSongMasterLibrary',
    r'db\.FuncRoulettes\b': 'db.TableFuncRouletteMain',
    r'db\.FuncMasterSongStagings\b': 'db.TableFuncSongMasterStaging',
    r'db\.FuncSonglistSessions\b': 'db.TableFuncSongListSessions',
    r'db\.FuncRouletteLogs\b': 'db.TableLogRouletteResults',
    r'db\.TableFuncSongQueues\b': 'db.TableFuncSongListQueues',
    r'db\.FuncSongQueues\b': 'db.TableFuncSongListQueues',
}

# General property list for simple Table prefix addition
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
    "LogPointDailySummaries", "LogRouletteStats", "LogCommandExecutions", "LogChatInteractions"
]

prop_pattern = re.compile(r'db\.(' + '|'.join(properties) + r')\b')

for file_path in all_cs_files:
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    new_content = content
    
    # 1. Apply specific mappings
    for pattern, replacement in mappings.items():
        new_content = re.sub(pattern, replacement, new_content)
    
    # 2. Apply general Table prefix
    new_content = prop_pattern.sub(lambda m: f"db.Table{m.group(1)}", new_content)
    
    if new_content != content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f"Updated {file_path}")

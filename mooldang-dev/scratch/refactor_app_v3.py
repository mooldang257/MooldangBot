import re
import os

# Find all files in MooldangBot.Application
application_path = "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application"
all_cs_files = []
for root, dirs, files in os.walk(application_path):
    for file in files:
        if file.endswith(".cs"):
            all_cs_files.append(os.path.join(root, file))

# Variable names commonly used for IAppDbContext
var_names = ["db", "_db", "context", "_context", "dbContext"]
var_pattern = r'\b(' + '|'.join(var_names) + r')\b'

# Mapping of specific significantly changed names
# Key is the property name, value is the full replacement property name
specific_mappings = {
    "FuncMasterSongLibraries": "TableFuncSongMasterLibrary",
    "FuncMasterSongStagings": "TableFuncSongMasterStaging",
    "FuncRoulettes": "TableFuncRouletteMain",
    "FuncSonglistSessions": "TableFuncSongListSessions",
    "FuncRouletteLogs": "TableLogRouletteResults",
    "FuncSongQueues": "TableFuncSongListQueues",
    "FuncStreamerOmakases": "TableFuncSongListOmakases"
}

# General properties that just need "Table" prefix
general_properties = [
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

# Combined pattern to match (db|context)[!]?.PropertyName
# Group 1: Variable name
# Group 2: Potential '!'
# Group 3: Property name
combined_props = list(set(list(specific_mappings.keys()) + general_properties))
pattern = re.compile(var_pattern + r'(!?)\.(' + '|'.join(combined_props) + r')\b')

def replace_func(m):
    var_name = m.group(1)
    bang = m.group(2)
    prop_name = m.group(3)
    
    # Priority 1: Specific mapping
    if prop_name in specific_mappings:
        return f"{var_name}{bang}.{specific_mappings[prop_name]}"
    
    # Priority 2: General "Table" prefix
    return f"{var_name}{bang}.Table{prop_name}"

for file_path in all_cs_files:
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Also handle TableFuncSongQueues -> TableFuncSongListQueues separately if it exists
    # And TableFuncStreamerOmakases -> TableFuncSongListOmakases
    # Since these might have been partially prefixed by previous script runs
    content = re.sub(r'\.TableFuncSongQueues\b', '.TableFuncSongListQueues', content)
    content = re.sub(r'\.TableFuncStreamerOmakases\b', '.TableFuncSongListOmakases', content)
    
    new_content = pattern.sub(replace_func, content)
    
    if new_content != content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f"Updated {file_path}")

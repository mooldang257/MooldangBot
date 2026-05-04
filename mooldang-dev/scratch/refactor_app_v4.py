import re
import os

# Find all files in MooldangBot.Application
application_path = "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application"
all_cs_files = []
for root, dirs, files in os.walk(application_path):
    for file in files:
        if file.endswith(".cs"):
            all_cs_files.append(os.path.join(root, file))

# Variable names commonly used for IAppDbContext (Case Insensitive)
var_names = ["db", "_db", "context", "_context", "dbContext", "scopedDb", "_dbContext"]
var_pattern = r'\b(' + '|'.join(var_names) + r')\b'

# Mapping of specific significantly changed names
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

combined_props = list(set(list(specific_mappings.keys()) + general_properties))
# Use re.IGNORECASE for the variable name part
pattern = re.compile(r'\b(?P<var>' + '|'.join(var_names) + r')(?P<bang>!?)\.(?P<prop>' + '|'.join(combined_props) + r')\b', re.IGNORECASE)

def replace_func(m):
    var_name = m.group('var')
    bang = m.group('bang')
    prop_name = m.group('prop')
    
    # We want to maintain the casing of prop_name if it's already Correct
    # But properties list is in PascalCase already.
    
    # Priority 1: Specific mapping (case-insensitive key search)
    match_prop = next((k for k in specific_mappings.keys() if k.lower() == prop_name.lower()), None)
    if match_prop:
        return f"{var_name}{bang}.{specific_mappings[match_prop]}"
    
    # Priority 2: General "Table" prefix
    # Check if already has Table prefix (to avoid TableTable)
    if prop_name.startswith("Table"):
        return f"{var_name}{bang}.{prop_name}"
    
    return f"{var_name}{bang}.Table{prop_name}"

for file_path in all_cs_files:
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Handle partially prefixed names first
    content = re.sub(r'\.TableFuncSongQueues\b', '.TableFuncSongListQueues', content)
    content = re.sub(r'\.TableFuncStreamerOmakases\b', '.TableFuncSongListOmakases', content)
    
    new_content = pattern.sub(replace_func, content)
    
    if new_content != content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f"Updated {file_path}")

import re
import os

# Project paths to refactor
project_paths = [
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Application",
    "/home/mooldang/projects/MooldangBot/mooldang-dev/MooldangBot.Infrastructure"
]

all_cs_files = []
for project_path in project_paths:
    for root, dirs, files in os.walk(project_path):
        for file in files:
            if file.endswith(".cs"):
                all_cs_files.append(os.path.join(root, file))

# Variable names commonly used for IAppDbContext (Case Insensitive)
var_names = ["db", "_db", "context", "_context", "dbContext", "scopedDb", "_dbContext", "AppDbContext"]
var_pattern = r'\b(?P<var>' + '|'.join(var_names) + r')'

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
    "LogPointDailySummaries", "LogRouletteStats", "LogCommandExecutions", "LogChatInteractions",
    "CommonThumbnail"
]

combined_props = list(set(list(specific_mappings.keys()) + general_properties))
pattern = re.compile(var_pattern + r'(?P<bang>!?)\.(?P<prop>' + '|'.join(combined_props) + r')\b', re.IGNORECASE)

def replace_func(m):
    var_name = m.group('var')
    bang = m.group('bang')
    prop_name = m.group('prop')
    
    # Priority 1: Specific mapping
    match_prop = next((k for k in specific_mappings.keys() if k.lower() == prop_name.lower()), None)
    if match_prop:
        return f"{var_name}{bang}.{specific_mappings[match_prop]}"
    
    # Priority 2: General "Table" prefix
    if prop_name.startswith("Table"):
        return f"{var_name}{bang}.{prop_name}"
    
    return f"{var_name}{bang}.Table{prop_name}"

for file_path in all_cs_files:
    # Skip AppDbContext.DbSets.cs itself to avoid double-prefixing if manually managed
    if "AppDbContext.DbSets.cs" in file_path or "IAppDbContext.cs" in file_path:
        continue
        
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

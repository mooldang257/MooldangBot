import os
import re

# [Project Osiris]: API Path Normalization Batch Script (P0)
# This script standardizes API paths in the frontend to match the new kebab-case and RESTful patterns.

TARGET_FOLDERS = [
    "MooldangBot.Admin/src",
    "MooldangBot.Studio/src",
    "MooldangBot.Overlay/src"
]

REPLACEMENTS = [
    # 1. Song Queue (GET)
    # Old: /api/song/queue/${uid}?status=...
    # New: /api/song/${uid}/queue?status=...
    (r"/api/song/queue/(\$\{.*?\})", r"/api/song/\1/queue"),

    # 2. Song Add (POST)
    # Old: /api/song/add/${uid}
    # New: /api/song/${uid}
    (r"/api/song/add/(\$\{.*?\})", r"/api/song/\1"),

    # 3. Song Delete (DELETE/POST bulk)
    # Old: /api/song/delete/${uid}
    # New: /api/song/\1/bulk (DELETE only, handled by matching method in source if possible, but path is priority)
    (r"/api/song/delete/(\$\{.*?\})", r"/api/song/\1/bulk"),

    # 4. Song Update/Edit (PUT)
    # Old: /api/song/${uid}/${id}/edit
    # New: /api/song/${uid}/${id}
    (r"/api/song/(\$\{.*?\})/(\$\{.*?\})/edit", r"/api/song/\1/\2"),

    # 5. Song Clear (DELETE)
    # Old: /api/song/clear/${uid}/${status}
    # New: /api/song/${uid}/clear/${status}
    (r"/api/song/clear/(\$\{.*?\})/(\$\{.*?\})", r"/api/song/\1/clear/\2"),

    # 6. Commands (Unified)
    # Old: /api/commands/unified/delete/${uid}/${id}
    # New: /api/commands/${uid}/${id}
    (r"/api/commands/unified/delete/(\$\{.*?\})/(\$\{.*?\})", r"/api/commands/\1/\2"),
    # Old: /api/commands/unified/toggle/${uid}/${id}
    # New: /api/commands/${uid}/${id}/status
    (r"/api/commands/unified/toggle/(\$\{.*?\})/(\$\{.*?\})", r"/api/commands/\1/\2/status"),
    # Old: /api/commands/unified/save/${uid}
    # New: /api/commands/${uid}
    (r"/api/commands/unified/save/(\$\{.*?\})", r"/api/commands/\1"),
    # Old: /api/commands/unified/${uid}
    # New: /api/commands/${uid}
    (r"/api/commands/unified/(\$\{.*?\})", r"/api/commands/\1"),

    # 7. Periodic Message
    (r"/api/SysPeriodicMessages/list/(\$\{.*?\})", r"/api/periodic-message/\1"),
    (r"/api/SysPeriodicMessages/save/(\$\{.*?\})", r"/api/periodic-message/\1"),
    (r"/api/SysPeriodicMessages/delete/(\$\{.*?\})/(\$\{.*?\})", r"/api/periodic-message/\1/\2"),
    (r"/api/SysPeriodicMessages/toggle/(\$\{.*?\})/(\$\{.*?\})", r"/api/periodic-message/\1/\2/status"),
    (r"api/SysPeriodicMessages", r"api/periodic-message"),

    # 8. Overlay Preset
    (r"/api/SysOverlayPresets/list/(\$\{.*?\})", r"/api/overlay-preset/\1"),
    (r"/api/SysOverlayPresets/upload-image/(\$\{.*?\})", r"/api/overlay-preset/\1/image"),
    (r"/api/SysOverlayPresets/active/(\$\{.*?\})", r"/api/overlay-preset/\1/active"),
    (r"/api/SysOverlayPresets/sync/(\$\{.*?\})/(\$\{.*?\})", r"/api/overlay-preset/\1/\2/active"),
    (r"api/SysOverlayPresets", r"api/overlay-preset"),

    # 9. Song Request (Sample)
    (r"/api/SongRequest/pending/(\$\{.*?\})", r"/api/song-request/\1/pending"),
    (r"api/SongRequest", r"api/song-request"),
    
    # 10. FuncRouletteMain Bulk History
    (r"api/admin/roulette/history/bulk-delete", r"api/admin/roulette/history/bulk"),
    
    # 11. Shared Component
    (r"api/SysSharedComponents", r"api/shared-component"),

    # 12. Method Replacements (Specific to Status/Toggle)
    # This matches apiFetch calls for status updates that were PUT but should be PATCH
    (r'apiFetch\(`(/api/song/\$\{.*?\}/\$\{.*?\}/status.*?)`, \{\s*method:\s*["\']PUT["\']', 
     r'apiFetch(`\1`, { method: "PATCH"'),
    (r'apiFetch\(`(/api/periodic-message/\$\{.*?\}/\$\{.*?\}/status.*?)`, \{\s*method:\s*["\']PUT["\']', 
     r'apiFetch(`\1`, { method: "PATCH"'),
]

def refactor_file(file_path):
    print(f"Checking: {file_path}")
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    new_content = content
    for pattern, replacement in REPLACEMENTS:
        new_content = re.sub(pattern, replacement, new_content)
    
    if new_content != content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f" [DONE] Refactored: {file_path}")
    else:
        pass

def main():
    base_path = "c:\\webapi\\MooldangAPI\\MooldangBot"
    for folder in TARGET_FOLDERS:
        folder_path = os.path.join(base_path, folder)
        if not os.path.exists(folder_path):
            print(f"Skip: {folder_path} (Not Found)")
            continue
            
        for root, _, files in os.walk(folder_path):
            for file in files:
                if file.endswith(('.svelte', '.ts', '.js')):
                    refactor_file(os.path.join(root, file))

if __name__ == "__main__":
    main()

import shutil
import os
from pathlib import Path

def wipe_db_data():
    target_dir = Path("./data/mariadb")
    
    print(f"[Wipe Script] Target Directory: {target_dir.absolute()}")
    
    if target_dir.exists():
        try:
            print(f"[Wipe Script] Starting data wipe...")
            shutil.rmtree(target_dir, ignore_errors=False)
            print(f"[Wipe Script] Data wipe completed.")
        except Exception as e:
            print(f"[Wipe Script] Error occurred: {e}")
            print(f"[Tip] Containers might still be running or files are in use.")
    else:
        print(f"[Wipe Script] Target directory does not exist. Already clean.")

if __name__ == "__main__":
    wipe_db_data()

import os

root_dir = r'c:\webapi\MooldangAPI\MooldangBot'
replacements = {
    'MooldangBot.ChzzkAPI.Interfaces': 'MooldangBot.Application.Interfaces',
    'MooldangBot.ChzzkAPI.Models': 'MooldangBot.Application.Models.Chzzk',
    'MooldangBot.ChzzkAPI.Serialization': 'MooldangBot.Application.Models.Chzzk'
}

print(f"Starting namespace cleanup in {root_dir}...")

count = 0
for root, dirs, files in os.walk(root_dir):
    for file in files:
        if file.endswith('.cs'):
            file_path = os.path.join(root, file)
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                new_content = content
                for old, new in replacements.items():
                    new_content = new_content.replace(old, new)
                
                if new_content != content:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(new_content)
                    print(f"Updated: {file_path}")
                    count += 1
            except Exception as e:
                print(f"Error processing {file_path}: {e}")

print(f"Cleanup finished. Total files updated: {count}")

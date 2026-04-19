import os
import re
from pathlib import Path

# 검색할 SQL 패턴 (Regex)
SQL_PATTERNS = {
    "UPSERT": r'INSERT.*?ON\s+DUPLICATE\s+KEY\s+UPDATE',
    "INSERT": r'INSERT\s+INTO\s+',
    "UPDATE": r'UPDATE\s+.*?\s+SET\s+',
    "DELETE": r'DELETE\s+FROM\s+'
}

def audit_queries():
    root_dir = Path(".")
    audit_results = []
    
    print(f"[Audit Script] Starting SQL pattern audit in: {root_dir.absolute()}")
    
    # 윈도우 환경에서도 안전하게 파일 순회
    for file_path in root_dir.rglob("*.cs"):
        if "obj" in file_path.parts or "bin" in file_path.parts:
            continue
            
        try:
            with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
                content = f.read()
                
                for key, pattern in SQL_PATTERNS.items():
                    matches = list(re.finditer(pattern, content, re.IGNORECASE | re.DOTALL))
                    if matches:
                        for match in matches:
                            line_no = content.count("\n", 0, match.start()) + 1
                            snippet = content[max(0, match.start()-20) : min(len(content), match.end()+50)].replace("\n", " ")
                            audit_results.append({
                                "file": str(file_path),
                                "line": line_no,
                                "type": key,
                                "snippet": snippet.strip()
                            })
        except Exception as e:
            print(f"[Audit Script] Failed to read {file_path}: {e}")

    # 결과 요약 및 출력
    if audit_results:
        print(f"\n[Audit Script] Found {len(audit_results)} SQL patterns.")
        print("-" * 80)
        for res in audit_results:
            print(f"[{res['type']}] {res['file']}:L{res['line']}")
            print(f"    Code: ...{res['snippet']}...")
        print("-" * 80)
    else:
        print("[Audit Script] No raw SQL patterns found (using regular expressions).")

if __name__ == "__main__":
    audit_queries()

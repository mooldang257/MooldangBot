$ErrorActionPreference = "Stop"

$path = "c:\webapi\MooldangAPI\wwwroot\dashboard.html"
$content = Get-Content -Raw $path
$content = $content.Replace('class="primary" style="background:var(--accent); color:white; margin-bottom: 0; border: none; padding: 10px; border-radius: 8px; white-space: nowrap; width: auto; font-size: 14px; box-shadow: 0 4px 10px rgba(255, 154, 158, 0.4);"', 'class="btn btn-danger" style="margin-bottom: 0;"')
$content = $content.Replace('style="background: white; color: var(--primary); border: 1px solid var(--primary); padding: 6px 15px; border-radius: 6px; font-size: 13px; cursor: pointer; font-weight: bold;"', 'class="btn btn-outline"')
$content = $content.Replace('style="background: white; color: var(--primary); border: 1px solid var(--primary);"', 'class="btn btn-outline"')
$content = $content.Replace('class="primary" onclick="addSong()">➕ 대기열에 추가하기', 'class="btn btn-primary" onclick="addSong()">➕ 대기열에 추가하기')
$content = $content.Replace('class="finish-btn"', 'class="btn btn-outline finish-btn"')

$content = $content.Replace('style="background:var(--accent); color:white; border:none; padding:6px 10px; border-radius:6px; font-size:12px; font-weight:bold; cursor:pointer;"', 'class="btn btn-danger btn-sm"')
$content = $content.Replace('background:var(--accent); color:white; border:none; padding:6px 10px; border-radius:6px; font-size:12px; font-weight:bold; cursor:pointer;', 'class="btn btn-danger btn-sm"')

$content = $content.Replace('class="action-btn" style="background:var(--text-muted);color:white;"', 'class="btn btn-secondary btn-sm"')
$content = $content.Replace('class="action-btn" style="background:white;color:var(--primary);border:1px solid var(--primary);"', 'class="btn btn-outline btn-sm"')
$content = $content.Replace('class="action-btn" style="background:var(--secondary);color:white;"', 'class="btn btn-primary btn-sm"')
$content = $content.Replace('class="action-btn" style="background:var(--accent);color:white;"', 'class="btn btn-danger btn-sm"')
$content = $content.Replace('class="action-btn" style="background:white;color:var(--text-muted);border:1px solid var(--text-muted);"', 'class="btn btn-outline btn-sm"')

Set-Content -Path $path -Value $content

$path2 = "c:\webapi\MooldangAPI\wwwroot\commands.html"
$content2 = Get-Content -Raw $path2
$content2 = $content2.Replace('class="btn-add"', 'class="btn btn-primary"')
$content2 = $content2.Replace('class="btn-back"', 'class="btn btn-outline"')
$content2 = $content2.Replace('class="action-btn" style="background:var(--accent);"', 'class="btn btn-danger btn-sm"')
Set-Content -Path $path2 -Value $content2

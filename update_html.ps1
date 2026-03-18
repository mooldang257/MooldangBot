$ErrorActionPreference = "Stop"

function Update-HtmlFile {
    param([string]$path)
    $content = [IO.File]::ReadAllText($path)
    $content = $content -replace '(?s)<style>.*?</style>', '<link rel="stylesheet" href="/css/glass-theme.css">'
    
    # Common text color fixes
    $content = $content.Replace('color: #888;', 'color: var(--text-muted);')
    $content = $content.Replace('color: #00ffa3;', 'color: var(--primary);')
    $content = $content.Replace('color: #fff;', 'color: var(--text-dark);')
    
    if ($path -match "dashboard.html") {
        $content = $content.Replace('border-color: #ff00ff; background: rgba(255, 0, 255, 0.05);', 'border: 1px solid var(--accent); background: rgba(255, 154, 158, 0.1);')
        $content = $content.Replace('color: #ff00ff; border-bottom-color: #ff00ff;', 'color: var(--accent); border-bottom-color: rgba(255,154,158,0.3);')
        $content = $content.Replace('flex: 2; border-color:#ff00ff; margin-bottom: 0;', 'flex: 2; margin-bottom: 0;')
        $content = $content.Replace('flex: 1; border-color:#ffaa00; margin-bottom: 0;', 'flex: 1; margin-bottom: 0;')
        $content = $content.Replace('background:#ff00ff; color:white; margin-bottom: 0; white-space: nowrap; width: auto; font-size: 14px;', 'background:var(--accent); color:white; margin-bottom: 0; border: none; padding: 10px; border-radius: 8px; white-space: nowrap; width: auto; font-size: 14px; box-shadow: 0 4px 10px rgba(255, 154, 158, 0.4);')
        $content = $content.Replace('background: #333; color: #00ffa3; border: 1px solid #00ffa3;', 'background: white; color: var(--primary); border: 1px solid var(--primary);')
        $content = $content.Replace('background: #333; color: #fff; border: 1px solid #555;', 'background: white; color: var(--text-dark); border: 1px solid var(--glass-border);')
        $content = $content.Replace('color: #00aaff; font-weight: bold; font-size: 16px;', 'color: var(--primary); font-weight: bold; font-size: 16px;')
        $content = $content.Replace('font-size: 24px; font-weight: bold; color: white;', 'font-size: 24px; font-weight: bold; color: var(--primary);')
        $content = $content.Replace('background:#555;', 'background:var(--text-muted);color:white;')
        $content = $content.Replace('background:#00ffa3;color:#000;', 'background:white;color:var(--primary);border:1px solid var(--primary);')
        $content = $content.Replace('background:#0088cc;', 'background:var(--secondary);color:white;')
        $content = $content.Replace('background:#b71c1c;', 'background:var(--accent);color:white;')
        $content = $content.Replace('background:#888;', 'background:white;color:var(--text-muted);border:1px solid var(--text-muted);')
        $content = $content.Replace('color: #aaa;', 'color: var(--text-muted);')
        $content = $content.Replace('background:#ff5252; color:white; border:none; padding:4px 10px; border-radius:4px; font-size:11px; cursor:pointer;', 'background:var(--accent); color:white; border:none; padding:6px 10px; border-radius:6px; font-size:12px; font-weight:bold; cursor:pointer;')
    }
    elseif ($path -match "commands.html") {
        # any commands specific fixes
    }
    elseif ($path -match "settings.html") {
        $content = $content.Replace('color: #00aaff;', 'color: var(--primary);')
        $content = $content.Replace('color: #aaa;', 'color: var(--text-muted);')
        $content = $content.Replace('border-left: 5px solid #00ffa3;', 'border-left: 5px solid var(--primary);')
        $content = $content.Replace('color: #eee;', 'color: var(--text-dark);')
    }
    
    [IO.File]::WriteAllText($path, $content)
}

Update-HtmlFile "c:\webapi\MooldangAPI\wwwroot\dashboard.html"
Update-HtmlFile "c:\webapi\MooldangAPI\wwwroot\commands.html"
Update-HtmlFile "c:\webapi\MooldangAPI\wwwroot\settings.html"

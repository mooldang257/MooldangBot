Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap('c:\webapi\MooldangAPI\wwwroot\images\wman_sd.png')
$pixel = $bmp.GetPixel(0,0)
$bmp.MakeTransparent($pixel)
$bmp.Save('c:\webapi\MooldangAPI\wwwroot\images\wman_sd_transparent.png', [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

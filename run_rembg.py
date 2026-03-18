from rembg import remove
from PIL import Image
try:
    input = Image.open(r'c:\webapi\MooldangAPI\wwwroot\images\wman_sd.png')
    output = remove(input)
    output.save(r'c:\webapi\MooldangAPI\wwwroot\images\wman_sd_transparent.png')
    print('SUCCESS')
except Exception as e:
    print('ERROR:', e)

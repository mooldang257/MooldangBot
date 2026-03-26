from rembg import remove, new_session
from PIL import Image
try:
    session = new_session('isnet-anime')
    input = Image.open(r'c:\webapi\MooldangAPI\wwwroot\images\wman_sd.png')
    output = remove(input, session=session, alpha_matting=True)
    output.save(r'c:\webapi\MooldangAPI\wwwroot\images\wman_sd_transparent.png')
    print('SUCCESS')
except Exception as e:
    print('ERROR:', e)

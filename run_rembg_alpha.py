from rembg import remove, new_session
from PIL import Image
try:
    session = new_session('u2net')
    input = Image.open(r'c:\webapi\MooldangAPI\wwwroot\images\wman_sd.png')
    
    # u2net + alpha_matting 조합으로 디테일 및 물결 유지!
    output = remove(input, session=session, alpha_matting=True)
    output.save(r'c:\webapi\MooldangAPI\wwwroot\images\wman_sd_transparent.png')
    print('SUCCESS')
except Exception as e:
    print('ERROR:', e)

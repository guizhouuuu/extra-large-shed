from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
image = Image.open(root/'assets/extra-large-shed.png').convert('RGBA')
mask = Image.open(root/'assets/extra-large-shed-paint-mask.png').convert('RGBA')
def pixels(im):
    return list(im.get_flattened_data()) if hasattr(im, 'get_flattened_data') else list(im.getdata())
assert image.size == mask.size == (112,128)
a, m = pixels(image), pixels(mask)
allowed = {(0,0,0,0), (255,0,0,255), (0,255,0,255), (0,0,255,255)}
assert set(m) == allowed
assert set(pixels(image.getchannel('A'))) == {0,255}
assert all(mark[3] == 0 or pixel[3] == 255 for pixel,mark in zip(a,m))
print('PASS: PNG decode, matching 112x128 canvases, hard alpha, all exact RGB paint channels, mask only on opaque pixels.')

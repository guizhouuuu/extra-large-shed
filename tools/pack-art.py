"""Pack the generated source into the native sprite grid and exact RGB paint channels.
Requires Pillow. This is a deterministic build-asset step, not a runtime dependency.
"""
from pathlib import Path
from PIL import Image
import colorsys, json

root = Path(__file__).resolve().parents[1]
source = root / 'art' / 'source-sprite.png'
assets = root / 'assets'
assets.mkdir(exist_ok=True)
image = Image.open(source).convert('RGBA').resize((112, 128), Image.Resampling.NEAREST)
# A game sprite needs hard alpha and a restrained palette at its final native size.
image.putalpha(image.getchannel('A').point(lambda a: 255 if a >= 128 else 0))
image = image.quantize(colors=64, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).convert('RGBA')
mask = Image.new('RGBA', image.size, (0, 0, 0, 0))
counts = {'Building': 0, 'Roof': 0, 'Trim': 0}
for y in range(image.height):
    for x in range(image.width):
        r, g, b, a = image.getpixel((x, y))
        if not a or max(r,g,b) < 50:
            continue
        neutral = (82 <= x <= 95 and 8 <= y <= 43) or any(
            lo <= x <= hi and top <= y <= bottom for lo, hi, top, bottom in
            ((27,35,47,54),(76,85,47,54),(51,61,63,73),(24,33,94,103),(78,87,94,103),(54,60,100,107)))
        if neutral: continue
        hue, saturation, value = colorsys.rgb_to_hsv(r/255, g/255, b/255)
        if y < 85 and saturation > .45 and (hue < .055 or hue > .95):
            channel, color = 'Roof', (0,255,0,255)
        elif r > g * 1.08 and g > b * 1.10 and saturation > .35:
            structural = y >= 78 and (78 <= y <= 83 or any(lo <= x <= hi for lo,hi in
                ((7,13),(25,30),(40,45),(67,73),(82,88),(98,104))))
            channel, color = ('Trim',(0,0,255,255)) if structural else ('Building',(255,0,0,255))
        else:
            continue # glass, stone, and dark outlines keep their own colors
        mask.putpixel((x,y), color)
        counts[channel] += 1
image.save(assets/'extra-large-shed.png')
mask.save(assets/'extra-large-shed-paint-mask.png')
print(json.dumps({'size': image.size, 'paint_pixels': counts}))

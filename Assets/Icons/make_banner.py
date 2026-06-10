"""
HanuYat PC-client hero banner. 1950x630, no text (full-bleed key art).
Concept: eerie school corridor, one-point perspective, vanishing point
off-center (right third), glowing emergency-exit doorway, haze + vignette.
Supersampled 2x then downsampled.
"""

from PIL import Image, ImageDraw, ImageFilter

W, H = 1950, 630
SS = 2
CW, CH = W * SS, H * SS
# vanishing point: right third, slightly above mid
CX, CY = int(CW * 0.60), int(CH * 0.44)

SKY = (10, 13, 15)
FLOOR_NEAR = (40, 46, 50)
CEIL_NEAR = (26, 32, 36)
WALL_L = (33, 40, 44)
WALL_R = (29, 36, 40)
DOOR = (46, 40, 34)
DOOR_FRAME = (60, 68, 70)
LINE = (66, 92, 94)
LIGHT = (210, 225, 220)
EXIT_GREEN = (60, 230, 140)

ft = 0.80  # far-wall depth fraction (wide -> push deep)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def vp(x, y, t):
    return (x + (CX - x) * t, y + (CY - y) * t)


img = Image.new("RGBA", (CW, CH), SKY + (255,))
d = ImageDraw.Draw(img)

# corridor surfaces
fl, fr = vp(0, 0, ft), vp(CW, 0, ft)
fbl, fbr = vp(0, CH, ft), vp(CW, CH, ft)
far_rect = [fl[0], fl[1], fr[0], fbr[1]]
d.polygon([(0, 0), (CW, 0), fr, fl], fill=CEIL_NEAR)
d.polygon([(0, CH), (CW, CH), fbr, fbl], fill=FLOOR_NEAR)
d.polygon([(0, 0), fl, fbl, (0, CH)], fill=WALL_L)
d.polygon([(CW, 0), fr, fbr, (CW, CH)], fill=WALL_R)
d.rectangle(far_rect, fill=lerp(WALL_L, SKY, 0.45))

# depth darkening
ov = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
od = ImageDraw.Draw(ov)
steps = 30
for i in range(steps):
    t0, t1 = i / steps, (i + 1) / steps
    a = int(160 * t0)
    od.polygon([vp(0, CH, t0 * ft), vp(CW, CH, t0 * ft),
                vp(CW, CH, t1 * ft), vp(0, CH, t1 * ft)], fill=(0, 0, 0, a))
    od.polygon([vp(0, 0, t0 * ft), vp(CW, 0, t0 * ft),
                vp(CW, 0, t1 * ft), vp(0, 0, t1 * ft)], fill=(0, 0, 0, a))
img = Image.alpha_composite(img, ov)
d = ImageDraw.Draw(img)

# perspective edge + floor/ceiling depth lines
for (x, y) in [(0, 0), (CW, 0), (0, CH), (CW, CH)]:
    d.line([(x, y), vp(x, y, ft)], fill=LINE, width=3)
for t in (0.10, 0.20, 0.32, 0.46, 0.60):
    d.line([vp(0, CH, t * ft), vp(CW, CH, t * ft)], fill=lerp(FLOOR_NEAR, LINE, 0.45), width=2)
    d.line([vp(0, 0, t * ft), vp(CW, 0, t * ft)], fill=lerp(CEIL_NEAR, LINE, 0.30), width=2)


# classroom doors along both walls
def wall_door(side, t0, t1, top_frac, bot_frac):
    ex = 0 if side == "L" else CW
    top0, bot0 = vp(ex, 0, t0 * ft), vp(ex, CH, t0 * ft)
    top1, bot1 = vp(ex, 0, t1 * ft), vp(ex, CH, t1 * ft)
    at = lambda tp, bt, f: (tp[0] + (bt[0] - tp[0]) * f, tp[1] + (bt[1] - tp[1]) * f)
    d.polygon([at(top0, bot0, top_frac), at(top1, bot1, top_frac),
               at(top1, bot1, bot_frac), at(top0, bot0, bot_frac)],
              fill=DOOR, outline=DOOR_FRAME)


door_depths = [(0.04, 0.16), (0.22, 0.32), (0.40, 0.48), (0.55, 0.62), (0.68, 0.74)]
for side in ("L", "R"):
    for (a, b) in door_depths:
        wall_door(side, a, b, 0.30, 0.84)

# ceiling light strips down the middle
for t in (0.06, 0.16, 0.28, 0.42, 0.58, 0.72):
    cl = vp(int(CW * 0.50), 0, t * ft)
    cr = vp(int(CW * 0.50) + int(CW * 0.10), 0, t * ft)
    d.line([cl, cr], fill=LIGHT, width=max(2, int(14 * (1 - t))))

# glowing emergency-exit doorway at vanishing point
ew, eh = int(CW * 0.045), int(CH * 0.20)
ex0, ey0 = CX - ew // 2, CY - int(eh * 0.40)
exit_rect = [ex0, ey0, ex0 + ew, ey0 + eh]
glow = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
ImageDraw.Draw(glow).rectangle(
    [exit_rect[0] - 40, exit_rect[1] - 40, exit_rect[2] + 40, exit_rect[3] + 40],
    fill=EXIT_GREEN + (255,))
glow = glow.filter(ImageFilter.GaussianBlur(70))
img = Image.alpha_composite(img, glow)
d = ImageDraw.Draw(img)
d.rectangle(exit_rect, fill=EXIT_GREEN + (255,))
d.rectangle([ex0 + ew // 4, ey0 + eh // 7, ex0 + 3 * ew // 4, ey0 + eh - eh // 8],
            fill=(205, 255, 228, 255))

# atmospheric haze: soft green tint pooling at far end
haze = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
ImageDraw.Draw(haze).ellipse(
    [CX - CW * 0.18, CY - CH * 0.30, CX + CW * 0.18, CY + CH * 0.30],
    fill=EXIT_GREEN + (45,))
haze = haze.filter(ImageFilter.GaussianBlur(120))
img = Image.alpha_composite(img, haze)

# vignette
vig = Image.new("L", (CW, CH), 0)
ImageDraw.Draw(vig).ellipse([-CW * 0.15, -CH * 0.35, CW * 1.15, CH * 1.35], fill=120)
vig = vig.filter(ImageFilter.GaussianBlur(180))
dark = Image.new("RGBA", (CW, CH), (0, 0, 0, 255))
dark.putalpha(Image.eval(vig, lambda v: 165 - v if v < 165 else 0))
img = Image.alpha_composite(img, dark)

# downsample + export
out = img.convert("RGB").resize((W, H), Image.LANCZOS)
out.save("Banner_1950x630.png")
print("png -> Banner_1950x630.png  (%dx%d)" % (W, H))

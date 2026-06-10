"""
HanuYat app icon generator (v2).
Concept: Exit-8 style anomaly-detection walking-loop in a SCHOOL CORRIDOR.
 - one-point perspective hallway: floor, ceiling, two side walls
 - classroom doors along both walls
 - ceiling light strips
 - glowing emergency-exit doorway at the vanishing point
 - no "8" mark
Renders at 1024 supersample, downsamples, writes multi-size .ico.
"""

from PIL import Image, ImageDraw, ImageFont, ImageFilter

S = 1024
CX, CY = S // 2, int(S * 0.46)  # vanishing point slightly above center

# ---- palette ----
SKY = (10, 13, 15)
FLOOR_NEAR = (40, 46, 50)
CEIL_NEAR = (26, 32, 36)
WALL_L = (33, 40, 44)
WALL_R = (29, 36, 40)
DOOR = (46, 40, 34)
DOOR_FRAME = (62, 70, 72)
LINE = (70, 96, 98)
LIGHT = (210, 225, 220)
EXIT_GREEN = (60, 230, 140)

ft = 0.62  # depth fraction of far wall


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def vpoint(x, y, t):
    """interpolate a screen point toward the vanishing point. t in 0..1."""
    return (x + (CX - x) * t, y + (CY - y) * t)


img = Image.new("RGBA", (S, S), SKY + (255,))
d = ImageDraw.Draw(img)

# ---- corridor surfaces (one-point perspective) ----
fl = vpoint(0, 0, ft)
fr = vpoint(S, 0, ft)
fbl = vpoint(0, S, ft)
fbr = vpoint(S, S, ft)
far_rect = [fl[0], fl[1], fr[0], fbr[1]]

d.polygon([(0, 0), (S, 0), (fr[0], fr[1]), (fl[0], fl[1])], fill=CEIL_NEAR)          # ceiling
d.polygon([(0, S), (S, S), (fbr[0], fbr[1]), (fbl[0], fbl[1])], fill=FLOOR_NEAR)     # floor
d.polygon([(0, 0), (fl[0], fl[1]), (fbl[0], fbl[1]), (0, S)], fill=WALL_L)           # left wall
d.polygon([(S, 0), (fr[0], fr[1]), (fbr[0], fbr[1]), (S, S)], fill=WALL_R)           # right wall
d.rectangle(far_rect, fill=lerp(WALL_L, SKY, 0.4))                                   # far wall

# ---- depth shading (darken toward far end) ----
ov = Image.new("RGBA", (S, S), (0, 0, 0, 0))
od = ImageDraw.Draw(ov)
steps = 24
for i in range(steps):
    t0, t1 = i / steps, (i + 1) / steps
    a = int(150 * t0)
    od.polygon([vpoint(0, S, t0 * ft), vpoint(S, S, t0 * ft),
                vpoint(S, S, t1 * ft), vpoint(0, S, t1 * ft)], fill=(0, 0, 0, a))
    od.polygon([vpoint(0, 0, t0 * ft), vpoint(S, 0, t0 * ft),
                vpoint(S, 0, t1 * ft), vpoint(0, 0, t1 * ft)], fill=(0, 0, 0, a))
img = Image.alpha_composite(img, ov)
d = ImageDraw.Draw(img)

# ---- perspective edge lines ----
for (x, y) in [(0, 0), (S, 0), (0, S), (S, S)]:
    d.line([(x, y), vpoint(x, y, ft)], fill=LINE, width=3)

# ---- floor depth lines ----
for t in (0.16, 0.30, 0.44, 0.56):
    d.line([vpoint(0, S, t * ft), vpoint(S, S, t * ft)],
           fill=lerp(FLOOR_NEAR, LINE, 0.5), width=2)

# ---- classroom doors ----
def wall_door(side, t0, t1, top_frac, bot_frac):
    edge_x = 0 if side == "L" else S
    top0, bot0 = vpoint(edge_x, 0, t0 * ft), vpoint(edge_x, S, t0 * ft)
    top1, bot1 = vpoint(edge_x, 0, t1 * ft), vpoint(edge_x, S, t1 * ft)
    at = lambda tp, bt, f: (tp[0] + (bt[0] - tp[0]) * f, tp[1] + (bt[1] - tp[1]) * f)
    d.polygon([at(top0, bot0, top_frac), at(top1, bot1, top_frac),
               at(top1, bot1, bot_frac), at(top0, bot0, bot_frac)],
              fill=DOOR, outline=DOOR_FRAME)

for side in ("L", "R"):
    wall_door(side, 0.05, 0.20, 0.30, 0.82)
    wall_door(side, 0.28, 0.40, 0.34, 0.80)
    wall_door(side, 0.46, 0.55, 0.37, 0.78)

# ---- ceiling light strips ----
for t in (0.10, 0.26, 0.42, 0.55):
    cl, cr = vpoint(int(S * 0.40), 0, t * ft), vpoint(int(S * 0.60), 0, t * ft)
    d.line([cl, cr], fill=LIGHT, width=max(2, int(10 * (1 - t))))

# ---- glowing emergency-exit doorway at vanishing point ----
ew, eh = int(S * 0.105), int(S * 0.16)
ex0, ey0 = CX - ew // 2, CY - int(eh * 0.42)
exit_rect = [ex0, ey0, ex0 + ew, ey0 + eh]

glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
gd = ImageDraw.Draw(glow)
gd.rectangle([exit_rect[0] - 30, exit_rect[1] - 30, exit_rect[2] + 30, exit_rect[3] + 30],
             fill=EXIT_GREEN + (255,))
glow = glow.filter(ImageFilter.GaussianBlur(45))
img = Image.alpha_composite(img, glow)
d = ImageDraw.Draw(img)
d.rectangle(exit_rect, fill=EXIT_GREEN + (255,))
d.rectangle([ex0 + ew // 4, ey0 + eh // 6, ex0 + 3 * ew // 4, ey0 + eh - eh // 8],
            fill=(200, 255, 225, 255))

# ---- vignette ----
vig = Image.new("L", (S, S), 0)
ImageDraw.Draw(vig).ellipse([-S * 0.25, -S * 0.25, S * 1.25, S * 1.25], fill=110)
vig = vig.filter(ImageFilter.GaussianBlur(120))
dark = Image.new("RGBA", (S, S), (0, 0, 0, 255))
dark.putalpha(Image.eval(vig, lambda v: 150 - v if v < 150 else 0))
img = Image.alpha_composite(img, dark)

# ---- rounded-corner mask ----
mask = Image.new("L", (S, S), 0)
rad = int(S * 0.18)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=rad, fill=255)
img.putalpha(mask)
ImageDraw.Draw(img).rounded_rectangle([6, 6, S - 7, S - 7], radius=rad - 6,
                                      outline=(70, 110, 110, 150), width=4)

# ---- export ----
img.save("AppIcon_1024.png")
print("png -> AppIcon_1024.png")
sizes = [256, 128, 64, 48, 32, 16]
img.resize((256, 256), Image.LANCZOS).save("AppIcon_256.png")
img.resize((256, 256), Image.LANCZOS).save(
    "AppIcon.ico", format="ICO", sizes=[(s, s) for s in sizes])
print("ico -> AppIcon.ico  sizes:", sizes)
print("png -> AppIcon_256.png")

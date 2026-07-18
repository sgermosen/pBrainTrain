#!/usr/bin/env python3
"""Genera las escenas de "Encuentra las Diferencias" de BrainTrain.

Cada escena es un SVG plano dibujado por código (cero copyright). La variante
aplica 5 modificaciones puntuales; las coordenadas normalizadas de cada
diferencia se emiten a un archivo C# para que el motor las valide.

Uso:  python3 tools/generate_spotdiff.py
Salida:
  mobile/src/BrainTrain.App/Resources/Images/spotdiff{n}{a,b}.png  (720x480)
  mobile/src/BrainTrain.App.Core/Minigames/SpotDiffScenes.g.cs
"""
import os
import subprocess
import tempfile

W, H = 720, 480
ROOT = os.path.join(os.path.dirname(__file__), "..")
IMG_OUT = os.path.join(ROOT, "mobile", "src", "BrainTrain.App", "Resources", "Images")
CS_OUT = os.path.join(ROOT, "mobile", "src", "BrainTrain.App.Core", "Minigames", "SpotDiffScenes.g.cs")
CHROMIUM = "/opt/pw-browsers/chromium"


def sun(x, y, r, color="#FFD54F", rays=True):
    parts = [f'<circle cx="{x}" cy="{y}" r="{r}" fill="{color}"/>']
    if rays:
        for i in range(8):
            import math
            a = i * math.pi / 4
            x1, y1 = x + math.cos(a) * (r + 8), y + math.sin(a) * (r + 8)
            x2, y2 = x + math.cos(a) * (r + 22), y + math.sin(a) * (r + 22)
            parts.append(f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="{color}" stroke-width="6" stroke-linecap="round"/>')
    return "".join(parts)


def cloud(x, y, s=1.0, color="#FFFFFF"):
    return (f'<g transform="translate({x},{y}) scale({s})" fill="{color}">'
            f'<circle cx="0" cy="0" r="22"/><circle cx="24" cy="-8" r="26"/>'
            f'<circle cx="50" cy="0" r="20"/><rect x="-18" y="0" width="86" height="20" rx="10"/></g>')


def tree(x, y, apples):
    dots = "".join(f'<circle cx="{x + dx}" cy="{y - 95 + dy}" r="7" fill="#E53935"/>' for dx, dy in apples)
    return (f'<rect x="{x - 10}" y="{y - 60}" width="20" height="60" rx="6" fill="#8D6E63"/>'
            f'<circle cx="{x}" cy="{y - 95}" r="52" fill="#66BB6A"/>'
            f'<circle cx="{x - 38}" cy="{y - 72}" r="30" fill="#81C784"/>'
            f'<circle cx="{x + 38}" cy="{y - 72}" r="30" fill="#81C784"/>' + dots)


def house(x, y, door_color="#5D4037", window="square"):
    win = (f'<rect x="{x + 52}" y="{y - 58}" width="30" height="30" rx="4" fill="#B3E5FC" stroke="#546E7A" stroke-width="3"/>'
           if window == "square" else
           f'<circle cx="{x + 67}" cy="{y - 43}" r="16" fill="#B3E5FC" stroke="#546E7A" stroke-width="3"/>')
    return (f'<rect x="{x}" y="{y - 70}" width="110" height="70" fill="#FFCC80"/>'
            f'<polygon points="{x - 12},{y - 70} {x + 55},{y - 118} {x + 122},{y - 70}" fill="#E57373"/>'
            f'<rect x="{x + 14}" y="{y - 44}" width="26" height="44" rx="3" fill="{door_color}"/>' + win)


def balloon(x, y, color):
    return (f'<path d="M{x},{y} q -4 30 0 44" stroke="#9E9E9E" stroke-width="2" fill="none"/>'
            f'<ellipse cx="{x}" cy="{y - 22}" rx="18" ry="24" fill="{color}"/>')


def bird(x, y, flip=False):
    s = -1 if flip else 1
    return (f'<g transform="translate({x},{y}) scale({s},1)">'
            f'<path d="M0,0 q 10 -14 22 0 q 10 -14 22 0" stroke="#455A64" stroke-width="4" fill="none" stroke-linecap="round"/></g>')


def flower(x, y, color):
    petals = "".join(f'<circle cx="{x + dx}" cy="{y + dy}" r="6" fill="{color}"/>'
                     for dx, dy in [(-7, 0), (7, 0), (0, -7), (0, 7)])
    return (f'<rect x="{x - 2}" y="{y}" width="4" height="26" fill="#66BB6A"/>' + petals +
            f'<circle cx="{x}" cy="{y}" r="5" fill="#FFD54F"/>')


def cat(x, y, tail=True):
    t = f'<path d="M{x + 30},{y - 6} q 18 -4 16 -22" stroke="#616161" stroke-width="8" fill="none" stroke-linecap="round"/>' if tail else ""
    return (f'<ellipse cx="{x}" cy="{y}" rx="30" ry="18" fill="#757575"/>'
            f'<circle cx="{x - 26}" cy="{y - 16}" r="13" fill="#757575"/>'
            f'<polygon points="{x - 35},{y - 24} {x - 32},{y - 38} {x - 25},{y - 26}" fill="#757575"/>'
            f'<polygon points="{x - 20},{y - 26} {x - 15},{y - 38} {x - 12},{y - 24}" fill="#757575"/>' + t)


def boat(x, y, sail_color):
    return (f'<path d="M{x - 45},{y} h 90 l -18 22 h -54 Z" fill="#8D6E63"/>'
            f'<rect x="{x - 2}" y="{y - 62}" width="4" height="62" fill="#5D4037"/>'
            f'<polygon points="{x + 2},{y - 62} {x + 42},{y - 12} {x + 2},{y - 12}" fill="{sail_color}"/>')


def crab(x, y, claws=2):
    claw2 = f'<circle cx="{x + 24}" cy="{y - 14}" r="8" fill="#E53935"/>' if claws == 2 else ""
    return (f'<ellipse cx="{x}" cy="{y}" rx="22" ry="14" fill="#EF5350"/>'
            f'<circle cx="{x - 8}" cy="{y - 8}" r="3.5" fill="#263238"/><circle cx="{x + 8}" cy="{y - 8}" r="3.5" fill="#263238"/>'
            f'<circle cx="{x - 24}" cy="{y - 14}" r="8" fill="#E53935"/>' + claw2)


def umbrella(x, y, color):
    return (f'<path d="M{x - 55},{y} a 55 55 0 0 1 110 0 Z" fill="{color}"/>'
            f'<rect x="{x - 2}" y="{y}" width="4" height="70" fill="#5D4037"/>')


def fish(x, y, color, flip=False):
    s = -1 if flip else 1
    return (f'<g transform="translate({x},{y}) scale({s},1)">'
            f'<ellipse cx="0" cy="0" rx="18" ry="10" fill="{color}"/>'
            f'<polygon points="-16,0 -30,-9 -30,9" fill="{color}"/>'
            f'<circle cx="8" cy="-2" r="2.5" fill="#263238"/></g>')


def star(x, y, r=7, color="#FFF59D"):
    import math
    pts = []
    for i in range(10):
        rad = r if i % 2 == 0 else r * 0.45
        a = -math.pi / 2 + i * math.pi / 5
        pts.append(f"{x + math.cos(a) * rad:.1f},{y + math.sin(a) * rad:.1f}")
    return f'<polygon points="{" ".join(pts)}" fill="{color}"/>'


def rocket(x, y, window_color):
    return (f'<g transform="translate({x},{y})">'
            f'<path d="M0,-70 q 26 30 14 78 h -28 q -12 -48 14 -78 Z" fill="#ECEFF1"/>'
            f'<circle cx="0" cy="-24" r="11" fill="{window_color}" stroke="#455A64" stroke-width="3"/>'
            f'<polygon points="-14,8 -30,34 -12,26" fill="#EF5350"/>'
            f'<polygon points="14,8 30,34 12,26" fill="#EF5350"/>'
            f'<polygon points="-8,10 0,34 8,10" fill="#FFB300"/></g>')


def planet(x, y, r, color, ring=True):
    ringpart = (f'<ellipse cx="{x}" cy="{y}" rx="{r + 18}" ry="{r * 0.42}" fill="none" stroke="#CE93D8" stroke-width="6"/>' if ring else "")
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{color}"/>' + ringpart


def alien(x, y, antennas=2):
    a2 = (f'<line x1="{x + 10}" y1="{y - 26}" x2="{x + 16}" y2="{y - 40}" stroke="#66BB6A" stroke-width="4" stroke-linecap="round"/>'
          f'<circle cx="{x + 16}" cy="{y - 42}" r="4" fill="#66BB6A"/>') if antennas == 2 else ""
    return (f'<ellipse cx="{x}" cy="{y}" rx="24" ry="20" fill="#81C784"/>'
            f'<circle cx="{x - 8}" cy="{y - 4}" r="6" fill="#263238"/><circle cx="{x + 8}" cy="{y - 4}" r="6" fill="#263238"/>'
            f'<line x1="{x - 10}" y1="{y - 26}" x2="{x - 16}" y2="{y - 40}" stroke="#66BB6A" stroke-width="4" stroke-linecap="round"/>'
            f'<circle cx="{x - 16}" cy="{y - 42}" r="4" fill="#66BB6A"/>' + a2)


# ------------------------------------------------------------------ escenas
def scene_park(variant):
    e = [f'<rect width="{W}" height="{H}" fill="#BBDEFB"/>',
         f'<rect y="330" width="{W}" height="{H - 330}" fill="#AED581"/>']
    e.append(sun(90, 85, 34, rays=not variant))                                    # dif 1: rayos del sol
    e.append(cloud(300, 70))
    e.append(cloud(520, 100, 0.8))
    e.append(house(120, 330, door_color="#5D4037", window="square" if not variant else "circle"))  # dif 2: ventana
    apples = [(-20, -15), (18, -25), (2, 10), (-30, 15)] if not variant else [(-20, -15), (18, -25), (2, 10)]
    e.append(tree(480, 340, apples))                                                # dif 3: manzana que falta
    e.append(balloon(620, 220, "#EF5350" if not variant else "#42A5F5"))            # dif 4: color del globo
    e.append(bird(280, 150))
    e.append(cat(360, 420, tail=not variant))                                       # dif 5: cola del gato
    e.append(flower(220, 410, "#F06292"))
    e.append(flower(600, 430, "#BA68C8"))
    e.append(flower(60, 440, "#F06292"))
    diffs = [(90 / W, 85 / H), (187 / W, 287 / H), (450 / W, 260 / H), (620 / W, 198 / H), (390 / W, 400 / H)]
    return e, diffs


def scene_beach(variant):
    e = [f'<rect width="{W}" height="{H}" fill="#B3E5FC"/>',
         f'<rect y="240" width="{W}" height="120" fill="#4FC3F7"/>',
         f'<rect y="360" width="{W}" height="{H - 360}" fill="#FFE082"/>']
    e.append(sun(640, 70, 32, color="#FFD54F" if not variant else "#FF8A65"))       # dif 1: color del sol
    e.append(cloud(120, 80, 0.9))
    e.append(boat(240, 300, sail_color="#EF5350" if not variant else "#66BB6A"))    # dif 2: color de la vela
    e.append(umbrella(560, 330, "#BA68C8"))
    e.append(crab(150, 420, claws=2 if not variant else 1))                          # dif 3: pinza del cangrejo
    e.append(fish(430, 300, "#FF7043"))
    if not variant:
        e.append(fish(500, 330, "#FFCA28", flip=True))                               # dif 4: pez que falta
    e.append(star(660, 420, 12, "#F48FB1"))
    e.append(star(90, 390, 10, "#F48FB1" if not variant else "#4DB6AC"))            # dif 5: color estrella de mar
    diffs = [(640 / W, 70 / H), (262 / W, 268 / H), (165 / W, 408 / H), (500 / W, 330 / H), (90 / W, 390 / H)]
    return e, diffs


def scene_space(variant):
    e = [f'<rect width="{W}" height="{H}" fill="#1A1740"/>']
    import random as _r
    rr = _r.Random(7)
    for _ in range(26):
        e.append(star(rr.randint(10, W - 10), rr.randint(10, H - 10), rr.choice([3, 4, 5])))
    e.append(planet(150, 130, 46, "#7986CB", ring=not variant))                      # dif 1: anillo del planeta
    e.append(planet(600, 90, 28, "#FF8A65" if not variant else "#4DB6AC", ring=False))  # dif 2: color planeta chico
    e.append(rocket(360, 240, window_color="#B3E5FC" if not variant else "#FFF59D"))    # dif 3: ventana del cohete
    e.append(alien(560, 380, antennas=2 if not variant else 1))                       # dif 4: antena del alien
    moon = f'<circle cx="130" cy="380" r="34" fill="#ECEFF1"/><circle cx="118" cy="370" r="7" fill="#CFD8DC"/><circle cx="142" cy="390" r="9" fill="#CFD8DC"/>'
    crater = '<circle cx="132" cy="366" r="5" fill="#CFD8DC"/>'
    e.append(moon + ("" if variant else crater))                                      # dif 5: cráter de la luna
    diffs = [(150 / W, 130 / H), (600 / W, 90 / H), (360 / W, 216 / H), (576 / W, 342 / H), (132 / W, 368 / H)]
    return e, diffs


SCENES = [("Parque", scene_park), ("Playa", scene_beach), ("Espacio", scene_space)]


def render(svg_body, out_png):
    html = (f'<!DOCTYPE html><html><head><style>*{{margin:0}}</style></head>'
            f'<body><svg width="{W}" height="{H}" xmlns="http://www.w3.org/2000/svg">{svg_body}</svg></body></html>')
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False) as f:
        f.write(html)
        path = f.name
    # Chromium recorta ~90 px del alto de viewport en --screenshot: se renderiza
    # con margen extra y se recorta al tamaño exacto con Pillow.
    subprocess.run([CHROMIUM, "--headless=new", "--no-sandbox", "--hide-scrollbars",
                    f"--screenshot={out_png}", f"--window-size={W},{H + 160}",
                    f"file://{path}"],
                   check=True, capture_output=True)
    os.unlink(path)
    from PIL import Image
    img = Image.open(out_png)
    img.crop((0, 0, W, H)).convert("RGB").save(out_png, optimize=True)


if __name__ == "__main__":
    os.makedirs(IMG_OUT, exist_ok=True)
    cs_scenes = []
    for i, (name, builder) in enumerate(SCENES, start=1):
        for suffix, variant in (("a", False), ("b", True)):
            body, diffs = builder(variant)
            out = os.path.join(IMG_OUT, f"spotdiff{i}{suffix}.png")
            render("".join(body), out)
            print(f"spotdiff{i}{suffix}.png: {os.path.getsize(out) / 1024:.0f} KB")
        diff_lits = ", ".join(f"new({cx:.4f}, {cy:.4f})" for cx, cy in diffs)
        cs_scenes.append(
            f'        new("{name}", "spotdiff{i}a.png", "spotdiff{i}b.png", [{diff_lits}]),')

    cs = ("// <auto-generated> — generado por tools/generate_spotdiff.py. No editar a mano.\n"
          "namespace BrainTrain.App.Core.Minigames;\n\n"
          "/// <summary>Centro normalizado (0..1) de una diferencia. El radio de acierto lo define el motor.</summary>\n"
          "public readonly record struct DiffPoint(double X, double Y);\n\n"
          "public sealed record SpotDiffScene(string Name, string ImageA, string ImageB, IReadOnlyList<DiffPoint> Diffs);\n\n"
          "public static class SpotDiffScenes\n{\n"
          "    public static readonly IReadOnlyList<SpotDiffScene> All =\n    [\n"
          + "\n".join(cs_scenes) +
          "\n    ];\n}\n")
    with open(CS_OUT, "w") as f:
        f.write(cs)
    print(f"SpotDiffScenes.g.cs: {len(SCENES)} escenas")

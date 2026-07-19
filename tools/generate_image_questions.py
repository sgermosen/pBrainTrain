#!/usr/bin/env python3
"""Preguntas de percepción con imagen, generadas por código (cero copyright).

Crea imágenes de conteo/atención (formas dispersas) donde la respuesta es
conocida por construcción, y emite el seed JSON con la respuesta correcta.

Uso:  python3 tools/generate_image_questions.py
Salida:
  backend/src/BrainTrain.Api/wwwroot/img/q/p{n}.png   (640×420)
  backend/src/BrainTrain.Api/Data/Seed/questions.images.es.json
"""
import json
import math
import os
import random
import subprocess
import tempfile

W, H = 640, 420
ROOT = os.path.join(os.path.dirname(__file__), "..")
IMG_OUT = os.path.join(ROOT, "backend", "src", "BrainTrain.Api", "wwwroot", "img", "q")
SEED_OUT = os.path.join(ROOT, "backend", "src", "BrainTrain.Api", "Data", "Seed", "questions.images.es.json")
CHROMIUM = "/opt/pw-browsers/chromium"

rng = random.Random(4801)


def render(svg_body, out_png):
    html = (f'<!DOCTYPE html><html><head><style>*{{margin:0}}</style></head>'
            f'<body><svg width="{W}" height="{H}" xmlns="http://www.w3.org/2000/svg">'
            f'<rect width="{W}" height="{H}" fill="#F7F5FF"/>{svg_body}</svg></body></html>')
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False) as f:
        f.write(html)
        path = f.name
    subprocess.run([CHROMIUM, "--headless=new", "--no-sandbox", "--hide-scrollbars",
                    f"--screenshot={out_png}", f"--window-size={W},{H + 160}", f"file://{path}"],
                   check=True, capture_output=True)
    os.unlink(path)
    from PIL import Image
    Image.open(out_png).crop((0, 0, W, H)).convert("RGB").save(out_png, optimize=True)


def scatter_positions(n, min_dist=52):
    """Posiciones aleatorias sin solaparse (para que todo sea contable)."""
    pts = []
    while len(pts) < n:
        x, y = rng.randint(40, W - 40), rng.randint(40, H - 40)
        if all(math.hypot(x - px, y - py) >= min_dist for px, py in pts):
            pts.append((x, y))
    return pts


def triangle(x, y, size, color, rot=0):
    pts = []
    for i in range(3):
        a = math.radians(rot + 90 + i * 120)
        pts.append(f"{x + math.cos(a) * size:.0f},{y - math.sin(a) * size:.0f}")
    return f'<polygon points="{" ".join(pts)}" fill="{color}"/>'


def star(x, y, r, color):
    pts = []
    for i in range(10):
        rad = r if i % 2 == 0 else r * 0.45
        a = -math.pi / 2 + i * math.pi / 5
        pts.append(f"{x + math.cos(a) * rad:.1f},{y + math.sin(a) * rad:.1f}")
    return f'<polygon points="{" ".join(pts)}" fill="{color}"/>'


def circle(x, y, r, color):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{color}"/>'


def square(x, y, s, color, rot=0):
    return (f'<rect x="{x - s}" y="{y - s}" width="{s * 2}" height="{s * 2}" rx="4" '
            f'fill="{color}" transform="rotate({rot} {x} {y})"/>')


PALETTE = ["#6C4DF4", "#FFB300", "#4CAF50", "#EF5350", "#42A5F5", "#EC407A"]


def count_shapes_question(idx, target_name, target_fn, decoys):
    """Imagen con N objetivos + distractores; pregunta '¿cuántos hay?'."""
    target_n = rng.randint(6, 11)
    decoy_n = rng.randint(10, 14)
    pts = scatter_positions(target_n + decoy_n)
    body = []
    for i, (x, y) in enumerate(pts):
        color = rng.choice(PALETTE)
        size = rng.randint(14, 22)
        rot = rng.randint(0, 359)
        if i < target_n:
            body.append(target_fn(x, y, size, color, rot) if target_fn in (triangle, square)
                        else target_fn(x, y, size, color))
        else:
            fn = rng.choice(decoys)
            body.append(fn(x, y, size, color, rot) if fn in (triangle, square) else fn(x, y, size, color))
    name = f"p{idx}.png"
    render("".join(body), os.path.join(IMG_OUT, name))

    options = sorted({target_n, target_n - 1, target_n + 1, target_n + 2})
    while len(options) < 4:
        options.append(max(options) + 1)
    rng.shuffle(options)
    return {
        "category": "percepcion",
        "type": "multiple_choice",
        "difficulty": rng.choice([2, 3]),
        "text": f"Observa la imagen: ¿cuántos {target_name} hay?",
        "imagePath": f"img/q/{name}",
        "choices": [{"text": str(o), "isCorrect": o == target_n} for o in options],
        "explanation": f"Hay exactamente {target_n}. Contar en grupos pequeños (de 2 o 3) y por zonas reduce los errores de conteo.",
        "funFact": "El cerebro solo 'subitiza' (cuenta de un vistazo) hasta ~4 elementos; después necesita contar."
    }


def odd_color_question(idx):
    """Cuadrícula de círculos con UNO de color distinto; pregunta la posición por fila."""
    rows, cols = 4, 6
    odd_r = rng.randint(0, rows - 1)
    body = []
    base, odd = "#6C4DF4", "#5A3FE0"  # diferencia sutil: atención real
    for r in range(rows):
        for c in range(cols):
            x = 70 + c * 100
            y = 60 + r * 100
            is_odd = r == odd_r and c == rng.randint(0, cols - 1) if False else False
            body.append(circle(x, y, 30, base))
    # coloca el distinto en una celda concreta
    odd_c = rng.randint(0, cols - 1)
    x, y = 70 + odd_c * 100, 60 + odd_r * 100
    body.append(circle(x, y, 30, odd))
    name = f"p{idx}.png"
    render("".join(body), os.path.join(IMG_OUT, name))

    labels = ["primera", "segunda", "tercera", "cuarta"]
    return {
        "category": "percepcion",
        "type": "multiple_choice",
        "difficulty": 3,
        "text": "Uno de los círculos tiene un tono ligeramente distinto. ¿En qué fila está?",
        "imagePath": f"img/q/{name}",
        "choices": [{"text": f"En la {l} fila", "isCorrect": i == odd_r} for i, l in enumerate(labels)],
        "explanation": f"Está en la {labels[odd_r]} fila. Barrer la imagen por filas, sin fijar la vista, hace saltar la diferencia de tono.",
        "funFact": "La visión periférica detecta diferencias de contraste mejor que la visión central enfocada."
    }


if __name__ == "__main__":
    os.makedirs(IMG_OUT, exist_ok=True)
    questions = []
    questions.append(count_shapes_question(1, "triángulos", triangle, [circle, square]))
    questions.append(count_shapes_question(2, "estrellas", star, [circle, triangle]))
    questions.append(count_shapes_question(3, "círculos", circle, [square, star]))
    questions.append(count_shapes_question(4, "cuadrados", square, [triangle, star]))
    questions.append(count_shapes_question(5, "triángulos", triangle, [star, square]))
    questions.append(count_shapes_question(6, "estrellas", star, [square, circle]))
    questions.append(odd_color_question(7))
    questions.append(odd_color_question(8))

    with open(SEED_OUT, "w") as f:
        json.dump({"questions": questions}, f, ensure_ascii=False, indent=2)
    print(f"{SEED_OUT}: {len(questions)} preguntas con imagen")

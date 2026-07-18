#!/usr/bin/env python3
"""Genera los audios procedurales de la sección Enfoque de BrainTrain.

Todos los sonidos son sintéticos (cero copyright) y están diseñados como
loops perfectos: los ruidos se cierran con crossfade y los binaurales usan
frecuencias con ciclos enteros dentro de la duración del loop.

Uso:  python3 tools/generate_focus_audio.py
Salida: mobile/src/BrainTrain.App/Resources/Raw/focus/*.wav
"""
import os
import struct
import wave

import numpy as np

SR = 22050          # suficiente para ruido/tonos, mitad de peso que 44.1k
LOOP_SECONDS = 12
OUT = os.path.join(os.path.dirname(__file__), "..",
                   "mobile", "src", "BrainTrain.App", "Resources", "Raw", "focus")

rng = np.random.default_rng(4801)


def save(name: str, data: np.ndarray, channels: int = 1) -> None:
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    pcm = np.clip(data, -1, 1)
    pcm = (pcm * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"{name}: {os.path.getsize(path) / 1024:.0f} KB")


def loopify(x: np.ndarray, fade_seconds: float = 0.75) -> np.ndarray:
    """Crossfade del final sobre el inicio para que el loop sea inaudible."""
    n = int(fade_seconds * SR)
    fade_in = np.linspace(0, 1, n)
    x[:n] = x[:n] * fade_in + x[-n:] * (1 - fade_in)
    return x[: len(x) - n]


def normalize(x: np.ndarray, peak: float = 0.6) -> np.ndarray:
    return x / np.max(np.abs(x)) * peak


def brown_noise(seconds: float) -> np.ndarray:
    """Ruido marrón (1/f²): grave y constante, el favorito para enmascarar ruido."""
    white = rng.standard_normal(int(seconds * SR))
    brown = np.cumsum(white)
    # High-pass suave para quitar la deriva DC del paseo aleatorio.
    alpha = 0.995
    out = np.empty_like(brown)
    prev_y = prev_x = 0.0
    for i, x in enumerate(brown):
        prev_y = alpha * (prev_y + x - prev_x)
        prev_x = x
        out[i] = prev_y
    return normalize(out)


def pink_noise(seconds: float) -> np.ndarray:
    """Ruido rosa (1/f) por conformado espectral."""
    n = int(seconds * SR)
    spectrum = np.fft.rfft(rng.standard_normal(n))
    freqs = np.fft.rfftfreq(n, 1 / SR)
    freqs[0] = freqs[1]
    spectrum /= np.sqrt(freqs)
    return normalize(np.fft.irfft(spectrum, n))


def rain(seconds: float) -> np.ndarray:
    """Lluvia sintética: cama de ruido filtrado + gotas aleatorias suaves."""
    n = int(seconds * SR)
    bed = pink_noise(seconds) * 0.8

    drops = np.zeros(n)
    for _ in range(int(seconds * 90)):           # ~90 gotas por segundo
        start = rng.integers(0, n - 400)
        length = rng.integers(80, 400)
        t = np.arange(length)
        envelope = np.exp(-t / (length / 4))
        drops[start:start + length] += rng.standard_normal(length) * envelope * rng.uniform(0.05, 0.22)

    return normalize(bed + drops)


def binaural(seconds: float, base: float, beat: float) -> np.ndarray:
    """Tonos binaurales: portadora en un oído, portadora+beat en el otro.
    base y base+beat son enteros → ciclos completos en un loop de segundos enteros."""
    t = np.arange(int(seconds * SR)) / SR
    left = np.sin(2 * np.pi * base * t)
    right = np.sin(2 * np.pi * (base + beat) * t)
    stereo = np.empty(len(t) * 2)
    stereo[0::2] = left * 0.32
    stereo[1::2] = right * 0.32
    return stereo


def chime() -> np.ndarray:
    """Campana suave de fin de sesión (2 parciales, decaimiento exponencial)."""
    seconds = 2.5
    t = np.arange(int(seconds * SR)) / SR
    tone = (np.sin(2 * np.pi * 660 * t) * 0.55 + np.sin(2 * np.pi * 990 * t) * 0.3)
    return tone * np.exp(-t * 2.2) * 0.7


if __name__ == "__main__":
    save("noise_brown.wav", loopify(brown_noise(LOOP_SECONDS + 1)))
    save("noise_pink.wav", loopify(pink_noise(LOOP_SECONDS + 1)))
    save("rain.wav", loopify(rain(LOOP_SECONDS + 1)))
    # 40 Hz (beta/gamma, "enfoque") y 6 Hz (theta, "calma") — evidencia mixta,
    # se ofrecen como opción honesta, no como magia. Requieren audífonos.
    save("binaural_focus40.wav", binaural(LOOP_SECONDS, 200, 40), channels=2)
    save("binaural_calm6.wav", binaural(LOOP_SECONDS, 200, 6), channels=2)
    save("chime.wav", chime())

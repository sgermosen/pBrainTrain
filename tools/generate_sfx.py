#!/usr/bin/env python3
"""Efectos de sonido procedurales de BrainTrain (cero copyright).

Uso:  python3 tools/generate_sfx.py
Salida: mobile/src/BrainTrain.App/Resources/Raw/sfx/*.wav (mono 22.05 kHz)
"""
import os
import wave

import numpy as np

SR = 22050
OUT = os.path.join(os.path.dirname(__file__), "..",
                   "mobile", "src", "BrainTrain.App", "Resources", "Raw", "sfx")


def save(name, data):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    pcm = (np.clip(data, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"{name}: {os.path.getsize(path) / 1024:.0f} KB")


def tone(freq, seconds, decay=8.0, harmonic=0.35):
    t = np.arange(int(seconds * SR)) / SR
    wave_ = np.sin(2 * np.pi * freq * t) + harmonic * np.sin(2 * np.pi * freq * 2 * t)
    return wave_ * np.exp(-t * decay)


def sequence(notes, note_len=0.14, gap=0.02, decay=7.0):
    parts = []
    silence = np.zeros(int(gap * SR))
    for f in notes:
        parts.append(tone(f, note_len, decay))
        parts.append(silence)
    return np.concatenate(parts) * 0.55


if __name__ == "__main__":
    # Acierto: arpegio mayor ascendente corto (C5-E5-G5).
    save("success.wav", sequence([523.25, 659.25, 783.99]))
    # Partida perfecta: fanfarria C5-E5-G5-C6 con brillo.
    perfect = sequence([523.25, 659.25, 783.99, 1046.5], note_len=0.16, decay=5.5)
    sparkle = tone(2093, 0.5, decay=6) * 0.15
    perfect[-len(sparkle):] += sparkle[: len(perfect[-len(sparkle):])]
    save("perfect.wav", perfect)
    # Moneda/cofre: dos toques agudos (estilo arcade suave).
    save("coin.wav", sequence([987.77, 1318.5], note_len=0.09, gap=0.012, decay=10) * 0.9)
    # Fallo amable: tono grave corto, sin drama (refuerzo positivo, no castigo).
    save("soft.wav", tone(220, 0.22, decay=12) * 0.4)

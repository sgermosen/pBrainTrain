# 🧠 BrainTrain

Juegos mentales, acertijos capciosos y curiosidades — para niños y adultos.
El objetivo del juego: **buscarle la lógica a las cosas**. Cada respuesta trae
su explicación; fallar también enseña.

| Carpeta | Qué es |
|---|---|
| `backend/` | API del juego en **.NET 10** (Minimal APIs, EF Core, JWT). 43 pruebas. |
| `mobile/` | App **.NET MAUI** (Android listo; iOS compilable en Mac) + Core testeable. 14 pruebas. |
| `docs/IMPLEMENTACION.md` | Arquitectura, gamificación, rendimiento, decisiones. |
| `docs/PUBLICACION.md` | Guía paso a paso: VPS, Google Play, App Store, IAP, iconos. |
| `docs/renders/` | Renders de la app en uso (mockups de las pantallas reales). |
| `docs/store-assets/` | Iconos y gráficos listos para las tiendas. |
| `Jmo.*`, `jmo_flutapp/` | Proyectos antiguos (Xamarin/netcore3.1) — reemplazados, solo referencia. |

## Arranque rápido

```bash
# Requisito: .NET 10 SDK (https://dot.net)

# 1. API con SQLite + contenido sembrado (164 preguntas, 26 logros)
cd backend/src/BrainTrain.Api && dotnet run     # http://localhost:5116

# 2. Pruebas
cd backend && dotnet test                        # backend completo
cd mobile/tests/BrainTrain.App.Core.Tests && dotnet test   # app E2E contra la API real

# 3. App Android (emulador)
cd mobile/src/BrainTrain.App && dotnet build -f net10.0-android -t:Run
```

## Características

- 🎮 Partida rápida, por categoría y **reto del día** (gratis, mantiene la racha)
- 🧠 **Entrenamiento**: 7 minijuegos — 2048, Cálculo Rápido, Sopa de Letras, Parejas de Memoria, Simón Dice, Encuentra las Diferencias y Guía del Cubo de Rubik con timer de speedcubing (XP con tope diario)
- 🧘 **Enfoque**: bloques de flow (15–90 min) con audios procedurales, respiración guiada (suspiro fisiológico, caja, exhalación larga), NSDR de 10 min y una pantalla de "la ciencia" con evidencia honesta (docs/CIENCIA-ENFOQUE.md)
- ❤️ Vidas tipo combustible (regeneran 1 cada 30 min) + tienda con monedas e IAP
- 📺 Anuncios recompensados: ver un anuncio = +1 vida (máx. 5/día)
- ⭐ **Premium** (mensual/anual): sin anuncios + conveniencia — nunca pay-to-win
- 💳 Pagos: Google Play Billing / App Store + **portal web con PayPal** (`/portal`)
- ⚔️ **Duelos 1v1** asíncronos: reta por código o rival al azar (+20 XP al ganador)
- 🎁 **Misiones diarias con cofres** y 🧪 **test inicial** que calibra tu dificultad + radar de habilidades
- 🛡️ **Ligas por divisiones** (Bronce→Leyenda) con metas semanales claras
- 🔥 Rachas, XP con niveles, 26 logros, leaderboard semanal, avatares desbloqueables
- 🔔 Push FCM opcional (racha en riesgo) + panel admin web (`/admin`) + CI GitHub Actions
- 🧩 6 categorías: Lógica, Matemática Mental, Preguntas Capciosas, Curiosidades, Percepción y Memoria, Palabras
- 🛡️ Servidor autoritativo (anti-trampas), JWT + refresh rotado, cuentas invitado ascendibles
- ⚡ Diseñado para ~1M de usuarios en un VPS de 2 cores / 4 GB (detalles en IMPLEMENTACION.md)

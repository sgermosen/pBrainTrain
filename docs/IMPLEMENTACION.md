# BrainTrain — Documento de Implementación

> App de juegos mentales, acertijos capciosos y curiosidades. Backend en **.NET 10 (LTS)**
> y app móvil en **.NET MAUI**. Objetivo: ingenio y aprendizaje con refuerzo positivo,
> soportando ~1M de usuarios registrados en un VPS de 2 cores / 4 GB RAM.

---

## 1. Resumen ejecutivo

| Componente | Tecnología | Ubicación |
|---|---|---|
| API del juego | ASP.NET Core 10 Minimal APIs | `backend/src/BrainTrain.Api` |
| Dominio | .NET 10 (entidades puras) | `backend/src/BrainTrain.Domain` |
| Acceso a datos | EF Core 10 (SQLite dev / PostgreSQL prod) | `backend/src/BrainTrain.Infrastructure` |
| App móvil | .NET MAUI (Android; iOS listo para compilar en Mac) | `mobile/src/BrainTrain.App` |
| Lógica móvil compartida | net10.0 + CommunityToolkit.Mvvm | `mobile/src/BrainTrain.App.Core` |
| Pruebas backend | xUnit (54 pruebas: unitarias + integración HTTP) | `backend/tests/BrainTrain.Tests` |
| Pruebas móviles | xUnit (49 pruebas: motores + ViewModels E2E contra la API real) | `mobile/tests/BrainTrain.App.Core.Tests` |

Los proyectos antiguos (`Jmo.*`, Xamarin/XF, netcoreapp3.1) quedan como referencia
histórica y están **reemplazados** por `backend/` y `mobile/`.

---

## 2. Arquitectura

```
┌─────────────────────────────┐        HTTPS/JSON        ┌──────────────────────────────┐
│  BrainTrain.App (MAUI)      │ ───────────────────────► │  BrainTrain.Api (.NET 10)    │
│  Pages (XAML)               │                          │  Minimal APIs + JWT          │
│  └─ BrainTrain.App.Core     │  ◄─ ProblemDetails ───   │  Servicios de juego          │
│     ViewModels / ApiClient  │                          │  ├─ Catálogo en RAM (cache)  │
│     QuizEngine (puro)       │                          │  ├─ OutputCache / RateLimit  │
└─────────────────────────────┘                          │  └─ EF Core (pooled)         │
                                                         └────────────┬─────────────────┘
                                                                      │
                                                     SQLite (dev) / PostgreSQL (prod)
```

**Principio rector:** el servidor es la única autoridad del juego (respuestas
correctas, XP, vidas, compras). El cliente solo presenta y recolecta.

---

## 3. Backend

### 3.1 Modelo de dominio

- **User** — una sola fila contiene todo el estado "caliente" del jugador (vidas,
  XP, nivel, monedas, racha, contadores agregados). El perfil completo se
  resuelve con **una lectura por clave primaria**: decisión clave para escalar.
- **Category / Question / Choice** — contenido del juego (164 preguntas seed en
  6 categorías, con `explanation` didáctica y `funFact`).
- **GameSession** — una fila por partida; las preguntas servidas van como CSV en
  la misma fila (sin tabla detalle → menos IO).
- **UserCategoryStat** — agregado por usuario+categoría. Evita guardar cada
  respuesta individual (a 1M de usuarios sería una tabla de miles de millones de filas).
- **Achievement / UserAchievement** — catálogo de 26 logros + desbloqueos.
- **DailyChallengeEntry** — PK compuesta (usuario, fecha): imposible repetir el reto.
- **PurchaseReceipt** — índice único (plataforma, transactionId): anti-replay.
- **RefreshToken** — solo hash SHA-256, rotación en cada refresh.
- **DeviceToken** — tokens push para futuro FCM/APNs.

### 3.2 Endpoints (v1)

| Método | Ruta | Auth | Notas |
|---|---|---|---|
| POST | `/api/v1/auth/guest` | — | Cuenta invitada por deviceId (entrada sin fricción) |
| POST | `/api/v1/auth/register` / `login` / `refresh` | — | Rate limit estricto (15/min/IP) |
| POST | `/api/v1/auth/upgrade` | ✔ | Invitado → cuenta completa conservando progreso |
| GET/PATCH | `/api/v1/me` | ✔ | Perfil + edición validada (avatares locales) |
| GET | `/api/v1/me/lives` | ✔ | Vidas con regeneración perezosa |
| GET | `/api/v1/me/achievements` | ✔ | Con progreso hacia cada logro |
| POST | `/api/v1/me/devices` | ✔ | Registro de token push |
| GET | `/api/v1/categories` | — | OutputCache 5 min |
| POST | `/api/v1/game/start` | ✔ | Quick/Category consumen 1 vida; Daily no |
| POST | `/api/v1/game/{id}/submit` | ✔ | Corrección server-side, XP, logros, racha |
| GET | `/api/v1/daily` | ✔ | Estado del reto del día |
| GET | `/api/v1/leaderboard/weekly` | opcional | Top 50 + mi posición |
| GET | `/api/v1/store/catalog` | — | OutputCache 5 min (incluye productos Premium) |
| POST | `/api/v1/store/purchase` | ✔ | Verificación de recibo + anti-replay |
| POST | `/api/v1/store/refill-with-coins` | ✔ | Economía blanda: monedas → vidas |
| POST | `/api/v1/ads/reward-life` | ✔ | Rewarded ad → +1 vida (tope 5/día) |
| GET | `/api/v1/minigames` | — | Catálogo de minijuegos (OutputCache) |
| POST | `/api/v1/minigames/submit` | ✔ | XP con topes por sesión y por día (anti-farmeo) |
| POST | `/api/v1/focus/complete` | ✔ | Sesión de enfoque: XP simbólico (10, tope 30/día), cuenta racha |
| GET | `/api/v1/practice/pack` | ✔ | Pack de práctica offline CON respuestas (no da XP → feedback instantáneo seguro) |
| GET | `/api/v1/paypal/config` | — | Config pública del portal (client-id) |
| POST | `/api/v1/duels` (`/join`, `/random`, `/mine`) | ✔ | Duelos 1v1: mismas preguntas, código de 6 letras o rival al azar; +20 XP al ganador |
| GET/POST | `/api/v1/quests` / `{code}/claim` | ✔ | 3 misiones diarias deterministas con cofres (contadores en la fila del usuario) |
| GET/POST | `/api/v1/avatars` / `buy` | ✔ | Tienda de avatares premium con monedas (cosméticos, no pay-to-win) |
| GET | `/api/v1/me/skills` | ✔ | Radar de precisión por categoría (perfil cerebral) |
| GET/POST/PUT/DELETE | `/api/admin/*` | X-Admin-Key | Métricas + CRUD de preguntas (panel en `/admin`) |
| POST | `/api/v1/paypal/create-order` | ✔ | Crea orden PayPal server-side |
| POST | `/api/v1/paypal/capture` | ✔ | Captura, valida monto/usuario y acredita |
| GET | `/portal` | — | Portal web de pagos (PayPal JS SDK) |
| GET | `/health` | — | Health check para monitoreo |

Errores uniformes como `ProblemDetails` (`title` = código estable, `detail` = mensaje humano).

### 3.3 Reglas de gamificación (fórmulas)

- **Niveles:** XP acumulado para nivel *n* = `50·n·(n−1)` → 100, 300, 600, 1000…
  (progreso rápido al inicio, aspiracional después).
- **XP por respuesta:** `10 × dificultad (1–5)`. Bonus: partida perfecta +25 XP
  +5 🪙; reto diario +50 XP +10 🪙; subir de nivel +20 🪙 por nivel.
- **Vidas (combustible):** máx. 5, regeneración perezosa 1 cada 30 min
  (calculada al leer, sin cron). Las compradas pueden superar el máximo (no caducan,
  pero por encima del tope no regeneran). Recarga con 100 monedas ganadas jugando.
- **Racha:** crece al completar actividad un día consecutivo (UTC); el reto
  diario no gasta vidas precisamente para que la racha siempre sea alcanzable.
- **Dificultad adaptativa:** el selector de preguntas centra la dificultad según
  la precisión histórica del jugador (empieza fácil; sube al 88%+ de acierto).
  Muestreo ponderado gaussiano — variedad garantizada.
- **Reto diario determinista:** mismas 7 preguntas para todo el mundo cada día
  (semilla = fecha), rejugable socialmente ("¿cuánto sacaste hoy?").
- **Liga semanal:** `WeeklyXp` con reinicio perezoso al cambiar de semana (lunes UTC);
  sin jobs nocturnos.
- **Premium (no pay-to-win):** `PremiumUntilUtc` en la fila del usuario. Beneficios
  de *conveniencia*: sin anuncios, 8 vidas máx. y regeneración cada 20 min. Cero
  multiplicadores de XP: el leaderboard sigue siendo justo. Se otorga por días
  (`StoreProduct.PremiumDays`) desde Play/App Store/PayPal; compras sucesivas
  extienden el vencimiento.
- **Anuncios recompensados:** ver un anuncio da +1 vida, máximo 5/día
  (contador `AdRewardsToday` con reinicio perezoso por fecha). Los Premium no ven
  anuncios (`ShowAds=false` en el perfil).
- **Minijuegos de entrenamiento** (estilo Lumosity/Peak/2048/Math Master/sopa de
  letras): el cliente juega, el servidor acredita `XP = score × factor` con tope
  por sesión y **tope diario de 300 XP** (`MinigameXpToday`), duración mínima y
  puntaje máximo por juego (anti-bots). Entrenar también cuenta para la racha.
- **Ligas por divisiones** (Bronce→Plata→Oro→Diamante→Leyenda): al cerrar cada
  semana (evaluación perezosa, sin jobs) subes si tu XP semanal superó el umbral
  del tier (150/300/500/800) y bajas si no llegaste al mínimo. Metas claras tipo
  "gana 300 XP esta semana para subir a Oro".
- **Misiones diarias con cofres**: 3 por día, deterministas por usuario+fecha
  (de un pool de 6: partidas, perfectas, aciertos, minijuegos, reto diario,
  enfoque). Progreso desde contadores diarios en la fila del usuario; recompensa
  al reclamar (monedas + XP). Cero tablas nuevas.
- **Duelos 1v1 asíncronos**: mismas 7 preguntas para ambos, por código
  compartible o emparejamiento aleatorio (pool de duelos públicos). Cuestan 1
  vida, +20 XP al ganador. La partida reutiliza el flujo del quiz.
- **Test inicial de calibración**: 10 preguntas variadas, gratis y una sola vez;
  siembra `UserCategoryStat` (dificultad adaptativa afinada desde el minuto 1) y
  alimenta el radar de habilidades del perfil.
- **Avatares premium**: 6 comprables con monedas — el destino cosmético de la
  economía blanda.
- **Push FCM** (opcional): `FcmPushSender` implementa HTTP v1 completo (OAuth de
  service account); `StreakReminderService` avisa una vez al día a quien tiene
  la racha en riesgo. Deshabilitado salvo configuración explícita.
- **Práctica offline**: `GET /practice/pack` entrega preguntas CON respuesta y
  explicación (no otorga XP, así que exponerlas es seguro); la app las cachea en
  local y ofrece feedback instantáneo por pregunta — funciona sin internet.
- **Preguntas con imagen**: `Question.ImagePath` + imágenes de percepción
  generadas por código (`tools/generate_image_questions.py`: conteo de formas y
  "tono distinto", con la respuesta conocida por construcción) servidas desde
  wwwroot y renderizadas en el quiz y la práctica.
- **Sonidos y haptics**: efectos procedurales (`tools/generate_sfx.py`) —
  celebración proporcional (perfecto/normal/suave), moneda al abrir cofres —
  y vibración sutil al responder. El fallo suena amable: refuerzo, no castigo.
- **Idiomas (ES/EN/PT)**: diccionarios en `L.cs` aplicados a las pantallas
  principales con selector en Ajustes (el contenido del juego sigue en español;
  el patrón permite completar el resto de pantallas incrementalmente).

### 3.4 Seguridad

- JWT HS256 (60 min) + refresh tokens rotados (60 días), almacenados **solo como hash**.
- La clave JWT **no existe en el repositorio**: en producción la app se niega a
  arrancar sin `Jwt__Key` (variable de entorno). En dev usa una clave marcada dev-only.
- Contraseñas con `PasswordHasher` (PBKDF2) de ASP.NET Identity.
- Anti-trampas: las respuestas correctas **nunca** viajan al cliente antes del
  submit; solo cuentan respuestas de preguntas servidas en esa sesión; sesiones
  expiran (60 min) y tienen edad mínima (4 s); anti-replay de partidas y de recibos.
- Rate limiting global (120/min) y de credenciales (15/min/IP).
- Verificación de compras en servidor (`IPurchaseVerifier`); el modo Test solo
  funciona si `Store:AllowTestReceipts=true` (nunca en producción).
- Sin subida de contenido de usuario (avatares = catálogo local): superficie
  mínima y apto para niños.

### 3.5 Rendimiento para 1M de usuarios en VPS 2c/4GB

Presupuesto de recursos: con 1M de usuarios registrados y ~2–5% activos/día
(20–50k DAU, pico ~100 req/s), el diseño mantiene el p95 bajo:

1. **Catálogo de preguntas 100% en RAM** (`QuestionCatalog`, refresco 15 min):
   servir preguntas y corregir partidas **no toca la base de datos** para el contenido.
2. **Una fila por usuario** para el estado del juego → perfil/vidas = 1 SELECT por PK.
3. **Agregados, no eventos**: contadores en `User` y `UserCategoryStat` en vez de
   una tabla de respuestas individuales.
4. **`AddDbContextPool`** (reutiliza contextos), **`AsNoTracking`** en todas las lecturas.
5. **OutputCache** en contenido público; **compresión Brotli/Gzip** (~80% menos bytes).
6. **JSON source generators** (sin reflexión en serialización).
7. **Índices cubrientes**: `(WeekStartUtc, WeeklyXp DESC)` para leaderboard;
   `(CategoryId, IsActive, Difficulty)` para selección; únicos para email/device/tx.
8. **Workstation GC** (`ServerGarbageCollection=false`): ~150–250 MB estables de RAM
   para la API, dejando 3+ GB a PostgreSQL y SO.
9. **Rate limiting** como amortiguador de picos y abuso.
10. Todo el trabajo periódico es **perezoso** (vidas, semanas): cero cron jobs, cero
    barridos nocturnos sobre tablas de millones de filas.

### 3.6 Contenido seed

`backend/src/BrainTrain.Api/Data/Seed/questions.es.json` (164 preguntas es-LA,
6 categorías, dificultad 1–5, cada una con explicación didáctica) y
`achievements.es.json` (26 logros bronze→diamond). Se cargan automáticamente al
primer arranque con base vacía (`Database:SeedOnStartup`).

### 3.7 Configuración

Todo en `appsettings.json` (sin secretos) sobrescribible por variables de entorno:
`Database:Provider` (`sqlite`|`postgres`), `ConnectionStrings__Postgres`,
`Jwt__Key`, `Game:*` (todas las reglas del juego son ajustables sin recompilar),
`Store:Products` (catálogo IAP), `RateLimiting:*`.

---

## 4. App móvil (.NET MAUI)

### 4.1 Estructura

- **BrainTrain.App.Core** (net10.0, sin dependencia de MAUI): modelos, `ApiClient`
  (Bearer + auto-refresh en 401), `QuizEngine` (estado puro de partida), y todos
  los ViewModels. Esto hace la lógica **testeable en CI sin emulador**.
- **BrainTrain.App** (MAUI): XAML + servicios de plataforma:
  - `SecureStorageTokenStore` — tokens en Keystore/Keychain.
  - `AndroidReminderScheduler` — recordatorio diario nativo (AlarmManager +
    BroadcastReceiver + reprogramación tras reinicio). Sin dependencias externas.
  - `SandboxPurchaser` — compras sandbox para desarrollo (el backend valida
    "TEST-OK" solo en dev). Producción: Google Play Billing / StoreKit
    (pasos exactos en PUBLICACION.md).

### 4.2 Pantallas

| Pantalla | Ruta | Qué hace |
|---|---|---|
| Entrenamiento | `training` | Catálogo de 7 minijuegos |
| Parejas de Memoria | `memorypairs` | 16 cartas, 8 parejas de emojis (memoria de trabajo) |
| Simón Dice | `simon` | Secuencia de 4 colores que crece por ronda |
| Encuentra las Diferencias | `spotdiff` | 3 escenas SVG generadas por código, 5 diferencias c/u, toque validado por regiones normalizadas |
| Guía Cubo de Rubik | `rubikguide` | Método por capas en 9 pasos (patrones 3×3 + algoritmos) y timer con mejor tiempo y Ao5 |
| Enfoque | `focus` | Hub: bloque de foco, calma/respiración y "la ciencia" |
| Bloque de foco | `focustimer` | Meta única + 15/25/50/90 min + sonido de fondo (loops locales) |
| Respiración | `breathe` | Suspiro fisiológico, caja, exhalación larga y NSDR con pacer animado |
| La ciencia | `focusscience` | Evidencia honesta por técnica (qué está probado y qué no) |
| 2048 | `game2048` | Motor clásico 4×4 con swipe; "cobrar XP" en cualquier momento |
| Cálculo Rápido | `mathsprint` | 60 s de operaciones con dificultad progresiva (sin negativos) |
| Sopa de Letras | `wordsearch` | Cuadrícula 10×10, selección por 2 toques, 8 direcciones |
| Onboarding | `//onboarding` | Nombre + avatar y **jugar en <10 segundos** como invitado |
| Auth | `auth` | Login / registro / ascenso de invitado |
| Home | `//home` | Vidas, racha, monedas, nivel con barra, **reto del día** destacado, partida rápida, categorías |
| Quiz | `quiz` | 1 pregunta a la vez, temporizador de 25 s, lock-in de respuesta |
| Resultados | `results` | Celebración proporcional, XP/monedas, logros desbloqueados y **repaso didáctico con la lógica de cada respuesta** |
| Perfil | `profile` | Estadísticas, precisión, avatar |
| Logros | `achievements` | 26 logros con barra de progreso, ordenados por cercanía |
| Liga | `leaderboard` | Top 50 semanal + tu posición |
| Tienda | `store` | Recarga con monedas + paquetes IAP |
| Ajustes | `settings` | Recordatorio diario (hora configurable), cuenta, sesión |

### 4.3 Decisión de diseño: feedback al final, no por pregunta

El cliente **no conoce** las respuestas correctas durante la partida (anti-trampas
y menor payload). El refuerzo llega al final: puntaje, celebración y el repaso
pregunta-a-pregunta con explicaciones — que es donde ocurre el aprendizaje real.

### 4.4 Psicología de gamificación aplicada (y dónde vive)

| Mecánica | Implementación |
|---|---|
| Entrada sin fricción | Cuenta invitada por deviceId (onboarding) |
| Hábito / racha | `StreakDays` + reto diario gratis + recordatorio local configurable |
| Aversión a la pérdida (suave) | Racha visible; el reto diario nunca depende de vidas |
| Economía dual | Vidas (escasez temporal) + monedas (recompensa por jugar) |
| Recompensa variable | Preguntas aleatorias ponderadas, funFacts sorpresa |
| Metas escalonadas | Logros bronze→diamond, ordenados por "casi lo tengo" |
| Progreso visible | Barra de nivel, XP por partida, contador de logros |
| Competencia sana | Liga semanal que se reinicia (siempre hay nueva oportunidad) |
| Celebración proporcional | Titulares según desempeño ("¡PERFECTO!" vs "¡Cada error enseña!") |
| Refuerzo positivo, no castigo | Sin puntos negativos; fallar muestra la lógica y un dato curioso |
| Dificultad adaptativa | Flujo (Csikszentmihalyi): reto ≈ habilidad, ni aburre ni frustra |
| Upgrade en el momento justo | "Guarda tu progreso" cuando ya hay progreso que perder |

---

## 4.5 Inspiración y qué tomamos de cada app

| Referencia | Qué adoptamos |
|---|---|
| Preguntados / Trivia Crack | Quiz por categorías con emojis y colores, vidas |
| Lumosity / Peak / NeuroNation | Sección "Entrenamiento" con minijuegos cortos y XP |
| 2048 / Twenty | Minijuego 2048 completo (motor propio) |
| Math Master / Logimathics | Cálculo Rápido con dificultad progresiva |
| Sopa de Letras / apps de palabras | Sopa de Letras 10×10 en español |
| Brain Test / acertijos capciosos | Categoría "Preguntas Capciosas" con trampa + explicación |
| Duolingo | Rachas, recordatorios, celebración proporcional, cuenta invitada |
| Brain Wars / Brainia | Liga semanal competitiva con reinicio |
| Apps de "flow"/"la zona" (Focus, Impulse, MentalUP) | Sección **Enfoque**: bloques de foco, respiración guiada, NSDR y audios — todo con nivel de evidencia visible (ver docs/CIENCIA-ENFOQUE.md) |

Ya implementados sobre ese modelo: **Parejas de Memoria**, **Simón Dice**,
**Encuentra las Diferencias** (escenas SVG→PNG generadas por
`tools/generate_spotdiff.py` con regiones de acierto normalizadas emitidas a
`SpotDiffScenes.g.cs`) y la **Guía del Cubo de Rubik** (método por capas, 9
pasos con patrón de cara objetivo y timer de speedcubing con Ao5).

Ideas restantes para una v2: "Solve in 30s", ilusiones ópticas con explicación
perceptual, más escenas de diferencias (solo re-ejecutar el script con escenas
nuevas), duelos asíncronos 1v1, packs por temporada.

## 5. Pruebas (cómo ejecutarlas)

```bash
# Backend: 54 pruebas (progresión, selección de preguntas, y flujo HTTP
# completo: auth, partidas, vidas, tienda, reto diario, liga, premium,
# rewarded ads, minijuegos con topes y PayPal con gateway simulado)
cd backend && dotnet test

# Móvil: 49 pruebas — motores de los 7 minijuegos, respiración/enfoque y E2E de los
# ViewModels reales contra el backend real en memoria (partida completa,
# sin vidas → tienda, compra sandbox, premium, vida por anuncio, reto diario,
# recordatorios, upgrade de cuenta, auto-refresh de token)
cd mobile/tests/BrainTrain.App.Core.Tests && dotnet test
```

Ambas suites usan SQLite temporal con el seed completo: cada corrida ejercita
también la carga de contenido real.

## 6. Correr todo en local

```bash
# API (puerto 5116, entorno Development: SQLite + seed automático)
cd backend/src/BrainTrain.Api && dotnet run

# Probar a mano
curl http://localhost:5116/health
curl -X POST http://localhost:5116/api/v1/auth/guest \
  -H 'Content-Type: application/json' -d '{"deviceId":"mi-dispositivo-12345"}'

# App Android (emulador; la app apunta a 10.0.2.2:5116 en DEBUG)
cd mobile/src/BrainTrain.App
dotnet build -f net10.0-android -t:Run
```

## 7. Limitaciones conocidas y siguientes pasos

1. **Billing real**: la tienda funciona end-to-end en sandbox; falta conectar
   Google Play Billing/StoreKit y el verificador de recibos de producción
   (interfaz lista: `IPurchaseVerifier`; pasos en PUBLICACION.md §2.6 y §4).
2. **AdMob real**: `IAdService` y el flujo rewarded→vida están completos con el
   sandbox; la integración AdMob de producción está documentada en PUBLICACION.md §6.
3. **PayPal**: integración real implementada (Orders v2 + portal); solo requiere
   credenciales (`PayPal__ClientId/Secret`) — PUBLICACION.md §7.3.
4. **Push remoto**: los recordatorios son locales (no requieren servidor);
   `DeviceToken` ya existe para sumar FCM/APNs después.
5. **iOS**: el código es multiplataforma pero el binario iOS debe compilarse en Mac.
6. Ideas de v2: modo duelo asíncrono, preguntas con imagen (`_Resources/` tiene
   material), más minijuegos (ver §4.5), packs por temporada, sonidos/haptics.

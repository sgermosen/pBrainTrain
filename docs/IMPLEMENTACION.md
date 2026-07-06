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
| Pruebas backend | xUnit (43 pruebas: unitarias + integración HTTP) | `backend/tests/BrainTrain.Tests` |
| Pruebas móviles | xUnit (11 pruebas: ViewModels E2E contra la API real) | `mobile/tests/BrainTrain.App.Core.Tests` |

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
| GET | `/api/v1/store/catalog` | — | OutputCache 5 min |
| POST | `/api/v1/store/purchase` | ✔ | Verificación de recibo + anti-replay |
| POST | `/api/v1/store/refill-with-coins` | ✔ | Economía blanda: monedas → vidas |
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

## 5. Pruebas (cómo ejecutarlas)

```bash
# Backend: 43 pruebas (lógica de progresión, selección de preguntas,
# y flujo HTTP completo: auth, partidas, vidas, tienda, reto diario, liga)
cd backend && dotnet test

# Móvil: 11 pruebas E2E — los ViewModels reales ejecutan sus comandos
# contra el backend real en memoria (partida completa, sin vidas → tienda,
# compra sandbox, reto diario, recordatorios, upgrade de cuenta, auto-refresh)
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
   (interfaz lista: `IPurchaseVerifier`; pasos en PUBLICACION.md).
2. **Push remoto**: los recordatorios son locales (no requieren servidor);
   `DeviceToken` ya existe para sumar FCM/APNs después.
3. **iOS**: el código es multiplataforma pero el binario iOS debe compilarse en Mac.
4. Ideas de v2: modo duelo asíncrono (el esquema `preguntados.sql` original da pistas),
   preguntas con imagen (`_Resources/` tiene material), packs de contenido por temporada,
   sonidos/haptics en la app.

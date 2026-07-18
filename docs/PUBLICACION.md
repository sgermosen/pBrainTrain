# BrainTrain — Guía de Publicación Paso a Paso

> Todo lo que hay que hacer para poner BrainTrain en producción: servidor VPS,
> Google Play y App Store. Cada paso incluye los comandos exactos.

---

## 0. Checklist rápido (imprime esto)

- [ ] VPS con Ubuntu 24.04, dominio apuntando al servidor (ej. `api.tudominio.com`)
- [ ] PostgreSQL instalado y ajustado (sección 1.3)
- [ ] `Jwt__Key` generada y en variables de entorno (¡nunca en git!)
- [ ] API publicada con systemd + nginx + HTTPS (secciones 1.4–1.6)
- [ ] `Store:AllowTestReceipts=false` en producción (es el valor por defecto)
- [ ] URL de producción puesta en `MauiProgram.ApiBaseUrl` (Release)
- [ ] Keystore Android creado y respaldado (sección 2.2)
- [ ] Cuenta Google Play ($25 una vez) y/o Apple Developer ($99/año)
- [ ] Productos IAP creados con los IDs exactos de `appsettings.json` (incluye `braintrain.premium.month` y `braintrain.premium.year`)
- [ ] Billing real conectado (sección 2.6 / 3.5) y verificador de recibos del servidor (sección 4)
- [ ] AdMob configurado (sección 6) y `SandboxAdService` reemplazado por la implementación real
- [ ] PayPal: app creada en developer.paypal.com y `PayPal__ClientId`/`PayPal__Secret` en el servidor (sección 7)
- [ ] Portal de pagos accesible en `https://api.tudominio.com/portal`
- [ ] Política de privacidad publicada en una URL (obligatoria en ambas tiendas; menciona anuncios y compras)

---

## 1. Servidor (VPS 2 cores / 4 GB)

### 1.1 Preparación del sistema

```bash
# Como root en Ubuntu 24.04
adduser braintrain --disabled-password
apt update && apt upgrade -y
apt install -y nginx postgresql postgresql-contrib ufw

# Firewall: solo SSH y web
ufw allow OpenSSH && ufw allow 'Nginx Full' && ufw enable
```

### 1.2 Instalar el runtime de .NET 10

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
  --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
```

### 1.3 PostgreSQL: crear base y ajustar para 4 GB de RAM

```bash
sudo -u postgres psql <<'SQL'
CREATE USER braintrain WITH PASSWORD 'CAMBIA-ESTA-CONTRASEÑA';
CREATE DATABASE braintrain OWNER braintrain;
SQL
```

Edita `/etc/postgresql/16/main/postgresql.conf` (valores para 4 GB compartidos
con la API — deja ~2.5 GB a PostgreSQL):

```conf
shared_buffers = 768MB          # ~25% de la RAM disponible para PG
effective_cache_size = 2GB
work_mem = 8MB
maintenance_work_mem = 128MB
max_connections = 60            # la API usa pooling; no necesitas más
wal_compression = on
random_page_cost = 1.1          # SSD
```

```bash
systemctl restart postgresql
```

> Con este diseño (1 fila por usuario, agregados, catálogo en RAM) una tabla de
> 1M de usuarios ocupa ~300–500 MB con índices: cabe cómodamente en cache.

### 1.4 Publicar la API

En tu máquina de desarrollo (o CI):

```bash
cd backend/src/BrainTrain.Api
dotnet publish -c Release -o publish
rsync -av publish/ braintrain@TU-SERVIDOR:/home/braintrain/api/
```

### 1.5 Variables de entorno y systemd

Genera la clave JWT (una sola vez, guárdala en un gestor de contraseñas):

```bash
openssl rand -base64 64
```

Crea `/etc/systemd/system/braintrain.service`:

```ini
[Unit]
Description=BrainTrain API
After=network.target postgresql.service

[Service]
WorkingDirectory=/home/braintrain/api
ExecStart=/usr/bin/dotnet /home/braintrain/api/BrainTrain.Api.dll
Restart=always
RestartSec=5
User=braintrain
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=Database__Provider=postgres
Environment=ConnectionStrings__Postgres=Host=localhost;Database=braintrain;Username=braintrain;Password=CAMBIA-ESTA-CONTRASEÑA;Maximum Pool Size=40
Environment=Jwt__Key=PEGA-AQUI-LA-CLAVE-GENERADA
# Límite de memoria de seguridad para el proceso de la API
MemoryMax=1200M

[Install]
WantedBy=multi-user.target
```

```bash
systemctl daemon-reload
systemctl enable --now braintrain
curl http://127.0.0.1:5000/health   # → Healthy (aplica migraciones y seed solo)
```

### 1.6 nginx + HTTPS (certbot)

`/etc/nginx/sites-available/braintrain`:

```nginx
server {
    server_name api.tudominio.com;
    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

```bash
ln -s /etc/nginx/sites-available/braintrain /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
apt install -y certbot python3-certbot-nginx
certbot --nginx -d api.tudominio.com     # HTTPS automático + renovación
```

### 1.7 Respaldos y monitoreo

```bash
# Respaldo diario 03:00 (crontab -e como braintrain)
0 3 * * * pg_dump -Fc braintrain > /home/braintrain/backups/braintrain-$(date +\%F).dump && find /home/braintrain/backups -mtime +14 -delete

# Logs y estado
journalctl -u braintrain -f
# Monitoreo externo gratuito: apunta UptimeRobot/BetterStack a https://api.tudominio.com/health
```

### 1.8 Actualizaciones

```bash
dotnet publish -c Release -o publish && rsync -av publish/ braintrain@SERVIDOR:/home/braintrain/api/
ssh braintrain@SERVIDOR 'sudo systemctl restart braintrain'
# Las migraciones EF Core se aplican solas al arrancar (Database:AutoMigrate=true)
```

---

## 2. Google Play (Android)

### 2.1 Cuenta

1. https://play.google.com/console → registro de desarrollador (**$25 una sola vez**).
2. Verifica identidad (1–3 días la primera vez).

### 2.2 Keystore de firma (¡respáldalo!)

```bash
keytool -genkeypair -v -keystore braintrain-release.keystore \
  -alias braintrain -keyalg RSA -keysize 4096 -validity 10000
```

> **Si pierdes este archivo o su contraseña no podrás actualizar la app.**
> Guarda copia cifrada fuera del servidor y NUNCA lo subas a git.

### 2.3 Configurar la app para Release

En `mobile/src/BrainTrain.App/MauiProgram.cs` pon tu dominio real en `ApiBaseUrl` (bloque `#else`).

Compila el bundle firmado (las propiedades de firma van por línea de comandos,
no en el csproj, para no filtrar secretos):

```bash
cd mobile/src/BrainTrain.App
dotnet publish -f net10.0-android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=/ruta/braintrain-release.keystore \
  -p:AndroidSigningKeyAlias=braintrain \
  -p:AndroidSigningKeyPass=env:KEY_PASS \
  -p:AndroidSigningStorePass=env:STORE_PASS
# Resultado: bin/Release/net10.0-android/publish/com.sgrysoft.braintrain-Signed.aab
```

### 2.4 Crear la ficha en Play Console

1. **Crear app** → Nombre: `BrainTrain — Juegos Mentales`; idioma: español (Latinoamérica); Gratis; Es un juego.
2. **Ficha de la tienda** (textos sugeridos, edítalos a gusto):
   - *Descripción corta (80):* «Acertijos, lógica y preguntas capciosas. Entrena tu ingenio jugando.»
   - *Descripción larga:* explica reto diario, rachas, logros, liga semanal, 6
     categorías, apto para niños y adultos, y que fallar enseña (cada pregunta
     trae su explicación).
   - *Icono 512×512 PNG:* `docs/store-assets/icon-512.png` (generado en este repo).
   - *Gráfico destacado 1024×500:* `docs/store-assets/feature-1024x500.png`.
   - *Capturas (mín. 2, 1080×1920):* usa los renders de `docs/renders/` o capturas reales del emulador.
3. **Clasificación de contenido:** cuestionario → categoría "Trivia", sin violencia → normalmente PEGI 3 / Everyone.
4. **Seguridad de datos:** declara: email (opcional, solo cuentas registradas),
   identificador de dispositivo (cuentas invitadas), compras. Sin publicidad. Sin datos compartidos con terceros.
5. **Público objetivo:** si marcas "incluye niños", Play exige política familiar
   (BrainTrain cumple: sin chat, sin UGC, sin anuncios).
6. **Política de privacidad:** URL obligatoria (publícala en tu dominio).

### 2.5 Productos dentro de la app

Play Console → Monetizar → Productos → Productos integrados → crea **exactamente estos IDs**
(deben coincidir con `appsettings.json` del backend):

| ID | Nombre | Precio sugerido |
|---|---|---|
| `braintrain.lives.refill` | Tanque lleno (5 vidas) | $0.99 |
| `braintrain.lives.pack15` | Reserva de energía (15 vidas) | $1.99 |
| `braintrain.coins.pack300` | Bolsa de monedas (300) | $1.99 |
| `braintrain.coins.pack1000` | Cofre de monedas (1000) | $4.99 |

### 2.6 Conectar el billing real en la app

La app ya tiene la interfaz lista (`IPlatformPurchaser`). Para producción:

```bash
cd mobile/src/BrainTrain.App
dotnet add package Plugin.InAppBilling
```

Crea `Services/PlayStorePurchaser.cs`:

```csharp
using Plugin.InAppBilling;
using BrainTrain.App.Core;

public sealed class PlayStorePurchaser : IPlatformPurchaser
{
    public async Task<PlatformPurchase?> BuyAsync(string productId, CancellationToken ct = default)
    {
        var billing = CrossInAppBilling.Current;
        try
        {
            if (!await billing.ConnectAsync()) return null;
            var purchase = await billing.PurchaseAsync(productId, ItemType.InAppPurchase);
            if (purchase is null) return null;
            // El token de compra es el "recibo" que valida nuestro backend
            return new PlatformPurchase("GooglePlay", purchase.TransactionIdentifier ?? purchase.Id, purchase.PurchaseToken);
        }
        finally { await billing.DisconnectAsync(); }
    }
}
```

y en `MauiProgram.cs` cambia el registro en Release:

```csharp
#if DEBUG
        services.AddSingleton<IPlatformPurchaser, SandboxPurchaser>();
#else
        services.AddSingleton<IPlatformPurchaser, PlayStorePurchaser>();
#endif
```

> Nota: los productos consumibles deben consumirse (`billing.ConsumePurchaseAsync`)
> tras el canje exitoso en el backend, para poder recomprarlos.

### 2.7 Lanzamiento

1. **Pruebas internas** → sube el `.aab` → agrega tu email como tester → prueba compra sandbox (Play permite testers de licencia sin cobro real).
2. Corrige, sube versión (`ApplicationVersion` +1 en el csproj) y pasa a **Producción**.
3. Primera revisión: 1–7 días.

---

## 3. App Store (iOS)

### 3.1 Requisitos

- Mac con Xcode 16+ (obligatorio para compilar iOS).
- Cuenta Apple Developer (**$99/año**): https://developer.apple.com.
- En la Mac: `dotnet workload install maui-ios`.

### 3.2 Identidad y perfiles

1. developer.apple.com → Identifiers → App ID `com.sgrysoft.braintrain`
   (capability: In-App Purchase).
2. Xcode → Settings → Accounts → tu Apple ID → "Manage Certificates" →
   crea el certificado de distribución (o usa firma automática).

### 3.3 Compilar y subir

```bash
cd mobile/src/BrainTrain.App
dotnet publish -f net10.0-ios -c Release \
  -p:ArchiveOnBuild=true \
  -p:CodesignKey="Apple Distribution: TU NOMBRE (TEAMID)" \
  -p:CodesignProvision="BrainTrain AppStore"
# Sube el .ipa con Transporter (App Store Connect → apps de macOS)
```

### 3.4 App Store Connect

1. Crear app → bundle `com.sgrysoft.braintrain`, nombre `BrainTrain — Juegos Mentales`, español (México/Latam).
2. Capturas: 6.9" (1320×2868) y 6.5" (1284×2778) — usa los renders como base o el simulador.
3. Clasificación: 4+. Privacidad: igual que en Play (email opcional, deviceId, compras).
4. **IAP**: crea los mismos 4 productos (tipo *Consumable*) con los mismos IDs.
5. Notas para revisión: indica que se puede jugar como invitado (sin login) —
   Apple lo exige y BrainTrain ya lo cumple.

### 3.5 StoreKit en la app

El mismo `Plugin.InAppBilling` funciona en iOS: `PlayStorePurchaser` sirve tal
cual (cambia `"GooglePlay"` por `DeviceInfo.Platform == DevicePlatform.iOS ?
"AppStore" : "GooglePlay"` y usa `purchase.TransactionIdentifier` como recibo).

---

## 4. Verificación de recibos en el servidor (producción)

El backend rechaza toda compra no verificada. Para producción implementa en
`backend/src/BrainTrain.Api/Services/StoreService.cs` (interfaz `IPurchaseVerifier`):

- **Google:** crea una *service account* en Google Cloud → dale acceso en Play
  Console (Configuración → Acceso a API) → llama a
  `androidpublisher.googleapis.com/androidpublisher/v3/applications/{pkg}/purchases/products/{productId}/tokens/{token}`
  y comprueba `purchaseState == 0`. Guarda el JSON de la service account como
  secreto del servidor (variable de entorno o archivo con permisos 600).
- **Apple:** usa la App Store Server API (`GET /inApps/v1/transactions/{id}`)
  con una clave de App Store Connect (tipo *In-App Purchase*), verificando el JWS.

Ambas integraciones son ~100 líneas; la interfaz, el anti-replay y el registro
de recibos ya están hechos y probados.

---

## 5. Iconos, splash y assets

- Fuente de verdad: `mobile/src/BrainTrain.App/Resources/AppIcon/appicon.svg` +
  `appiconfg.svg` y `Resources/Splash/splash.svg`. **MAUI genera automáticamente
  todos los tamaños** (mipmaps Android, AppIcon iOS, splash) al compilar.
- Para las fichas de tienda: `docs/store-assets/icon-512.png` (Play),
  `icon-1024.png` (App Store) y `feature-1024x500.png` (gráfico destacado Play).
- Renders de la app en uso (mockups de las pantallas reales): `docs/renders/*.png`
  — útiles directamente como material de marketing o base para capturas.

## 6. Anuncios con AdMob (banner + rewarded)

La app ya trae la abstracción `IAdService` y el flujo completo funcionando en
sandbox: el botón «Ver anuncio» de la tienda muestra el rewarded y el servidor
acredita **+1 vida con tope de 5/día** (`POST /api/v1/ads/reward-life`).
Los usuarios Premium no ven anuncios (`profile.showAds == false` oculta la UI).

### 6.1 Crear las unidades de anuncio

1. https://admob.google.com → registra la app (Android y/o iOS) → copia el
   **App ID** (`ca-app-pub-XXXX~YYYY`).
2. Crea 2 unidades: **Recompensado** (vida gratis) y **Banner** (opcional, en Home).
   Guarda sus IDs (`ca-app-pub-XXXX/ZZZZ`).
3. Vincula AdMob con la ficha de Play cuando la app esté publicada (mejora eCPM).

### 6.2 Integrar en la app MAUI

```bash
cd mobile/src/BrainTrain.App
dotnet add package Plugin.MauiMTAdmob
```

`MauiProgram.cs`: `builder.UseMauiMTAdmob();` y en `Platforms/Android/AndroidManifest.xml`:

```xml
<meta-data android:name="com.google.android.gms.ads.APPLICATION_ID"
           android:value="ca-app-pub-XXXX~YYYY"/>
```

Crea `Services/AdmobAdService.cs` implementando `IAdService`:

```csharp
using Plugin.MauiMTAdmob;
using BrainTrain.App.Core;

public sealed class AdmobAdService : IAdService
{
    private const string RewardedId = "ca-app-pub-XXXX/ZZZZ"; // unidad recompensada
    public bool BannerEnabled => true;

    public async Task<bool> ShowRewardedAdAsync(CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<bool>();
        CrossMauiMTAdmob.Current.OnRewardedLoaded += (_, _) => CrossMauiMTAdmob.Current.ShowRewarded();
        CrossMauiMTAdmob.Current.OnUserEarnedReward += (_, _) => tcs.TrySetResult(true);
        CrossMauiMTAdmob.Current.OnRewardedFailedToLoad += (_, _) => tcs.TrySetResult(false);
        CrossMauiMTAdmob.Current.OnRewardedClosed += (_, _) => tcs.TrySetResult(false);
        CrossMauiMTAdmob.Current.LoadRewarded(RewardedId);
        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
    }

    public Task ShowInterstitialIfDueAsync(CancellationToken ct = default) => Task.CompletedTask;
}
```

y en `MauiProgram.cs` (Release): `services.AddSingleton<IAdService, AdmobAdService>();`
en lugar de `SandboxAdService`. Usa los **IDs de prueba de Google** hasta publicar
(rewarded test: `ca-app-pub-3940256099942544/5224354917`).

### 6.3 Reglas importantes

- App dirigida también a niños → en AdMob marca **tratamiento para menores**
  (TFCD) y usa solo anuncios aptos; en Play declara "sí muestra anuncios".
- No pongas banner dentro del quiz (política de UX y de Google: nada de clics accidentales).
- Opcional avanzado: verificación server-side (SSV) de rewarded — AdMob puede
  llamar a tu servidor firmando cada recompensa; nuestro endpoint con tope
  diario ya limita el abuso sin SSV.

## 7. Pagos: Google Pay / Play Billing, suscripciones y PayPal

### 7.1 ¿"Google Pay" o "Play Billing"? (importante)

Para **bienes digitales dentro de la app** (vidas, monedas, Premium) Google
**obliga** a usar **Google Play Billing** — no la API de Google Pay (esa es para
bienes físicos/servicios). "Pagar con Google" les aparece automáticamente a los
usuarios dentro del flujo de Play Billing con sus tarjetas guardadas. Los pasos
completos ya están en la sección 2.5–2.6. En resumen:

1. Play Console → Monetizar → Productos integrados → crea los 6 IDs (tabla 2.5 + los 2 premium).
2. App: `Plugin.InAppBilling` + `PlayStorePurchaser` (código en 2.6).
3. Backend: verificador de recibos con service account (sección 4).

### 7.2 Premium: ¿producto consumible o suscripción real?

BrainTrain implementa Premium como **producto de días** (30/365): simple,
funciona igual en Play, App Store y PayPal, y no requiere webhooks. Si más
adelante quieres **suscripción auto-renovable** de Play/App Store:

1. Play Console → Monetizar → Suscripciones → crea `braintrain.premium.sub` con
   plan base mensual.
2. Configura **RTDN** (Real-Time Developer Notifications) hacia un endpoint
   nuevo del backend vía Pub/Sub para enterarte de renovaciones/cancelaciones y
   actualizar `PremiumUntilUtc`.
3. En App Store: App Store Server Notifications V2 (JWS) al mismo efecto.

### 7.3 PayPal en el portal web (ya implementado)

El backend incluye la integración completa de **PayPal Orders v2** y un portal
web en `/portal`: el usuario inicia sesión con su cuenta del juego, elige
producto y paga con PayPal; el servidor crea la orden, la captura, valida monto
y usuario, y acredita con anti-replay (`PurchaseReceipts`).

Pasos para activarlo:

1. https://developer.paypal.com → **Apps & Credentials** → Create App (tipo
   Merchant). Copia **Client ID** y **Secret** de *Sandbox*.
2. En el servidor (systemd, junto a las demás variables):

```ini
Environment=PayPal__ClientId=TU-CLIENT-ID
Environment=PayPal__Secret=TU-SECRET
Environment=PayPal__Mode=sandbox     # cambia a "live" al salir a producción
```

3. Prueba en `https://api.tudominio.com/portal` con una cuenta sandbox de
   comprador (developer.paypal.com → Testing Tools → Sandbox Accounts).
4. Para producción: crea/usa las credenciales **Live** de la misma app,
   `PayPal__Mode=live`, y haz una compra real pequeña de verificación.
5. Los precios PayPal salen de `Store:Products[].PriceUsd` en `appsettings.json`.

> Nota de políticas: vender bienes digitales por web (portal) es válido; lo que
> las tiendas prohíben es ofrecer *dentro de la app* enlaces para pagar por
> fuera. No enlaces el portal desde la app iOS/Android.

## 8. Costos y capacidad (referencia)

| Concepto | Costo |
|---|---|
| VPS 2c/4GB (Hetzner/Contabo/DO) | ~$6–15/mes |
| Dominio + TLS (Let's Encrypt) | ~$12/año + gratis |
| Google Play | $25 una vez |
| Apple Developer | $99/año |

Capacidad estimada con esta arquitectura en ese VPS: ~100 req/s sostenidas
(20–50k usuarios activos/día) con p95 < 100 ms; el cuello de botella será
PostgreSQL en escrituras de partidas, mitigado por transacciones únicas por submit.

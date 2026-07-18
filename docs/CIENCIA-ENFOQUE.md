# La ciencia detrás de la sección "Enfoque"

> Este documento respalda cada técnica y audio de la sección Enfoque de
> BrainTrain. Regla de la casa: **sin humo** — cada afirmación lleva su nivel de
> evidencia y su cita. Lo que no está probado se dice tal cual ("evidencia
> mixta", "pruébalo y decide").

## Resumen por técnica

| Técnica en la app | Nivel de evidencia | Fuente clave |
|---|---|---|
| Bloque de foco sin interrupciones (single-tasking) | **FUERTE** (coste de interrupción ~23 min de recuperación) | Mark et al., CHI 2008 |
| Condiciones de flow: meta clara + reto≈habilidad + feedback | **MODERADA** (replicada en ESM; sin "firma cerebral" única) | Csikszentmihalyi 1990; Alameda et al., Communications Psychology 2024 |
| Suspiro fisiológico / cyclic sighing 5 min | **MODERADA** (RCT: mejor ánimo y menor frecuencia respiratoria; sin cambios en HRV) | Balban et al., Cell Reports Medicine 2023 |
| Respiración lenta (~5-6 rpm, exhalación larga) | **MODERADA** (aumenta HRV vagal) | Laborde et al., Neurosci. Biobehav. Rev. 2022 |
| Meditación de atención focalizada 10 min | **MODERADA** (mejora atención, incluso con prácticas breves) | Zeidan et al., Consciousness & Cognition 2010 |
| NSDR / Yoga Nidra 10-20 min | **MODERADA** para estrés/bienestar (efectos pequeños) | Moszeik et al., Current Psychology 2022 |
| Sonidos de naturaleza (lluvia) | **MODERADA** (reduce estrés; mejora cognitiva leve) | Buxton et al., PNAS 2021 |
| Ruido blanco/rosa | **MODERADA en TDAH** (efecto pequeño), **DÉBIL/NEGATIVA en población general** | Metaanálisis JAACAP 2024 |
| Ruido marrón | **SIN ESTUDIOS PROPIOS** (popular; útil como enmascarador) | — |
| Binaural beats (40 Hz / 6 Hz) | **MIXTA** (metaanálisis en desacuerdo; entrainment no demostrado) | Garcia-Argibay et al. 2019 vs. Ingendoh et al., PLoS ONE 2023 |
| Pomodoro / bloques de 90 min | Convención **útil**, no ley biológica | Kleitman (BRAC, sueño); Ericsson 1993 |

## Detalle y matices honestos

1. **Interrupciones**: tras una interrupción se tarda en promedio ~23 min en
   volver a la tarea (peor caso naturalista; tareas simples ~8 min). Es el
   argumento más sólido de toda la sección: por eso el "modo foco" pide
   silenciar notificaciones y una sola meta por bloque.
2. **Cyclic sighing** (doble inhalación nasal + exhalación larga bucal): en el
   RCT de Stanford (108 personas, 5 min/día, 28 días) superó a mindfulness en
   mejora de ánimo y redujo la frecuencia respiratoria. **No** mejoró HRV — la
   app no promete eso.
3. **Ruido de fondo**: en TDAH el blanco/rosa ayuda un poco (g≈0.25); en
   población general el metaanálisis da efecto *negativo* (g≈−0.21). El ruido
   marrón viral **no tiene estudios controlados**. Por eso la app lo ofrece como
   "enmascarador de distracciones: pruébalo y decide", nunca como potenciador
   universal.
4. **Binaurales**: un metaanálisis (2019) encuentra efectos conductuales
   moderados; una revisión de 2023 encuentra que solo ~36% de los estudios
   apoyan el "entrainment" cerebral. La app los etiqueta "evidencia mixta,
   requiere audífonos" — jamás "sincroniza tus ondas".
5. **NSDR**: RCT (n≈770) con menos estrés y mejor sueño, efectos pequeños. El
   famoso "+65% de dopamina" viene de un PET con **8 personas sin réplica**
   (Kjaer 2002): la app no usa ese argumento.
6. **90 minutos**: el ciclo ultradiano BRAC está probado en el sueño, no como
   reloj exacto de productividad. Por eso las duraciones son configurables
   (15/25/50/90) y se presentan como estructura, no como magia.

## Afirmaciones prohibidas en la app (checklist de marketing)

- ❌ "Desbloquea el 100% de tu cerebro" / "sincroniza tus ondas cerebrales"
- ❌ "El 40 Hz garantiza concentración máxima"
- ❌ "NSDR sube tu dopamina 65%"
- ❌ "El ruido marrón mejora la concentración" (no hay estudios)
- ❌ "Cyclic sighing mejora tu HRV"
- ❌ "90 minutos es tu reloj biológico exacto"
- ✅ Permitido: "según un ensayo controlado…", "a algunas personas les ayuda,
  pruébalo", "la técnica con mejor evidencia para calmarte rápido".

## Los audios de la app

Generados proceduralmente (`tools/generate_focus_audio.py`, cero copyright),
loops perfectos de 11-12 s a 22.05 kHz: ruido marrón (1/f²), ruido rosa (1/f),
lluvia sintética (cama rosa + gotas aleatorias), binaural 200/240 Hz (beat
40 Hz) y 200/206 Hz (beat 6 Hz) en estéreo real, y campana de fin de sesión.

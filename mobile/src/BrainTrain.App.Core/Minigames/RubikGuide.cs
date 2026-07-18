namespace BrainTrain.App.Core.Minigames;

public sealed record RubikAlgorithm(string When, string Moves);

/// <summary>
/// Un paso de la guía. FacePattern: 9 caracteres (fila por fila) de la cara
/// relevante al terminar el paso — W blanco, Y amarillo, R rojo, G verde,
/// B azul, O naranja, x = cualquier color.
/// </summary>
public sealed record RubikStep(
    string Emoji, string Title, string Goal,
    IReadOnlyList<RubikAlgorithm> Algorithms,
    string Tip, string? FacePattern, string FaceLabel);

/// <summary>
/// Método principiante por capas (LBL), el estándar para aprender: margarita →
/// cruz blanca → esquinas → segunda capa → cruz amarilla → última capa.
/// </summary>
public static class RubikGuide
{
    public static readonly IReadOnlyList<RubikStep> Steps =
    [
        new("🧭", "Conoce tu cubo y la notación",
            "Los centros no se mueven: definen el color de cada cara. Aprende las letras de los giros — todo lo demás es combinarlas.",
            [
                new("U / D", "cara de arriba (Up) / abajo (Down), giro horario"),
                new("L / R", "cara izquierda (Left) / derecha (Right)"),
                new("F / B", "cara del frente (Front) / de atrás (Back)"),
                new("' (prima)", "el mismo giro pero antihorario (ej: R')"),
                new("2", "giro doble, 180° (ej: U2)")
            ],
            "Sostén siempre el cubo igual: blanco abajo cuando armes, salvo en el paso de la margarita.",
            null, ""),

        new("🌼", "Paso 1 — La margarita",
            "Pon el centro AMARILLO arriba y coloca las 4 aristas BLANCAS a su alrededor, como pétalos.",
            [ new("Libre", "sin fórmulas: mueve cada arista blanca hacia arriba sin tirar las que ya pusiste") ],
            "Es el único paso 'a ojo'. Tómalo con calma: entrena tu visión del cubo.",
            "xWxWYWxWx", "Cara superior (centro amarillo)"),

        new("✚", "Paso 2 — La cruz blanca",
            "Baja cada pétalo: alinea el color lateral de la arista con su centro y gira esa cara 180°.",
            [ new("Por cada pétalo", "gira U hasta alinear el color lateral con su centro → F2") ],
            "Comprueba que cada arista blanca coincida también con el color del centro lateral: cruz 'buena', no solo blanca.",
            "xWxWWWxWx", "Cara inferior (blanca) al terminar"),

        new("⬜", "Paso 3 — Esquinas blancas (primera capa completa)",
            "Coloca cada esquina blanca entre los dos centros de sus colores, con el 'martillito'.",
            [ new("Esquina abajo-derecha", "R' D' R D — repite hasta que encaje (máx. 6 veces)") ],
            "Pon la esquina debajo del hueco donde va y repite el algoritmo sin miedo: parece que desarmas, pero siempre se arregla.",
            "WWWWWWWWW", "Cara blanca completa"),

        new("🟦", "Paso 4 — Segunda capa",
            "Las 4 aristas del medio (las que no tienen amarillo). Alinea la arista con su centro y mándala a izquierda o derecha.",
            [
                new("Va a la derecha", "U R U' R'  U' F' U F"),
                new("Va a la izquierda", "U' L' U L  U F U' F'")
            ],
            "¿Una arista quedó mal puesta? Métele cualquier arista amarilla encima con el mismo algoritmo y sácala de nuevo.",
            null, ""),

        new("➕", "Paso 5 — La cruz amarilla",
            "Arriba puede haber: un punto, una L o una línea. El mismo algoritmo avanza de una figura a la siguiente.",
            [ new("Punto → L → línea → cruz", "F R U R' U' F'  (la L apunta atrás-izquierda; la línea, horizontal)") ],
            "No importa que las aristas no coincidan con los lados todavía: eso es el paso 6.",
            "xYxYYYxYx", "Cara superior amarilla"),

        new("🔄", "Paso 6 — Alinear la cruz",
            "Gira U hasta que 2 aristas coincidan con sus centros. Si son opuestas, haz el algoritmo y vuelve a mirar.",
            [ new("Dos aristas correctas atrás y derecha", "R U R' U R U2 R'") ],
            "Busca dejar las dos aristas correctas formando una esquina (atrás + derecha) antes de ejecutar.",
            null, ""),

        new("📍", "Paso 7 — Posicionar las esquinas amarillas",
            "Cada esquina debe estar entre sus 3 colores (aunque girada). Encuentra una correcta y ponla adelante-derecha.",
            [ new("Con una esquina buena en URF", "U R U' L'  U R' U' L — repite hasta que las 4 estén en su sitio") ],
            "¿Ninguna esquina está bien? Ejecuta el algoritmo una vez y aparecerá una.",
            null, ""),

        new("🏁", "Paso 8 — Orientar las esquinas (¡final!)",
            "Con el cubo igual en tus manos: martillito a la esquina de adelante-derecha hasta que quede amarilla; luego SOLO gira U para traer la siguiente.",
            [ new("Esquina adelante-derecha", "R' D' R D — repite hasta orientarla; luego U y siguiente") ],
            "A mitad del paso el cubo parece un desastre: es normal. No sueltes el agarre y confía en el proceso.",
            "YYYYYYYYY", "¡Cubo armado! 🎉"),
    ];
}

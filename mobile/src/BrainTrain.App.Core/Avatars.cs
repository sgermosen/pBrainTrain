namespace BrainTrain.App.Core;

/// <summary>Avatares locales: sin fotos ni descargas — seguro para niños.</summary>
public static class Avatars
{
    public static readonly IReadOnlyList<string> Codes =
        ["fox", "owl", "cat", "panda", "robot", "alien", "lion", "penguin", "koala", "dragon"];

    public static string Emoji(string code) => code switch
    {
        "fox" => "🦊",
        "owl" => "🦉",
        "cat" => "🐱",
        "panda" => "🐼",
        "robot" => "🤖",
        "alien" => "👽",
        "lion" => "🦁",
        "penguin" => "🐧",
        "koala" => "🐨",
        "dragon" => "🐲",
        "monkey" => "🐵",
        "unicorn" => "🦄",
        "tiger" => "🐯",
        "wolf" => "🐺",
        "octopus" => "🐙",
        "butterfly" => "🦋",
        _ => "🙂"
    };
}

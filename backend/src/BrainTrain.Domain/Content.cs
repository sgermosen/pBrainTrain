namespace BrainTrain.Domain;

public class Category
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;

    /// <summary>Color de marca en hex (#RRGGBB) que la app usa para tematizar la categoría.</summary>
    public string Color { get; set; } = "#7C4DFF";
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<Question> Questions { get; set; } = [];
}

public class Question
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public QuestionType Type { get; set; }

    /// <summary>Dificultad 1..5.</summary>
    public int Difficulty { get; set; } = 1;

    public string Text { get; set; } = string.Empty;

    /// <summary>La lógica detrás de la respuesta — pilar pedagógico del juego.</summary>
    public string Explanation { get; set; } = string.Empty;
    public string? FunFact { get; set; }

    /// <summary>Ruta relativa de la imagen (servida desde wwwroot), ej. "img/q/p1.png".</summary>
    public string? ImagePath { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Choice> Choices { get; set; } = [];
}

public class Choice
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}

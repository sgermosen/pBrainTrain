using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BrainTrain.Api.Services;

public sealed record CatalogChoice(int Id, string Text, bool IsCorrect);
public sealed record CatalogQuestion(
    int Id, int CategoryId, QuestionType Type, int Difficulty,
    string Text, string Explanation, string? FunFact, string? ImagePath,
    IReadOnlyList<CatalogChoice> Choices)
{
    public int CorrectChoiceId => Choices.First(c => c.IsCorrect).Id;
    public QuestionDto ToDto() => new(Id, CategoryId, Type, Difficulty, Text, ImagePath,
        Choices.Select(c => new ChoiceDto(c.Id, c.Text)).ToList());
}

public sealed record CatalogSnapshot(
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyDictionary<int, CatalogQuestion> QuestionsById,
    IReadOnlyDictionary<int, int[]> QuestionIdsByCategory,
    int[] AllQuestionIds);

/// <summary>
/// Catálogo completo de preguntas en memoria, refrescado periódicamente.
/// El contenido del juego es pequeño y de solo lectura: servirlo desde RAM
/// elimina el 90%+ de las consultas a la base de datos en el camino caliente.
/// </summary>
public sealed class QuestionCatalog(
    IServiceScopeFactory scopeFactory, IMemoryCache cache, IOptions<GameOptions> options)
{
    private const string Key = "question-catalog";
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async ValueTask<CatalogSnapshot> GetAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue<CatalogSnapshot>(Key, out var snap) && snap is not null)
            return snap;

        await Gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue<CatalogSnapshot>(Key, out snap) && snap is not null)
                return snap;

            snap = await LoadAsync(ct);
            cache.Set(Key, snap, TimeSpan.FromMinutes(options.Value.CatalogCacheMinutes));
            return snap;
        }
        finally
        {
            Gate.Release();
        }
    }

    public void Invalidate() => cache.Remove(Key);

    private async Task<CatalogSnapshot> LoadAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto(c.Id, c.Slug, c.Name, c.Emoji, c.Color, c.Description))
            .ToListAsync(ct);

        var questions = await db.Questions.AsNoTracking()
            .Where(q => q.IsActive)
            .Include(q => q.Choices)
            .ToListAsync(ct);

        var byId = questions.ToDictionary(
            q => q.Id,
            q => new CatalogQuestion(q.Id, q.CategoryId, q.Type, q.Difficulty, q.Text,
                q.Explanation, q.FunFact, q.ImagePath,
                q.Choices.OrderBy(c => c.SortOrder).Select(c => new CatalogChoice(c.Id, c.Text, c.IsCorrect)).ToList()));

        var byCategory = questions.GroupBy(q => q.CategoryId)
            .ToDictionary(g => g.Key, g => g.Select(q => q.Id).ToArray());

        return new CatalogSnapshot(categories, byId, byCategory, byId.Keys.ToArray());
    }
}

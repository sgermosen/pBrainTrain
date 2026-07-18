using BrainTrain.Api.Services;
using BrainTrain.Domain;
using BrainTrain.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BrainTrain.Api.Endpoints;

/// <summary>
/// Endpoints de administración (panel en /admin). Protegidos por clave estática
/// en el header X-Admin-Key (variable de entorno Admin__Key). Si no hay clave
/// configurada, el área queda deshabilitada por completo.
/// </summary>
public static class AdminEndpoints
{
    public static void MapAdmin(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin");
        g.AddEndpointFilter(async (ctx, next) =>
        {
            var config = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var key = config["Admin:Key"];
            if (string.IsNullOrWhiteSpace(key) || key.Length < 16)
                return Results.Problem(statusCode: 503, title: "admin_disabled",
                    detail: "Configura Admin__Key (16+ caracteres) para habilitar la administración.");
            if (!ctx.HttpContext.Request.Headers.TryGetValue("X-Admin-Key", out var given) || given != key)
                return Results.Problem(statusCode: 401, title: "bad_admin_key");
            return await next(ctx);
        });

        g.MapGet("/stats", async (AppDbContext db, CancellationToken ct) =>
        {
            var todayStart = DateTime.UtcNow.Date;
            return Results.Ok(new AdminStatsDto(
                TotalUsers: await db.Users.CountAsync(ct),
                NewUsersToday: await db.Users.CountAsync(u => u.CreatedAtUtc >= todayStart, ct),
                DauToday: await db.GameSessions.Where(s => s.StartedAtUtc >= todayStart)
                    .Select(s => s.UserId).Distinct().CountAsync(ct),
                SessionsToday: await db.GameSessions.CountAsync(s => s.StartedAtUtc >= todayStart, ct),
                PremiumActive: await db.Users.CountAsync(u => u.PremiumUntilUtc > DateTime.UtcNow, ct),
                ReceiptsTotal: await db.PurchaseReceipts.CountAsync(ct),
                QuestionsActive: await db.Questions.CountAsync(q => q.IsActive, ct)));
        });

        g.MapGet("/questions", async (AppDbContext db, int page, string? search, CancellationToken ct) =>
        {
            var query = db.Questions.AsNoTracking().Include(q => q.Choices).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(q => q.Text.Contains(search));
            var items = await query.OrderByDescending(q => q.Id)
                .Skip(Math.Max(0, page) * 20).Take(20)
                .ToListAsync(ct);
            return Results.Ok(items.Select(ToDto).ToList());
        });

        g.MapPost("/questions", async (AdminQuestionRequest req, AppDbContext db, QuestionCatalog catalog,
            CancellationToken ct) =>
        {
            var error = Validate(req);
            if (error is not null) return error;

            var question = new Question
            {
                CategoryId = req.CategoryId,
                Type = req.Type,
                Difficulty = Math.Clamp(req.Difficulty, 1, 5),
                Text = req.Text.Trim(),
                Explanation = req.Explanation.Trim(),
                FunFact = string.IsNullOrWhiteSpace(req.FunFact) ? null : req.FunFact.Trim(),
                IsActive = true
            };
            var order = 0;
            foreach (var c in req.Choices)
                question.Choices.Add(new Choice { Text = c.Text.Trim(), IsCorrect = c.IsCorrect, SortOrder = order++ });
            db.Questions.Add(question);
            await db.SaveChangesAsync(ct);
            catalog.Invalidate();
            return Results.Ok(ToDto(question));
        });

        g.MapPut("/questions/{id:int}", async (int id, AdminQuestionRequest req, AppDbContext db,
            QuestionCatalog catalog, CancellationToken ct) =>
        {
            var error = Validate(req);
            if (error is not null) return error;

            var question = await db.Questions.Include(q => q.Choices).FirstOrDefaultAsync(q => q.Id == id, ct);
            if (question is null) return Results.Problem(statusCode: 404, title: "question_not_found");

            question.CategoryId = req.CategoryId;
            question.Type = req.Type;
            question.Difficulty = Math.Clamp(req.Difficulty, 1, 5);
            question.Text = req.Text.Trim();
            question.Explanation = req.Explanation.Trim();
            question.FunFact = string.IsNullOrWhiteSpace(req.FunFact) ? null : req.FunFact.Trim();
            db.Choices.RemoveRange(question.Choices);
            question.Choices.Clear();
            var order = 0;
            foreach (var c in req.Choices)
                question.Choices.Add(new Choice { Text = c.Text.Trim(), IsCorrect = c.IsCorrect, SortOrder = order++ });
            await db.SaveChangesAsync(ct);
            catalog.Invalidate();
            return Results.Ok(ToDto(question));
        });

        g.MapDelete("/questions/{id:int}", async (int id, AppDbContext db, QuestionCatalog catalog, CancellationToken ct) =>
        {
            var question = await db.Questions.FirstOrDefaultAsync(q => q.Id == id, ct);
            if (question is null) return Results.Problem(statusCode: 404, title: "question_not_found");
            question.IsActive = false; // borrado suave: las partidas históricas no se rompen
            await db.SaveChangesAsync(ct);
            catalog.Invalidate();
            return Results.NoContent();
        });
    }

    private static IResult? Validate(AdminQuestionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Text) || string.IsNullOrWhiteSpace(req.Explanation))
            return Results.Problem(statusCode: 400, title: "bad_question", detail: "Texto y explicación son obligatorios.");
        if (req.Choices is not { Count: >= 2 and <= 4 } || req.Choices.Count(c => c.IsCorrect) != 1)
            return Results.Problem(statusCode: 400, title: "bad_choices",
                detail: "Entre 2 y 4 opciones con exactamente una correcta.");
        return null;
    }

    private static AdminQuestionDto ToDto(Question q) => new(
        q.Id, q.CategoryId, q.Type, q.Difficulty, q.Text, q.Explanation, q.FunFact, q.IsActive,
        q.Choices.OrderBy(c => c.SortOrder).Select(c => new AdminChoiceDto(c.Text, c.IsCorrect)).ToList());
}

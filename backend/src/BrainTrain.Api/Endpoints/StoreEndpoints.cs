using BrainTrain.Api.Services;

namespace BrainTrain.Api.Endpoints;

public static class StoreEndpoints
{
    public static void MapStore(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/store/catalog", (StoreService store) => Results.Ok(store.Catalog()))
            .CacheOutput("content");

        var g = app.MapGroup("/api/v1/store").RequireAuthorization();

        g.MapPost("/purchase", async (PurchaseRequest req, HttpContext http, StoreService store, CancellationToken ct) =>
            Results.Ok(await store.PurchaseAsync(http.User.UserId(), req, ct)));

        g.MapPost("/refill-with-coins", async (HttpContext http, StoreService store, CancellationToken ct) =>
            Results.Ok(await store.RefillWithCoinsAsync(http.User.UserId(), ct)));
    }
}

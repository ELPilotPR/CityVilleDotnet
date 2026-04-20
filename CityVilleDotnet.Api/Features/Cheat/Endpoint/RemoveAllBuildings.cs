using CityVilleDotnet.Domain.Entities;
using CityVilleDotnet.Persistence;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CityVilleDotnet.Api.Features.Cheat.Endpoint;

internal sealed class RemoveAllBuildings(UserManager<ApplicationUser> userManager, CityVilleDbContext dbContext, IConfiguration configuration) : Endpoint<RemoveAllBuildingsRequest>
{
    public override void Configure()
    {
        Post("/api/cheat/remove-buildings");
    }

    public override async Task HandleAsync(RemoveAllBuildingsRequest req, CancellationToken ct)
    {
        var enableCheat = configuration.GetValue<bool>("enableCheat");

        if (!enableCheat)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var currentUser = await userManager.GetUserAsync(HttpContext.User);

        if (currentUser is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await dbContext.Set<Player>()
            .AsSplitQuery()
            .Include(x => x.World)
            .ThenInclude(x => x!.Objects)
            .FirstOrDefaultAsync(x => x.AppUser!.Id == currentUser.Id, ct);

        if (user?.World is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var objects = user.World.Objects.ToList();
        int removed;

        if (!string.IsNullOrEmpty(req.ItemName))
        {
            var toRemove = objects.Where(o => o.ItemName == req.ItemName).ToList();
            removed = toRemove.Count;
            dbContext.Set<WorldObject>().RemoveRange(toRemove);
        }
        else
        {
            removed = objects.Count;
            dbContext.Set<WorldObject>().RemoveRange(objects);
        }

        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(cancellation: ct);
    }
}

internal sealed class RemoveAllBuildingsRequest
{
    public string? ItemName { get; set; }
}

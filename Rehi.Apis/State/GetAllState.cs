using MediatR;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.State;

public class GetAllState : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/states", async (ISender sender) =>
            {
                var result = await sender.Send(new Application.States.GetAllState.Query());
                return result.MatchOk();
            })
            .WithTags("States")
            .RequireAuthorization()
            .WithName("GetAllStates");
    }
}
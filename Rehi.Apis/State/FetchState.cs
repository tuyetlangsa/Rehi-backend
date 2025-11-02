using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.State;

public class FetchState : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/states/{lastUpdateTime}", async ([FromRoute] long lastUpdateTime, ISender sender) =>
            {
                var result = await sender.Send(new Application.States.FetchState.Query(lastUpdateTime));
                return result.MatchOk();
            })
            .WithTags("States")
            .RequireAuthorization()
            .WithName("FetchState");
    }
}
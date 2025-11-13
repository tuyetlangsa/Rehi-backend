using MediatR;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Dashboard;

public class GetDashboard : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("admin/get-dash-board", async (ISender sender) =>
            {
                var result = await sender.Send(new Application.Dashboard.GetDashboard.GetDashboard.Command());
                return result.MatchOk();
            }).WithTags("Dashboard")
            .RequireAuthorization()
            .WithName("GetDashboard");
    }
}
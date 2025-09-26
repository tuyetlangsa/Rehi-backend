namespace Rehi.Infrastructure.Authentication;

public class Auth0Options
{
    public string Domain { get; set; } = null!;
    public string Audience { get; set; } = null!;
}
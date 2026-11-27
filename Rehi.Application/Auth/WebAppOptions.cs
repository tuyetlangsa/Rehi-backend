namespace Rehi.Application.Auth;

public sealed class WebAppOptions
{
    public const string Section = "Rehi";

    public string WebBaseUrl { get; init; } = "";
}

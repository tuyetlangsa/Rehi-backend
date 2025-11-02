namespace Rehi.Domain.Common;

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static readonly Error NullValue = new(
        "General.Null",
        "Null value was provided",
        ErrorType.Failure);

    public Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    public string Code { get; }

    public string Description { get; }

    public ErrorType Type { get; }

    public static Error Failure(string code, string description)
    {
        return new Error(code, description, ErrorType.Failure);
        //internal error : 500
    }

    public static Error NotFound(string code, string description)
    {
        return new Error(code, description, ErrorType.NotFound);
        //not found by id : 404
    }

    public static Error Conflict(string code, string description)
    {
        return new Error(code, description, ErrorType.Conflict);
        // conflict business rules: 409
    }

    public static Error BadRequest(string code, string description)
    {
        return new Error(code, description, ErrorType.Validation);
        // bad request
    }
}
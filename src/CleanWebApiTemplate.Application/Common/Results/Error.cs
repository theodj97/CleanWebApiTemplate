namespace CleanWebApiTemplate.Application.Common.Results;

public abstract record Error(string? Title = null, string? Description = null)
{
    public string? Title { get; } = Title;
    public string? Description { get; } = Description;
}

public sealed record BadRequestError : Error
{
    public BadRequestError(string title, string description) : base(title, description) { }

    public BadRequestError(string description) : base("Bad Request", description) { }
}

public sealed record DomainError : Error
{
    public DomainError(string title, string description) : base(title, description) { }

    public DomainError(string description) : base("Domain Error", description) { }
}

public sealed record NotFoundError : Error
{
    public NotFoundError(string title, string description) : base(title, description) { }

    public NotFoundError(string description) : base("Not Found", description) { }
}

public sealed record ConflictError : Error
{
    public ConflictError(string title, string description) : base(title, description) { }

    public ConflictError(string description) : base("Conflict", description) { }
}

public sealed record UnauthorizedError : Error
{
    public UnauthorizedError() : base() { }
}

public sealed record ForbiddenError : Error
{
    public ForbiddenError() : base() { }
}

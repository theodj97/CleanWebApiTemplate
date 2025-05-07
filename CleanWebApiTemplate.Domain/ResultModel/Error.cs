namespace CleanWebApiTemplate.Domain.ResultModel;

public abstract record Error(string? Title = null, string? Description = null)
{
    public string? Title { get; } = Title;
    public string? Description { get; } = Description;
}

public record BadRequestError : Error
{
    public BadRequestError(string title, string description) : base(title, description) { }

    public BadRequestError(string description) : base("Bad Request", description) { }
}

public record DomainError : Error
{
    public DomainError(string title, string description) : base(title, description) { }

    public DomainError(string description) : base("Domain Error", description) { }
}

public record NotFoundError : Error
{
    public NotFoundError(string title, string description) : base(title, description) { }

    public NotFoundError(string description) : base("Not Found", description) { }
}

public record ConflictError : Error
{
    public ConflictError(string title, string description) : base(title, description) { }

    public ConflictError(string description) : base("Conflict", description) { }
}

public record UnauthorizedError : Error
{
    public UnauthorizedError() : base() { }
}

public record ForbiddenError : Error
{
    public ForbiddenError() : base() { }
}

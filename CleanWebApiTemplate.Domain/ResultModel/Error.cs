namespace CleanWebApiTemplate.Domain.ResultModel;

public abstract class Error(string? title = null, string? description = null)
{
    public string? Title { get; } = title;
    public string? Description { get; } = description;
}

public class BadRequestError : Error
{
    public BadRequestError(string title, string description) : base(title, description) { }

    public BadRequestError(string description) : base("Bad Request", description) { }
}

public class DomainError : Error
{
    public DomainError(string title, string description) : base(title, description) { }

    public DomainError(string description) : base("Domain Error", description) { }
}

public class NotFoundError : Error
{
    public NotFoundError(string title, string description) : base(title, description) { }

    public NotFoundError(string description) : base("Not Found", description) { }
}

public class ConflictError : Error
{
    public ConflictError(string title, string description) : base(title, description) { }

    public ConflictError(string description) : base("Conflict", description) { }
}

public class UnauthorizedError : Error
{
    public UnauthorizedError() : base() { }
}

public class ForbiddenError : Error
{
    public ForbiddenError() : base() { }
}

namespace CleanWebApiTemplate.Domain.Todo;

/// <summary>The stage a Todo is in. A new Todo starts as <see cref="Pending"/>.</summary>
public enum ETodoStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}

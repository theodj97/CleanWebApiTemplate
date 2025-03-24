namespace CleanWebApiTemplate.Host.Routes.Todo.Filter;

public struct FilteredTodoRequest
{
    public string[]? Ids { get; set; }
    public string[]? Title { get; set; }
    public int[]? Status { get; set; }
    public string[]? CreatedBy { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

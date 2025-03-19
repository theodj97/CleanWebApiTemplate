namespace CleanWebApiTemplate.Host.Routes.Todo.Filter;

public struct FilteredTodoRequest
{
    public string[]? Ids { get; set; }
    public string[]? Title { get; set; }
    public byte[]? Status { get; set; }
    public string[]? CreatedBy { get; set; }
}

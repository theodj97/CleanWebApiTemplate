namespace CleanWebApiTemplate.Domain.Project;

/// <summary>The stage a Project is in. A new Project starts as <see cref="Draft"/>.</summary>
public enum EProjectStatus
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Archived = 4,
}

namespace DBCONNECTOR.Dtos.Common;

public class RelationshipDto
{
    public string? Direction { get; set; }
    public string? ForeignKey { get; set; }
    public string? ChildSchema { get; set; }
    public string? ChildTable { get; set; }
    public string? ChildColumns { get; set; }
    public string? ParentSchema { get; set; }
    public string? ParentTable { get; set; }
    public string? ParentColumns { get; set; }
    public string? OnDelete { get; set; }
    public string? OnUpdate { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsNotTrusted { get; set; }
    public bool ChildColumnsNullableAny { get; set; }
    public bool ChildColumnsHaveUniqueIndex { get; set; }
    public string? ChildCoveringIndexName { get; set; }
}

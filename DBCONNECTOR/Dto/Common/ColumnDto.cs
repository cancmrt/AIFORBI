namespace DBCONNECTOR.Dtos.Common;

public class ColumnDto
{
    public string? Catalog { get; set; }
    public string? ObjectType { get; set; }
    public string? Schema { get; set; }
    public string? Table { get; set; }
    public string? Column { get; set; }
    public int OrdinalPosition { get; set; }
    public string? DataType { get; set; }
    public string? FullDataType { get; set; }
    public int MaxLength { get; set; }
    public byte Precision { get; set; }
    public byte Scale { get; set; }
    public bool IsNullable { get; set; }
    public string? DefaultDefinition { get; set; }
    public bool IsIdentity { get; set; }
    public string? IdentitySeed { get; set; }
    public string? IdentityIncrement { get; set; }
    public bool IsComputed { get; set; }
    public string? ComputedDefinition { get; set; }
    public string? CollationName { get; set; }
    public bool IsRowGuidCol { get; set; }
    public bool IsSparse { get; set; }
    public bool IsHidden { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
    public bool IsForeignKey { get; set; }
    public string? ForeignKeyName { get; set; }
    public string? ForeignKeyRef { get; set; }
    public string? Description { get; set; }
}

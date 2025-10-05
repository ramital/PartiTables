namespace PartiTables;

/// <summary>
/// Declares the row key pattern for an entity type.
/// This enables automatic row key generation and pattern matching.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RowKeyPatternAttribute : Attribute
{
    /// <summary>
    /// The row key pattern template.
    /// Use placeholders: {ParentProperty}, {Property}, or literal strings.
    /// Example: "{CustomerId}-order-{OrderId}"
    /// </summary>
    public string Pattern { get; }
    
    /// <summary>
    /// The keyword that uniquely identifies this entity type.
    /// Extracted from the pattern if not explicitly provided.
    /// Example: "order", "address", "profile"
    /// </summary>
    public string? TypeKeyword { get; set; }

    public RowKeyPatternAttribute(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));
            
        Pattern = pattern;
    }
}

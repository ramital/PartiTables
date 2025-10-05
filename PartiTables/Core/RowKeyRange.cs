namespace PartiTables;

/// <summary>
/// Utility for creating row key ranges for prefix queries.
/// </summary>
public static class RowKeyRange
{
    /// <summary>
    /// Returns the inclusive-from / exclusive-to range for a RowKey prefix.
    /// This is used for efficient prefix queries in Azure Table Storage.
    /// </summary>
    /// <param name="prefix">The row key prefix to search for.</param>
    /// <returns>A tuple containing the inclusive start and exclusive end values for the range query.</returns>
    /// <example>
    /// <code>
    /// var (from, to) = RowKeyRange.ForPrefix("patient-123-");
    /// // Query: RowKey >= "patient-123-" AND RowKey < "patient-123-\uFFFF"
    /// </code>
    /// </example>
    public static (string From, string To) ForPrefix(string prefix)
    {
        prefix ??= string.Empty;
        return (prefix, prefix + "\uFFFF");
    }

    /// <summary>
    /// Returns a range for a specific row key (exact match).
    /// </summary>
    /// <param name="rowKey">The exact row key to match.</param>
    /// <returns>A tuple with the same value for both from and to.</returns>
    public static (string From, string To) Exact(string rowKey)
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            throw new ArgumentException("Row key cannot be null or empty.", nameof(rowKey));
        
        return (rowKey, rowKey);
    }

    /// <summary>
    /// Returns a custom range between two row keys.
    /// </summary>
    /// <param name="from">The inclusive start row key.</param>
    /// <param name="to">The exclusive end row key.</param>
    /// <returns>A tuple containing the range.</returns>
    public static (string From, string To) Between(string from, string to)
    {
        if (string.IsNullOrWhiteSpace(from))
            throw new ArgumentException("From row key cannot be null or empty.", nameof(from));
        
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("To row key cannot be null or empty.", nameof(to));

        return (from, to);
    }
}
namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies how to read <c>semantic_feature.feature_value</c>.
    /// </summary>
    /// <remarks>
    /// The column itself stays free text because the set of features is open — a fixed enum would force a
    /// schema migration for every new one. This says how to parse whatever text is there without
    /// constraining which features exist.
    /// </remarks>
    public enum SemanticValueKind
    {
        /// <summary>A true/false feature (feature_value is "true" or "false").</summary>
        Binary,

        /// <summary>A position on a scale (feature_value is a number or an ordered label).</summary>
        Scalar,

        /// <summary>A label from an open set (feature_value is the label itself).</summary>
        Categorical
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Represents one page of one table, as the API sends it.
    /// </summary>
    /// <remarks>
    /// The wire format mirrors the tables rather than nesting lemmas inside frames inside slots. That is
    /// the less obvious choice for an API and the right one here, because the identifiers are assigned by
    /// the server and the local copy has to carry them over unchanged — a nested document would either
    /// have to repeat them anyway or force the importer to invent its own, and invented identifiers make
    /// the replica impossible to compare against the server it came from.
    /// <para>
    /// Rows are arrays rather than objects, with the column names stated once in <see cref="Columns"/>.
    /// At a hundred thousand lemmas repeating twenty-four keys per row is most of the payload, and the
    /// single header doubles as the contract check: the importer refuses a page whose columns are not the
    /// ones the schema expects, in that order.
    /// </para>
    /// </remarks>
    public sealed record LexiconPage
    {
        /// <summary>
        /// Gets the table these rows belong to.
        /// </summary>
        [JsonPropertyName("table")]
        public string Table { get; init; } = string.Empty;

        /// <summary>
        /// Gets the column names, in the order the values appear in each row.
        /// </summary>
        [JsonPropertyName("columns")]
        public IReadOnlyList<string> Columns { get; init; } = [];

        /// <summary>
        /// Gets the rows, each holding one value per column.
        /// </summary>
        /// <remarks>
        /// Typed as object so that a value can be a number, a string or null without a wrapper. On the way
        /// out those are the values read from the database; on the way in System.Text.Json hands back
        /// <see cref="JsonElement"/>, which is why <see cref="ToDatabaseValue"/> exists.
        /// </remarks>
        [JsonPropertyName("rows")]
        public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; } = [];

        /// <summary>
        /// Gets the key to pass as the after parameter to fetch the next page, or <see langword="null"/>
        /// when this page is the last one.
        /// </summary>
        /// <remarks>
        /// Keyset paging rather than an offset. An offset re-counts the skipped rows on every request and,
        /// worse, shifts when the dictionary is edited mid-pull, which silently drops or repeats a row.
        /// The key is carried as text so that one mechanism covers both the integer primary keys and the
        /// string key of lexicon_meta.
        /// </remarks>
        [JsonPropertyName("next_after")]
        public string? NextAfter { get; init; }

        /// <summary>
        /// The options both ends use, so the property names match without relying on a default policy.
        /// </summary>
        public static JsonSerializerOptions SerializerOptions { get; } = new()
        {
            WriteIndented = true,

            // Czech is most of the payload and escaping every diacritic to \uXXXX would inflate it while
            // making the file unreadable. The output is UTF-8 and declared as such.
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Converts a value as it arrives from JSON into something a database parameter accepts.
        /// </summary>
        /// <param name="value">The value read from the page.</param>
        /// <returns>A string, a long, a double, or <see cref="DBNull.Value"/> for a null.</returns>
        /// <exception cref="InvalidOperationException">The value is of a kind the lexicon never stores.</exception>
        public static object ToDatabaseValue(object? value) => value switch
        {
            null => DBNull.Value,

            // Serializing writes primitives directly; deserializing gives them back boxed as JsonElement.
            // Both arrive here, because the round-trip test feeds this the writer's own output.
            JsonElement element => element.ValueKind switch
            {
                JsonValueKind.Null => DBNull.Value,
                JsonValueKind.String => element.GetString()!,
                JsonValueKind.Number => element.TryGetInt64(out var number) ? number : element.GetDouble(),
                JsonValueKind.True => 1L,
                JsonValueKind.False => 0L,
                _ => throw new InvalidOperationException(
                    $"Hodnota typu {element.ValueKind} se v lexikonu nevyskytuje.")
            },

            _ => value
        };
    }
}

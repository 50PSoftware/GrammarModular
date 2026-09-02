namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Names the tables of the lexicon and the columns each one carries.
    /// </summary>
    /// <remarks>
    /// One list, used by everything that moves rows: the SQL dump, the JSON export and the import from
    /// the API. They were going to need the same list anyway, and three copies of it would have drifted
    /// in the direction that is hardest to notice — a column dropped from one path still produces a
    /// database that opens, with that column silently empty.
    /// <para>
    /// The columns are written out rather than discovered from the database, so that a column added to
    /// the schema and forgotten here fails loudly on the next round trip instead of being skipped.
    /// </para>
    /// </remarks>
    public static class LexiconSchema
    {
        /// <summary>
        /// The tables in dependency order — parents first, so inserts satisfy the foreign keys as they go.
        /// </summary>
        public static IReadOnlyList<LexiconTable> Tables { get; } =
        [
            new("lexicon_meta", ["meta_key", "meta_value"], KeyIsText: true),
            new("lexeme", ["lexeme_id", "primary_lemma", "note"]),
            new("lemma_entry", [
                "lemma_entry_id", "lemma", "lemma_key", "homonym_index", "category", "gender", "pattern",
                "is_animate", "has_mobile_e", "has_genitive_plural_shortening",
                "has_epenthesis_in_genitive_plural", "is_indeclinable", "is_plural_only", "is_countable",
                "prefers_short_form", "verb_class", "aspect", "aspect_counterpart", "aktionsart", "reflexive_type",
                "base_verb_lemma", "inherent_functor",
                "stem", "present_stem", "past_stem", "future_stem",
                "imperative_stem", "passive_stem", "infinitive", "forms_passive",
                "lexeme_id", "source", "is_verified", "note"]),
            new("lemma_variant", ["variant_id", "lemma_entry_id", "lemma", "lemma_key", "note"]),
            new("lexical_unit", ["lu_id", "lexeme_id", "sense_label", "gloss", "ssc_class_id"]),
            new("lemma_sense", ["lemma_sense_id", "lemma_entry_id", "lu_id", "aktionsart", "note"]),
            new("valency_frame", [
                "frame_id", "lu_id", "kind", "diathesis", "is_default", "reflexive_type"]),
            new("valency_slot", [
                "slot_id", "frame_id", "functor", "canonical_order", "obligatoriness",
                "can_drop_contextual", "can_drop_generic", "control_target"]),
            new("slot_realization", [
                "realization_id", "slot_id", "morph_case", "preposition", "clause_type",
                "takes_infinitive", "preference"]),
            new("construction", [
                "construction_id", "pattern_name", "light_verb_lemma", "pred_noun_lemma", "template_json"]),
            new("semantic_feature", [
                "feature_id", "lu_id", "feature_name", "feature_value", "value_kind", "source", "note",
                "is_verified"]),
            new("semantic_relation", [
                "relation_id", "lu_id_a", "lu_id_b", "relation_type", "antonym_subtype", "strength",
                "source", "note", "is_verified"])
        ];

        /// <summary>
        /// Gets the table of the supplied name.
        /// </summary>
        /// <param name="name">The table name to look up.</param>
        /// <returns>The table definition.</returns>
        /// <exception cref="InvalidOperationException">The lexicon has no such table.</exception>
        public static LexiconTable Get(string name)
            => Tables.FirstOrDefault(table => table.Name == name)
                ?? throw new InvalidOperationException(
                    $"Lexikon nemá tabulku '{name}'. Zná: "
                    + string.Join(", ", Tables.Select(table => table.Name)) + ".");
    }

    /// <summary>
    /// Represents one table of the lexicon.
    /// </summary>
    /// <param name="Name">The table name.</param>
    /// <param name="Columns">The columns, in the order rows are read and written.</param>
    /// <param name="KeyIsText">
    /// Whether the primary key is text rather than an integer, which decides how paging compares it.
    /// </param>
    public sealed record LexiconTable(string Name, IReadOnlyList<string> Columns, bool KeyIsText = false)
    {
        /// <summary>
        /// Gets the column paging orders by, which is the primary key and always the first column.
        /// </summary>
        public string KeyColumn => Columns[0];

        /// <summary>
        /// Converts a paging key from the wire back into the type the key column compares against.
        /// </summary>
        /// <param name="after">The key as it travelled, which is always text.</param>
        /// <returns>The value to bind.</returns>
        /// <remarks>
        /// The key crosses the wire as text so that one parameter covers both kinds, and is converted
        /// back here rather than cast in SQL. Casting the column instead — ORDER BY CAST(id AS TEXT) —
        /// does give a consistent comparison, but it also stops the primary key index from being usable
        /// and makes every page a full scan and a sort. Restoring the type keeps the index and keeps the
        /// two sides ordering the same way.
        /// </remarks>
        public object ToKeyValue(string after)
            => KeyIsText
                ? after
                : long.TryParse(after, out var number)
                    ? number
                    : throw new InvalidOperationException(
                        $"Tabulka '{Name}' má číselný klíč, ale stránkovací klíč je '{after}'.");
    }
}

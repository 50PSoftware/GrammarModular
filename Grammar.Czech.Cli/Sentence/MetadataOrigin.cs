namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Says where a word's grammatical metadata came from.
    /// </summary>
    /// <remarks>
    /// Shown in the review table and in the JSON output, because it is the difference between an answer
    /// and a guess. A pattern read from the dictionary is as good as the dictionary; a pattern inferred
    /// from the ending is the tool's proposal and the one worth a second look.
    /// </remarks>
    public enum MetadataOrigin
    {
        /// <summary>
        /// The lexicon holds the lemma and supplied its metadata.
        /// </summary>
        Lexicon,

        /// <summary>
        /// The word belongs to a closed class the rule data enumerates, which is where it was recognized.
        /// </summary>
        /// <remarks>
        /// Pronouns and prepositions are not in the dictionary and never will be — they are closed
        /// classes and live in the embedded JSON rules instead. Reporting them as unknown would be
        /// wrong twice over: nothing about them was guessed, and nothing is missing.
        /// </remarks>
        Rules,

        /// <summary>
        /// The lemma is not in the lexicon and the metadata was inferred from its ending.
        /// </summary>
        Guess,

        /// <summary>
        /// The user stated the metadata, on the command line or in the review.
        /// </summary>
        User,
    }
}

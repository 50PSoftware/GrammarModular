namespace Grammar.Core.Exceptions
{
    /// <summary>
    /// Represents an error raised when a lemma is missing or incomplete in the lexicon.
    /// </summary>
    public sealed class LemmaNotFoundException : Exception
    {
        /// <summary>
        /// Gets or sets the dictionary form of the word.
        /// </summary>
        public string Lemma { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LemmaNotFoundException"/> type.
        /// </summary>
        public LemmaNotFoundException(string lemma)
            : base($"Lemma '{lemma}' was not found in the lexicon. Add it in the admin, or in a "
                + "seed block, and pull the dictionary again.")
        {
            Lemma = lemma;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LemmaNotFoundException"/> type.
        /// </summary>
        public LemmaNotFoundException(string lemma, string message)
            : base(message)
        {
            Lemma = lemma;
        }
    }
}

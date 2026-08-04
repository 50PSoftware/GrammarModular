namespace Grammar.Czech.Helpers
{
    /// <summary>
    /// Provides helper operations for Czech morphology and ending application.
    /// </summary>
    public static class MorphologyHelper
    {
        /// <summary>
        /// Applies an inflection ending to a stem while respecting removable ending markers.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="ending">The ending used to choose the morphology rule.</param>
        /// <returns>The stem plus the normalized ending marker, or the supplied replacement form when the value is not an ending marker.</returns>
        public static string ApplyFormEnding(string stem, string ending)
        {
            if (IsEnding(ending))
            {
                return stem + ending.Replace("-", "");
            }
            else
            {
                return ending;
            }
        }

        /// <summary>
        /// Determines whether the supplied stem ends with two consonants.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <returns><see langword="true"/> when the stem ends with two consonants; otherwise, <see langword="false"/>.</returns>
        public static bool EndsWithTwoConsonants(string stem)
        {
            if (stem.Length < 2)
            {
                return false;
            }

            var last = stem[^1];
            var secondLast = stem[^2];
            return IsConsonant(secondLast) && IsConsonant(last);
        }

        /// <summary>
        /// Determines whether the lemma ends in a vowel-consonant-vowel-consonant sequence.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns><see langword="true"/> when the ending sequence matches; otherwise, <see langword="false"/>.</returns>
        public static bool EndsWithVowelConsonantVowelConsonant(string lemma)
        {
            if (lemma.Length < 4)
            {
                return false;
            }

            var vowel = lemma[^4];
            var consonant = lemma[^3];
            var lastVowel = lemma[^2];
            var lastConsonant = lemma[^1];
            return !IsConsonant(vowel) && IsConsonant(consonant) && !IsConsonant(lastVowel) && IsConsonant(lastConsonant);
        }

        /// <summary>
        /// Determines whether the lemma is likely to carry a mobile -e- that drops in the oblique cases.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns><see langword="true"/> when the lemma ends with a productive mobile-e suffix; otherwise, <see langword="false"/>.</returns>
        public static bool HasLikelyMobileE(string lemma)
        {
            // Only -ec, -ek and -ev are regular enough to decide by rule (otec → otc-, větev → větv-);
            // closed-class stems and the -ev nouns that keep the vowel belong in the lexicon, which wins
            // here. A looser test also caught doktor, sever and kmen. Ordinal, for the usual cs-CZ reason.
            if (lemma.Length < 3)
            {
                return false;
            }

            return lemma.EndsWith("ec", StringComparison.Ordinal)
                || lemma.EndsWith("ek", StringComparison.Ordinal)
                || lemma.EndsWith("ev", StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the supplied character is not one of the Czech vowels known to the helper.
        /// </summary>
        /// <param name="c">The character to classify.</param>
        /// <returns><see langword="true"/> when the character is treated as a consonant; otherwise, <see langword="false"/>.</returns>
        public static bool IsConsonant(char c)
        {
            return !"aáeéěiíyýoóuúů".Contains(char.ToLower(c));
        }

        /// <summary>
        /// Determines whether the supplied value is an ending marker.
        /// </summary>
        /// <param name="ending">The ending used to choose the morphology rule.</param>
        /// <returns><see langword="true"/> when the value contains an ending marker; otherwise, <see langword="false"/>.</returns>
        public static bool IsEnding(string ending) => ending.Contains("-");
    }
}

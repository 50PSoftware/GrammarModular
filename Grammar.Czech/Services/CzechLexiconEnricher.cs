using Grammar.Core.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Fills in from the lexicon whatever a request did not state.
    /// </summary>
    /// <remarks>
    /// Additive: it only writes where the request holds <see langword="null"/>, so anything the caller
    /// said stands and an unknown word passes through unchanged. That rests on a nullable flag having
    /// three states — <c>HasMobileE = false</c> is a claim, <see langword="null"/> is a gap, and only the
    /// second is filled. Hence nullable lexicon columns rather than defaulted ones.
    /// </remarks>
    public sealed class CzechLexiconEnricher
    {
        private readonly IValencyProvider<CzechLexicalEntry> _valencyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechLexiconEnricher"/> type.
        /// </summary>
        /// <param name="valencyProvider">The lexicon to read.</param>
        public CzechLexiconEnricher(IValencyProvider<CzechLexicalEntry> valencyProvider)
        {
            _valencyProvider = valencyProvider;
        }

        /// <summary>
        /// Returns the request with its gaps filled from the lexicon entry for its lemma.
        /// </summary>
        /// <param name="word">The request to complete.</param>
        /// <returns>The completed request, or the original when the lemma is not in the lexicon.</returns>
        public CzechWordRequest Enrich(CzechWordRequest word)
        {
            if (string.IsNullOrEmpty(word.Lemma))
            {
                return word;
            }

            // A stated word class picks the right homonym — stát the country and stát the verb share a
            // lemma and nothing else. Without one, whichever row comes first has to do.
            var entry = word.WordCategory is { } stated
                ? _valencyProvider.GetEntry(word.Lemma, stated)
                : _valencyProvider.GetEntry(word.Lemma);

            if (entry is null)
            {
                // Most of Czech is not in the dictionary and never will be, so a word it does not hold
                // is the ordinary case: the caller supplies the metadata, as it always has.
                return word;
            }

            // The category first — it decides which service the request is routed to, and therefore
            // which of the fields below is ever read.
            word.WordCategory ??= entry.Category;

            word.Gender ??= entry.Gender;
            word.Pattern ??= entry.Pattern;
            word.IsAnimate ??= entry.IsAnimate;
            word.HasMobileE ??= entry.HasMobileE;
            word.HasGenitivePluralShortening ??= entry.HasGenitivePluralShortening;
            word.HasEpenthesisInGenitivePlural ??= entry.HasEpenthesisInGenitivePlural;
            word.IsIndeclinable ??= entry.IsIndeclinable;
            word.IsPluralOnly ??= entry.IsPluralOnly;
            word.IsCountable ??= entry.IsCountable;
            word.PrefersShortForm ??= entry.PrefersShortForm;
            word.VerbClass ??= entry.VerbClass;
            word.Aspect ??= entry.Aspect;

            // ReflexiveType is not nullable, so None doubles as "not stated" and there is no way to tell
            // a caller who wants no particle from one who did not think about it. Filling it from the
            // lexicon is therefore only safe in that one direction: an entry that says nothing cannot
            // overwrite a caller who does.
            if (word.ReflexiveType == Enums.ReflexiveType.None)
            {
                word.ReflexiveType = entry.ReflexiveType;
            }

            return word;
        }
    }
}

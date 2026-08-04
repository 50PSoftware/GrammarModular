using Grammar.Core.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Fills in from the lexicon whatever a request did not state.
    /// </summary>
    /// <remarks>
    /// Additive by construction: it only ever writes where the request holds <see langword="null"/>, so
    /// anything the caller said stands, and a word the lexicon has never heard of goes through unchanged
    /// and fails — or succeeds — exactly as it did before. That is what lets it sit in front of every
    /// inflection without changing the meaning of any existing call.
    /// <para>
    /// The distinction it rests on is that a nullable flag has three states, not two.
    /// <c>HasMobileE = false</c> is the caller saying the word has no mobile e; <see langword="null"/> is
    /// the caller not saying. Only the second is a gap worth filling, which is also why the lexicon
    /// columns are nullable rather than defaulted.
    /// </para>
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

            var entry = _valencyProvider.GetEntry(word.Lemma);

            if (entry is null)
            {
                // Most of Czech is not in the dictionary and never will be. A word it does not hold is
                // the ordinary case, not a failure — the caller supplies the metadata itself, as it
                // always has.
                return word;
            }

            if (word.WordCategory is { } stated && stated != entry.Category)
            {
                // The lexicon holds a different word class under this lemma — stát the country against
                // stát the verb. Everything on that row describes the other word, so filling from it
                // would not complete this request but answer a different one: the caller would ask to
                // conjugate stát and be handed the vzor hrad.
                //
                // GetEntry takes a lemma and no category, so it cannot pick the right homonym; until it
                // can, refusing the wrong one is the whole of what is available. The request goes through
                // as the caller wrote it, which is what happened before there was a lexicon at all.
                return word;
            }

            // The category first, because it is what decides which service the request is routed to and
            // therefore which of the fields below will even be read. Every lexicon row carries one.
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

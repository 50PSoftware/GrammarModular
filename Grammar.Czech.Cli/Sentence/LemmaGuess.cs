using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Services;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Proposes the metadata of a lemma the lexicon does not hold, from its ending.
    /// </summary>
    /// <remarks>
    /// This is a prompt default, not a grammar rule, and it belongs here rather than in the library for
    /// that reason: most of Czech is not in the dictionary, and a tool that refused every unlisted word
    /// would be unusable, while a library that guessed silently would be wrong. What the tool infers is
    /// always shown as inferred and is always overridable.
    /// <para>
    /// Animacy is the one thing an ending cannot decide — <em>zajíc</em> and <em>hrnec</em> end alike —
    /// so it is inferred only from suffixes that name people, and left inanimate otherwise.
    /// </para>
    /// </remarks>
    public sealed class LemmaGuess
    {
        private readonly CzechVerbConjugationService _verbService;
        private readonly CzechAdjectiveDeclensionService _adjectiveService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LemmaGuess"/> type.
        /// </summary>
        /// <param name="verbService">The conjugation service, for its verb class inference.</param>
        /// <param name="adjectiveService">The adjective service, for its pattern inference.</param>
        public LemmaGuess(
            CzechVerbConjugationService verbService,
            CzechAdjectiveDeclensionService adjectiveService)
        {
            _verbService = verbService;
            _adjectiveService = adjectiveService;
        }

        /// <summary>
        /// Fills in whatever the request still leaves unsaid, from the shape of the lemma.
        /// </summary>
        /// <param name="word">The request to complete.</param>
        /// <returns>The completed request.</returns>
        /// <remarks>
        /// Additive in the same sense as the lexicon enricher: it only writes where the request holds
        /// <see langword="null"/>, so anything already known — from the dictionary or from the user —
        /// stands.
        /// </remarks>
        public CzechWordRequest Complete(CzechWordRequest word)
        {
            word.WordCategory ??= GuessCategory(word.Lemma);

            return word.WordCategory switch
            {
                WordCategory.Verb => CompleteVerb(word),
                WordCategory.Adjective => CompleteAdjective(word),
                WordCategory.Noun => CompleteNoun(word),
                _ => word,
            };
        }

        /// <summary>
        /// Determines whether the lemma looks like an infinitive.
        /// </summary>
        /// <param name="lemma">The lemma to classify.</param>
        /// <returns><see langword="true"/> when the lemma ends like an infinitive.</returns>
        public static bool LooksLikeInfinitive(string lemma) =>
            lemma.EndsWith("t", StringComparison.Ordinal)
            || lemma.EndsWith("ti", StringComparison.Ordinal)
            || lemma.EndsWith("ct", StringComparison.Ordinal)
            || lemma.EndsWith("ci", StringComparison.Ordinal);

        private static WordCategory GuessCategory(string lemma)
        {
            if (LooksLikeInfinitive(lemma))
            {
                return WordCategory.Verb;
            }

            // Koncovka -ý/-í je adjektivní, ale -í je taky vzor stavení; jméno v -í se pozná podle toho,
            // že adjektivum by nezačínalo velkým písmenem.
            if (lemma.EndsWith("ý", StringComparison.Ordinal)
                || lemma.EndsWith("á", StringComparison.Ordinal)
                || lemma.EndsWith("é", StringComparison.Ordinal)
                || (lemma.EndsWith("í", StringComparison.Ordinal) && !StartsUpper(lemma)))
            {
                return WordCategory.Adjective;
            }

            return WordCategory.Noun;
        }

        private CzechWordRequest CompleteVerb(CzechWordRequest word)
        {
            var verbClass = _verbService.GuessVerbClass(word.Lemma);

            word.VerbClass ??= verbClass;

            // Když GuessVerbClass vrátí null, je lemma samo klíčem vzoru — nést, být — a odhadovat
            // třídu z koncovky by u nich sáhlo vedle.
            word.Pattern ??= verbClass is { } inferred
                ? "trida" + ((int)inferred + 1)
                : _verbService.GuessVerbPattern(word.Lemma);

            word.Aspect ??= VerbAspect.Imperfective;

            return word;
        }

        private CzechWordRequest CompleteAdjective(CzechWordRequest word)
        {
            word.Pattern ??= _adjectiveService.GuessAdjectivePattern(word.Lemma);
            word.Degree ??= Degree.Positive;

            return word;
        }

        private static CzechWordRequest CompleteNoun(CzechWordRequest word)
        {
            var (gender, pattern, animate) = GuessNoun(word.Lemma, word.IsAnimate);

            word.Gender ??= gender;
            word.Pattern ??= pattern;
            word.IsAnimate ??= animate;

            return word;
        }

        // Životnost odjinud než z --zivotne nebo z vlastního jména odhadnout nejde — koncovka sama
        // nerozhodne (zajíc/hrnec). Když ji volající už zná (z --zivotne), musí odhad vzoru vycházet
        // z ní, ne ji tiše přebít vzorem pro opačnou životnost.
        private static (Gender Gender, string Pattern, bool Animate) GuessNoun(string lemma, bool? isAnimate)
        {
            // Velké písmeno na začátku bere tvar jako vlastní jméno: Klára je pak žena a Petr pán, ne
            // hrad. Životnost odjinud než z vlastního jména odhadnout nejde.
            var proper = StartsUpper(lemma);

            if (Ends(lemma, "ost"))
            {
                return (Gender.Feminine, "kost", false);
            }

            if (Ends(lemma, "ista") || Ends(lemma, "asta"))
            {
                return (Gender.Masculine, "turista", true);
            }

            if (Ends(lemma, "tel") || Ends(lemma, "ák") || Ends(lemma, "ář") || Ends(lemma, "ař"))
            {
                return (Gender.Masculine, Ends(lemma, "tel") ? "učitel" : "pán", true);
            }

            if (Ends(lemma, "a"))
            {
                return (Gender.Feminine, "žena", proper);
            }

            if (Ends(lemma, "e") || Ends(lemma, "ě"))
            {
                return (Gender.Feminine, "růže", false);
            }

            if (Ends(lemma, "o"))
            {
                return (Gender.Neuter, "město", false);
            }

            if (Ends(lemma, "í"))
            {
                return (Gender.Neuter, "stavení", false);
            }

            // Měkké zakončení dostane měkký vzor. 'ch' se testuje spolu s ostatními, protože pod cs-CZ
            // je to jedna kolační jednotka a EndsWith("h") by na 'mouch' vrátilo false — proto všude
            // StringComparison.Ordinal.
            if (Ends(lemma, "ď") || Ends(lemma, "ť") || Ends(lemma, "ň") || Ends(lemma, "ž")
                || Ends(lemma, "š") || Ends(lemma, "č") || Ends(lemma, "ř") || Ends(lemma, "c")
                || Ends(lemma, "j"))
            {
                var animateSoft = isAnimate ?? proper;

                return animateSoft
                    ? (Gender.Masculine, "muž", true)
                    : (Gender.Masculine, "stroj", false);
            }

            var animateHard = isAnimate ?? proper;

            return animateHard
                ? (Gender.Masculine, "pán", true)
                : (Gender.Masculine, "hrad", false);
        }

        private static bool Ends(string lemma, string ending) =>
            lemma.EndsWith(ending, StringComparison.Ordinal);

        private static bool StartsUpper(string lemma) =>
            lemma.Length > 0 && char.IsUpper(lemma[0]);
    }
}

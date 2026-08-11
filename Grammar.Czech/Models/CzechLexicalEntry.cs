using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents czech lexical entry.
    /// </summary>
    public sealed record CzechLexicalEntry : LexicalEntry
    {
        /// <summary>
        /// Gets or sets a value indicating whether the noun is animate.
        /// </summary>
        public bool? IsAnimate { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether mobile vowel alternation applies.
        /// </summary>
        /// <remarks>
        /// Named to match the has_mobile_e column and <see cref="CzechWordRequest.HasMobileE"/>.
        /// It was HasMobileVowel, which the camelCase naming policy turned into a key the data never had,
        /// so the lexicon's mobile-vowel flag deserialized to null for every entry.
        /// </remarks>
        public bool? HasMobileE { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether genitive plural shortening applies.
        /// </summary>
        public bool? HasGenitivePluralShortening { get; init; }

        /// <summary>
        /// Gets or sets the requested or resolved verb aspect.
        /// </summary>
        public VerbAspect? Aspect { get; init; }

        /// <summary>
        /// Gets or sets aspect Counterpart.
        /// </summary>
        public string? AspectCounterpart { get; init; }

        /// <summary>
        /// Gets the way the verb's action unfolds — způsob slovesného děje — or null when the entry
        /// does not classify it.
        /// </summary>
        /// <remarks>
        /// Not a finer grade of aspect. Aspect is grammatical and every Czech verb has one; this is a
        /// lexical classification of the kind of event, and most verbs are in none of its groups, which
        /// is why null means unclassified rather than "none". Where it is stated it implies the aspect —
        /// see <see cref="Core.Enums.AktionsartFacts.RequiredAspect"/>.
        /// </remarks>
        public Aktionsart? Aktionsart { get; init; }

        /// <summary>
        /// Gets or sets the reflexive type, encoding which particle (se/si) the verb requires.
        /// </summary>
        public ReflexiveType ReflexiveType { get; init; }

        // Lexical facts that used to reach the engine only through CzechWordRequest, which made every
        // caller know that banka takes an epenthetic e in the genitive plural.

        /// <summary>
        /// Gets a value indicating whether genitive plural epenthesis applies, as in banka to bank.
        /// </summary>
        public bool? HasEpenthesisInGenitivePlural { get; init; }

        /// <summary>
        /// Gets a value indicating whether the word is indeclinable.
        /// </summary>
        public bool? IsIndeclinable { get; init; }

        /// <summary>
        /// Gets a value indicating whether the word occurs only in plural forms, as in kamna or lyže.
        /// </summary>
        public bool? IsPluralOnly { get; init; }

        /// <summary>
        /// Gets a value indicating whether the noun denotes something countable.
        /// </summary>
        /// <remarks>
        /// Only the vague quantifiers care: mnoho takes the genitive plural of a countable noun
        /// (mnoho studentů) and the genitive singular of an uncountable one (mnoho práce).
        /// </remarks>
        public bool? IsCountable { get; init; }

        /// <summary>
        /// Gets a value indicating whether the shorter of two competing forms is preferred.
        /// </summary>
        public bool? PrefersShortForm { get; init; }

        /// <summary>
        /// Gets the Czech verb class, which decides the conjugation pattern.
        /// </summary>
        /// <remarks>
        /// <see cref="Services.CzechVerbConjugationService"/> guesses this from the infinitive suffix, and
        /// the suffix is not enough on its own — mazat and dělat both end in -at yet belong to different
        /// classes. A stored class overrules the guess.
        /// </remarks>
        public VerbClass? VerbClass { get; init; }

        /// <summary>
        /// Gets the verb a deverbal noun is derived from, or <see langword="null"/> for a primary noun.
        /// </summary>
        /// <remarks>
        /// příjezd inherits the frame of přijet rather than restating it; the nominal surface rules —
        /// nominative actor to genitive, accusative patient to genitive — apply on top.
        /// </remarks>
        public string? BaseVerbLemma { get; init; }

        /// <summary>
        /// Gets the circumstance this adverb expresses on its own, or <see langword="null"/> when nobody
        /// has said.
        /// </summary>
        /// <remarks>
        /// <em>dnes</em> answers when, <em>doma</em> answers where and <em>rychle</em> answers how, and
        /// nothing about the words says so — not the ending, and not the adjective the adverb was
        /// derived from, since <em>rychlý</em> and <em>rychle</em> are one word in two classes and only
        /// one of them answers "how". It is recorded per word for that reason.
        /// <para>
        /// Null is the ordinary case and means unrecorded rather than "no circumstance". A generator
        /// then has to be told the functor, which is what it had to be told for every adverb before.
        /// </para>
        /// </remarks>
        public FgdFunctor? AdverbialFunctor { get; init; }

        // Stems the word inflects on, for the verbs whose pattern does not predict them. Null is the
        // ordinary case and means the pattern decides; only what the pattern gets wrong is stated.
        // They sit on the entry rather than in the embedded irregulars.json because říct is a
        // dictionary word like any other, and correcting it should be an edit in the admin instead of
        // a rebuild and a release of the library.

        /// <summary>
        /// Gets the general stem, or <see langword="null"/> to keep the one the pattern carries.
        /// </summary>
        public string? Stem { get; init; }

        /// <summary>
        /// Gets the stem the present forms are built on, or <see langword="null"/> for the pattern's.
        /// </summary>
        public string? PresentStem { get; init; }

        /// <summary>
        /// Gets the stem the past forms are built on, or <see langword="null"/> for the pattern's.
        /// </summary>
        /// <remarks>
        /// The one that earns the rest of them: říct conjugates by the first class and still forms its
        /// past on řek-, which no rule reaches from the infinitive.
        /// </remarks>
        public string? PastStem { get; init; }

        /// <summary>
        /// Gets the stem the future forms are built on, or <see langword="null"/> for the pattern's.
        /// </summary>
        public string? FutureStem { get; init; }

        /// <summary>
        /// Gets the stem the imperative is built on, or <see langword="null"/> for the pattern's.
        /// </summary>
        public string? ImperativeStem { get; init; }

        /// <summary>
        /// Gets the stem the passive participle is built on, or <see langword="null"/> for the pattern's.
        /// </summary>
        public string? PassiveStem { get; init; }

        /// <summary>
        /// Gets the infinitive when it is not the lemma, as with říct beside říci.
        /// </summary>
        public string? Infinitive { get; init; }

        /// <summary>
        /// Gets a value indicating whether the verb forms a passive participle at all.
        /// </summary>
        /// <remarks>
        /// Nullable where <see cref="VerbPattern.FormsPassive"/> is not, for the same reason as the
        /// flags above: null is the gap the pattern fills, and only a verb that refuses — moci has no
        /// passive participle where pomoci has pomožen — has to say so.
        /// </remarks>
        public bool? FormsPassive { get; init; }

        /// <summary>
        /// Gets a value indicating whether the entry states anything the pattern would otherwise decide.
        /// </summary>
        /// <remarks>
        /// Almost no entry does — a stem is written only where the pattern gets the word wrong — so this
        /// is what lets conjugation skip the override for the ordinary verb instead of rebuilding the
        /// pattern and the structure on every form.
        /// </remarks>
        public bool HasStems =>
            Stem is not null
            || PresentStem is not null
            || PastStem is not null
            || FutureStem is not null
            || ImperativeStem is not null
            || PassiveStem is not null
            || Infinitive is not null
            || FormsPassive is not null;
    }
}

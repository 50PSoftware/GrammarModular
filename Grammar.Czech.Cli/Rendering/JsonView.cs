using Grammar.Czech.Cli.Sentence;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Cli.Rendering
{
    /// <summary>
    /// Writes the finished sentence out as JSON, together with the analysis it came from.
    /// </summary>
    /// <remarks>
    /// The sentence alone would be the smaller half. What a caller downstream cannot recompute is which
    /// role each word ended up in, which case governs it and which frame decided that — so the breakdown
    /// travels with it.
    /// <para>
    /// Keys are unaccented, values are not. A key is typed into a script; a value is read by a person.
    /// </para>
    /// </remarks>
    public static class JsonView
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,

            // Bez tohohle by z 'češtinu' bylo 'češtinu' — platný JSON, nečitelný výstup.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Renders the draft and the sentence built from it.
        /// </summary>
        /// <param name="draft">The sentence draft the sentence was built from.</param>
        /// <param name="sentence">The assembled sentence.</param>
        /// <returns>The JSON document.</returns>
        public static string Render(SentenceDraft draft, string sentence) => JsonSerializer.Serialize(
            new
            {
                veta = sentence,
                klauze = draft.Clauses.Select(Describe).ToList(),
                poznamky = draft.Notes.ToList(),
            },
            Options);

        private static object Describe(ClauseDraft draft) => new
            {
                poradi = draft.Ordinal,
                spojka = draft.Conjunction,
                visi_na = draft.ParentOrdinal,
                prisudek = new
                {
                    lemma = draft.PredicateLemma,
                    vzor = draft.Predicate.Pattern,
                    vid = draft.Predicate.Aspect is { } aspect ? Terms.Name(aspect) : null,
                    cas = draft.Predicate.Tense is { } tense ? Terms.Name(tense) : null,
                    zpusob = draft.Predicate.Modus is { } mood ? Terms.Name(mood) : null,
                    rod = draft.Predicate.Voice is { } voice ? Terms.Name(voice) : null,
                    osoba = draft.Predicate.Person is { } person ? (int?)person : null,
                    cislo = draft.Predicate.Number is { } number ? Terms.Name(number) : null,
                    zapor = draft.Predicate.IsNegative,
                    zdroj = Source(draft.PredicateOrigin),
                },
                ramec = draft.Frame is null ? null : new
                {
                    popisek = draft.Frame.FrameLabel,
                    diateze = draft.Frame.Diathesis.ToString(),
                    sloty = draft.Frame.Slots
                        .OrderBy(slot => slot.CanonicalOrder)
                        .Select(slot => slot.Functor.ToString())
                        .ToList(),
                },
                cleny = draft.Constituents.Select(constituent => new
                {
                    poradi = constituent.Position,
                    lemma = constituent.Lemma,
                    role = constituent.Functor?.ToString(),
                    cleneni = Terms.Name(constituent.Status),
                    predlozka = constituent.EffectivePreposition,
                    pad = constituent.EffectiveCase is { } kase ? Terms.Name(kase) : null,
                    pad_z_ramce = constituent.Word.Case is null && constituent.FrameCase is not null,
                    rod = constituent.Word.Gender is { } gender ? Terms.Name(gender) : null,
                    cislo = constituent.Word.Number is { } number ? Terms.Name(number) : null,
                    vzor = constituent.Word.Pattern,
                    zivotne = constituent.Word.IsAnimate,
                    privlastky = constituent.Modifiers.Select(modifier => modifier.Lemma).ToList(),
                    zdroj = Source(constituent.Origin),
                }).ToList(),
            };

        private static string Source(MetadataOrigin origin) => origin switch
        {
            MetadataOrigin.Lexicon => "slovník",
            MetadataOrigin.Rules => "pravidla",
            MetadataOrigin.Guess => "odhad",
            _ => "zadáno",
        };
    }
}

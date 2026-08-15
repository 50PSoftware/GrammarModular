using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides czech pronoun operations.
    /// </summary>
    public class CzechPronounService : ICzechPronounService, IInflectionService<CzechWordRequest>
    {
        private readonly Dictionary<string, PronounData> _pronouns;
        private readonly Dictionary<string, PronounParadigm> _paradigms;
        private readonly CzechAdjectiveDeclensionService _adjectiveService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechPronounService"/> type.
        /// </summary>
        public CzechPronounService(
            IPronounDataProvider provider,
            CzechAdjectiveDeclensionService adjectiveService)
        {
            _pronouns = provider.GetPronouns();
            _paradigms = provider.GetParadigms();
            _adjectiveService = adjectiveService;
        }

        // ── Veřejné API ────────────────────────────────────────────────

        /// <summary>
        /// Attempts to resolve a pronoun form for the supplied grammatical options.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="grammaticalCase">The grammatical case requested for the generated form.</param>
        /// <returns>The matching form when the paradigm contains one; otherwise, null.</returns>
        public string? TryGetForm(string lemma, Case grammaticalCase)
            => TryGetForm(lemma, grammaticalCase, null, null, null, null);

        /// <summary>
        /// Attempts to resolve a pronoun form for the supplied grammatical options.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="grammaticalCase">The grammatical case requested for the generated form.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="animate">True when the masculine form is animate; otherwise, false.</param>
        /// <param name="options">The JSON serializer options used to deserialize the resource.</param>
        /// <returns>The matching form when the paradigm contains one; otherwise, null.</returns>
        public string? TryGetForm(
            string lemma,
            Case grammaticalCase,
            Gender? gender,
            Number? number,
            bool? animate,
            PronounFormOptions? options)
        {
            if (!_pronouns.TryGetValue(lemma, out var data))
                return null;

            return data.InflectionClass switch
            {
                InflectionClass.Substantive => LookupFixedForm(data, grammaticalCase, options),
                InflectionClass.PronounHard => LookupParadigm(data, grammaticalCase, gender, number, animate),
                InflectionClass.PronounSoft => LookupParadigm(data, grammaticalCase, gender, number, animate),
                InflectionClass.AdjectiveHard => DelegateToAdjective(lemma, data, grammaticalCase, gender, number, animate),
                InflectionClass.AdjectiveSoft => DelegateToAdjective(lemma, data, grammaticalCase, gender, number, animate),
                InflectionClass.Indeclinable => lemma,
                _ => null
            };
        }

        // ── IInflectionService<CzechWordRequest> ───────────────────────

        /// <summary>
        /// Builds the requested inflected form.
        /// </summary>
        /// <param name="request">The Czech word request to process.</param>
        /// <returns>The generated pronoun form.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the lemma is not a registered pronoun or has no form for the requested categories.
        /// </exception>
        /// <remarks>
        /// This used to fall back to the lemma when nothing resolved, which hid the failure behind a word
        /// that often looked plausible: a request for the lemma "to" — a form of "ten", not a lemma —
        /// resolved to nothing and came back as "to" anyway. Pronouns are a closed class, so not finding one
        /// is a mistake in the request, and it is reported like the preposition and conjunction lookups are.
        /// </remarks>
        public WordForm GetForm(CzechWordRequest request)
        {
            if (request.Case is null)
                throw new ArgumentException("Case must be specified for pronoun inflection.", nameof(request));

            var options = BuildOptions(request);
            var form = TryGetForm(
                request.Lemma,
                request.Case.Value,
                request.Gender,
                request.Number,
                request.IsAnimate,
                options);

            if (form is null)
            {
                throw new InvalidOperationException(
                    _pronouns.ContainsKey(request.Lemma)
                        ? $"Zájmeno '{request.Lemma}' nemá tvar pro pád {request.Case}, rod {request.Gender} a číslo {request.Number}."
                        : $"'{request.Lemma}' není lemma zájmena. Zkontroluj, jestli nejde o tvar — 'to' je tvar lemmatu 'ten'.");
            }

            return new WordForm(form);
        }

        // ── ICzechPronounService helpers ───────────────────────────────

        /// <summary>
        /// Gets the cases available for the requested pronoun paradigm.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The cases supported by the pronoun data, or all cases for paradigm-based pronouns.</returns>
        public IEnumerable<Case> GetAvailableCases(string lemma)
        {
            if (!_pronouns.TryGetValue(lemma, out var data))
                return Enumerable.Empty<Case>();

            if (data.InflectionClass == InflectionClass.Substantive && data.FixedForms != null)
                return data.FixedForms.Keys;

            // Pro paradigmatická a adjektivní zájmena vrátíme všechny pády
            return Enum.GetValues<Case>();
        }

        /// <summary>
        /// Determines whether the requested pronoun case can be generated.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="grammaticalCase">The grammatical case requested for the generated form.</param>
        /// <returns><see langword="true"/> when a form exists for the requested case; otherwise, <see langword="false"/>.</returns>
        public bool IsAllowed(string lemma, Case grammaticalCase)
            => TryGetForm(lemma, grammaticalCase) != null;

        /// <summary>
        /// Gets the pronoun type used to select a paradigm.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The pronoun type stored for the lemma, or <see langword="null"/> when the lemma is unknown.</returns>
        public PronounType? GetPronounType(string lemma)
            => _pronouns.TryGetValue(lemma, out var data) ? data.Type : null;

        /// <summary>
        /// Gets every reading the pronoun has, the primary one first.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>
        /// The readings, or an empty list when the lemma is not a registered pronoun.
        /// </returns>
        public IReadOnlyList<PronounData> GetReadings(string lemma)
            => _pronouns.TryGetValue(lemma, out var data)
                ? [data, .. data.AlsoReads]
                : [];

        /// <summary>
        /// Determines whether the lemma is a relative pronoun that possesses rather than stands for a
        /// participant.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>
        /// <see langword="true"/> for jehož, jejíž and jejichž; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsPossessiveRelative(string lemma)
        {
            var readings = GetReadings(lemma);

            return readings.Any(reading => reading.Type == PronounType.Relative)
                && readings.Any(reading => reading.Type == PronounType.Possessive);
        }

        /// <summary>
        /// Gets the inflection class used to choose pronoun form lookup.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The inflection class stored for the lemma, or <see langword="null"/> when the lemma is unknown.</returns>
        public InflectionClass? GetInflectionClass(string lemma)
            => _pronouns.TryGetValue(lemma, out var data) ? data.InflectionClass : null;

        // ── Privátní metody ────────────────────────────────────────────

        private string? LookupFixedForm(PronounData data, Case grammaticalCase, PronounFormOptions? options)
        {
            if (data.FixedForms == null)
                return null;

            if (!data.FixedForms.TryGetValue(grammaticalCase, out var caseForms))
                return null;

            return SelectBestForm(caseForms, options);
        }

        private string? LookupParadigm(
            PronounData data,
            Case grammaticalCase,
            Gender? gender,
            Number? number,
            bool? animate)
        {
            if (data.ParadigmId == null)
                return null;

            if (!_paradigms.TryGetValue(data.ParadigmId, out var paradigm))
                return null;

            var numberKey = (number ?? Number.Singular);
            if (!paradigm.Slots.TryGetValue(numberKey, out var genderSlots))
                return null;

            var slot = ResolveGenderSlot(gender, number, animate, genderSlots);
            if (slot == null)
                return null;

            if (!slot.TryGetValue(grammaticalCase, out var form))
                return null;

            // Prefix pro nikdo/někdo/nic/něco
            return string.IsNullOrEmpty(data.Prefix)
                ? form
                : data.Prefix + form;
        }

        private string? DelegateToAdjective(
            string lemma,
            PronounData data,
            Case grammaticalCase,
            Gender? gender,
            Number? number,
            bool? animate)
        {
            if (data.DeclensionPattern == null || gender == null || number == null)
                return null;

            var adjectiveRequest = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Adjective,
                Pattern = data.DeclensionPattern,
                Gender = gender,
                Number = number,
                Case = grammaticalCase,
                IsAnimate = animate,
                Degree = Degree.Positive,
            };

            return _adjectiveService.GetForm(adjectiveRequest).Form;
        }

        private static Dictionary<Case, string>? ResolveGenderSlot(
            Gender? gender,
            Number? number,
            bool? animate,
            Dictionary<GenderSlot, Dictionary<Case, string>> genderSlots)
        {
            // Plurál — zkus MasculineAnimate, jinak Other
            if (number == Number.Plural)
            {
                if (gender == Gender.Masculine && animate == true
                    && genderSlots.TryGetValue(GenderSlot.MasculineAnimate, out var maPlural))
                    return maPlural;

                if (genderSlots.TryGetValue(GenderSlot.Other, out var other))
                    return other;
            }

            // Singulár — přesný slot
            var targetSlot = (gender, animate) switch
            {
                (Gender.Masculine, true) => GenderSlot.MasculineAnimate,
                (Gender.Masculine, false) => GenderSlot.MasculineInanimate,
                (Gender.Feminine, _) => GenderSlot.Feminine,
                (Gender.Neuter, _) => GenderSlot.Neuter,
                _ => GenderSlot.MasculineAnimate // fallback pro kdo/co
            };

            return genderSlots.TryGetValue(targetSlot, out var slot) ? slot : null;
        }

        private static PronounFormOptions? BuildOptions(CzechWordRequest request)
        {
            // Prozatím mapujeme jen AfterPreposition — rozšiř dle potřeby
            if (request.IsAfterPreposition == true)
                return new PronounFormOptions { AfterPreposition = true };

            return null;
        }

        private static string? SelectBestForm(PronounCaseForms caseForms, PronounFormOptions? options)
        {
            if (options == null)
                return caseForms.Default
                    ?? caseForms.AfterPreposition
                    ?? caseForms.Clitic
                    ?? caseForms.Rare;

            if (options.AfterPreposition)
                return caseForms.AfterPreposition
                    ?? caseForms.Default
                    ?? caseForms.Rare
                    ?? caseForms.Clitic;

            if (options.PreferClitic)
                return caseForms.Clitic
                    ?? caseForms.Default
                    ?? caseForms.Rare
                    ?? caseForms.AfterPreposition;

            if (options.PreferRare)
                return caseForms.Rare
                    ?? caseForms.Default
                    ?? caseForms.AfterPreposition
                    ?? caseForms.Clitic;

            return caseForms.Default
                ?? caseForms.AfterPreposition
                ?? caseForms.Clitic
                ?? caseForms.Rare;
        }
    }
}

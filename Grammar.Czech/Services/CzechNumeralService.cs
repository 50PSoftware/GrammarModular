using Grammar.Core.Enums;
using Grammar.Core.Models.Word;
using Grammar.Czech.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides Czech numeral operations.
    /// </summary>
    public class CzechNumeralService : ICzechNumeralService
    {
        private static readonly Regex NumericLemma = new(@"^\d+(,\d+)?$", RegexOptions.Compiled);

        private readonly Dictionary<string, NumeralData> _numerals;
        private readonly Dictionary<string, NumeralParadigm> _paradigms;
        private readonly CzechAdjectiveDeclensionService _adjectiveService;
        private readonly CzechNounDeclensionService _nounService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechNumeralService"/> type.
        /// </summary>
        public CzechNumeralService(
            INumeralDataProvider provider,
            CzechAdjectiveDeclensionService adjectiveService,
            CzechNounDeclensionService nounService)
        {
            _numerals = provider.GetNumerals();
            _paradigms = provider.GetParadigms();
            _adjectiveService = adjectiveService;
            _nounService = nounService;
        }

        // ── Veřejné API ────────────────────────────────────────────────

        /// <summary>
        /// Attempts to resolve a numeral form for the supplied grammatical options.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="grammaticalCase">The grammatical case requested for the generated form.</param>
        /// <returns>The matching form when the numeral has one; otherwise, null.</returns>
        public string? TryGetForm(string lemma, Case grammaticalCase)
            => TryGetForm(lemma, grammaticalCase, null, null, null, null);

        /// <summary>
        /// Attempts to resolve a numeral form for the supplied grammatical options.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="grammaticalCase">The grammatical case requested for the generated form.</param>
        /// <param name="gender">The grammatical gender requested for the generated form.</param>
        /// <param name="number">The grammatical number requested for the generated form.</param>
        /// <param name="isAnimate">True when the masculine form is animate; otherwise, false.</param>
        /// <param name="options">The options selecting between competing standard forms.</param>
        /// <returns>The matching form when the numeral has one; otherwise, null.</returns>
        public string? TryGetForm(
            string lemma,
            Case grammaticalCase,
            Gender? gender,
            Number? number,
            bool? isAnimate,
            NumeralFormOptions? options)
        {
            if (!_numerals.TryGetValue(lemma, out var data))
            {
                // A numeral written in digits stands for itself and does not decline: 1,5 metru, 25 studentů.
                return TryParseNumericLemma(lemma, out _) ? lemma : null;
            }

            var effectiveNumber = data.FixedNumber ?? number;

            // Overrides win over everything, which is what makes them usable as the escape hatch for a
            // delegated pattern that gets one cell wrong — sto is a město whose locative is stu, not stě.
            var overridden = LookupOverride(data, grammaticalCase, effectiveNumber, options);
            if (overridden != null)
                return overridden;

            return data.Morphology switch
            {
                NumeralMorphology.Pronominal
                    or NumeralMorphology.DualRelic
                    or NumeralMorphology.ThreeFour => LookupParadigm(data, grammaticalCase, gender, effectiveNumber, isAnimate),
                NumeralMorphology.FiveNinetyNine => BuildTwoFormCardinal(lemma, grammaticalCase),
                NumeralMorphology.HardAdjective
                    or NumeralMorphology.SoftAdjective => DelegateToAdjective(lemma, data, grammaticalCase, gender, effectiveNumber, isAnimate),
                NumeralMorphology.NounMasculine
                    or NumeralMorphology.NounNeuter
                    or NumeralMorphology.NounFeminine => DelegateToNoun(lemma, data, grammaticalCase, effectiveNumber),
                NumeralMorphology.Adverb
                    or NumeralMorphology.Indeclinable => lemma,
                _ => null
            };
        }

        /// <summary>
        /// Builds the requested inflected form.
        /// </summary>
        /// <param name="request">The Czech word request to process.</param>
        /// <returns>The generated numeral form.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the lemma is not a registered numeral or has no form for the requested categories.
        /// </exception>
        /// <remarks>
        /// Falling back to the lemma here hid the same failure it hid for pronouns: numeral forms look like
        /// lemmas, so a request for "pěti" or "dvou" — or "tří", which differs from the lemma "tři" by a
        /// single mark — resolved to nothing and came back unchanged, reading as an answer.
        /// </remarks>
        public WordForm GetForm(CzechWordRequest request)
        {
            if (request.Case is null)
                throw new ArgumentException("Case must be specified for numeral inflection.", nameof(request));

            var form = TryGetForm(
                request.Lemma,
                request.Case.Value,
                request.Gender,
                request.Number,
                request.IsAnimate,
                null);

            if (form is null)
            {
                throw new InvalidOperationException(
                    _numerals.ContainsKey(request.Lemma)
                        ? $"Číslovka '{request.Lemma}' nemá tvar pro pád {request.Case}, rod {request.Gender} a číslo {request.Number}."
                        : $"'{request.Lemma}' není lemma číslovky. Zkontroluj, jestli nejde o tvar — 'pěti' je tvar lemmatu 'pět'.");
            }

            return new WordForm(form);
        }

        /// <summary>
        /// Gets the semantic kind of the numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The numeral type, or <see langword="null"/> when the lemma is unknown.</returns>
        public NumeralType? GetNumeralType(string lemma)
            => _numerals.TryGetValue(lemma, out var data) ? data.Type : null;

        /// <summary>
        /// Gets the inflection class of the numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The numeral morphology, or <see langword="null"/> when the lemma is unknown.</returns>
        public NumeralMorphology? GetMorphology(string lemma)
            => _numerals.TryGetValue(lemma, out var data) ? data.Morphology : null;

        /// <summary>
        /// Gets what the numeral imposes on the noun it counts.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The agreement the numeral governs with, or None when the lemma is unknown.</returns>
        public CardinalAgreement GetAgreement(string lemma)
        {
            if (_numerals.TryGetValue(lemma, out var data))
                return data.Agreement;

            return TryParseNumericLemma(lemma, out var value)
                ? GetAgreementForValue(value)
                : CardinalAgreement.None;
        }

        /// <summary>
        /// Gets what a numeric value imposes on the noun it counts.
        /// </summary>
        /// <param name="value">The value counted with.</param>
        /// <returns>The agreement the counted noun follows.</returns>
        public CardinalAgreement GetAgreementForValue(decimal value)
        {
            // A decimal is governed by its last named place, which is a fraction — so the counted noun goes
            // into the genitive singular: 0,5 metru, 2,36 litru, 14,25 sekundy (IJP id=792).
            if (value != decimal.Truncate(value))
                return CardinalAgreement.GenitiveSingular;

            return (long)value switch
            {
                1 => CardinalAgreement.AgreesSingular,
                2 or 3 or 4 => CardinalAgreement.AgreesPlural,
                > 4 and < 100 => CardinalAgreement.GenitivePluralInDirectCases,
                _ => CardinalAgreement.AlwaysGenitivePlural
            };
        }

        /// <summary>
        /// Resolves the case and number the counted noun takes.
        /// </summary>
        /// <param name="agreement">What the numeral imposes.</param>
        /// <param name="phraseCase">The case the whole numeral phrase stands in.</param>
        /// <param name="isCountable">False when the noun denotes something uncountable.</param>
        /// <returns>The case and number of the counted noun.</returns>
        public (Case NounCase, Number NounNumber) ResolveCountedForm(
            CardinalAgreement agreement,
            Case phraseCase,
            bool isCountable = true)
        {
            // What decides the genitive is the case of the phrase. "Pro pět studentů" keeps it because the
            // preposition governs the accusative, which is direct; "o pěti studentech" loses it because the
            // locative is not.
            var isDirect = phraseCase is Case.Nominative or Case.Accusative or Case.Vocative;

            return agreement switch
            {
                CardinalAgreement.AgreesSingular => (phraseCase, Number.Singular),
                CardinalAgreement.AgreesPlural => (phraseCase, Number.Plural),
                CardinalAgreement.AlwaysGenitivePlural => (Case.Genitive, Number.Plural),
                CardinalAgreement.GenitiveSingular => (Case.Genitive, Number.Singular),
                CardinalAgreement.GenitivePluralInDirectCases when !isCountable => (Case.Genitive, Number.Singular),
                CardinalAgreement.GenitivePluralInDirectCases => isDirect
                    ? (Case.Genitive, Number.Plural)
                    : (phraseCase, Number.Plural),
                _ => (phraseCase, Number.Singular)
            };
        }

        /// <summary>
        /// Gets the numeric value of the numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The value, or <see langword="null"/> for indefinites and unknown lemmas.</returns>
        public decimal? GetValue(string lemma)
        {
            if (_numerals.TryGetValue(lemma, out var data))
                return data.Value;

            return TryParseNumericLemma(lemma, out var value) ? value : null;
        }

        /// <summary>
        /// Determines whether the lemma is a known numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns><see langword="true"/> when the numeral is known; otherwise, <see langword="false"/>.</returns>
        public bool IsNumeral(string lemma) => _numerals.ContainsKey(lemma);

        /// <summary>
        /// Gets the raw data entry for the numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The numeral data, or <see langword="null"/> when the lemma is unknown.</returns>
        public NumeralData? GetData(string lemma)
            => _numerals.TryGetValue(lemma, out var data) ? data : null;

        // ── Privátní metody ────────────────────────────────────────────

        // A numeral may reach the service already written in digits, which is how a decimal gets here at
        // all — there is no lemma for 1,5. The comma is the Czech decimal separator; a trailing full stop
        // is not a separator but the ordinal marker, and is deliberately not accepted here.
        private static bool TryParseNumericLemma(string lemma, out decimal value)
        {
            value = 0m;

            return NumericLemma.IsMatch(lemma)
                && decimal.TryParse(
                    lemma.Replace(',', '.'),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out value);
        }

        // Five upwards has only two forms: bare in the direct cases, plus -i everywhere else. Written as a
        // rule rather than as data, which keeps every numeral from pět to devadesát down to one metadata
        // line. Verified against pěti, šesti, sedmi, osmi, deseti, jedenácti, dvaceti, padesáti, devadesáti.
        // The one exception, devět → devíti, is carried by that entry's overrides and never reaches here.
        private static string BuildTwoFormCardinal(string lemma, Case grammaticalCase) =>
            grammaticalCase is Case.Nominative or Case.Accusative or Case.Vocative
                ? lemma
                : lemma + "i";

        private static string? LookupOverride(
            NumeralData data,
            Case grammaticalCase,
            Number? number,
            NumeralFormOptions? options)
        {
            if (data.Overrides == null)
                return null;

            var numberKey = number == Number.Plural ? "plural" : "singular";

            // The number-specific block wins; "any" carries the forms that hold whatever the number.
            if (data.Overrides.TryGetValue(numberKey, out var specific)
                && specific.TryGetValue(grammaticalCase, out var specificForms))
                return SelectBestForm(specificForms, options);

            if (data.Overrides.TryGetValue("any", out var shared)
                && shared.TryGetValue(grammaticalCase, out var sharedForms))
                return SelectBestForm(sharedForms, options);

            return null;
        }

        private string? LookupParadigm(
            NumeralData data,
            Case grammaticalCase,
            Gender? gender,
            Number? number,
            bool? isAnimate)
        {
            if (data.ParadigmId == null)
                return null;

            if (!_paradigms.TryGetValue(data.ParadigmId, out var paradigm))
                return null;

            var genderSlots = ResolveNumberSlot(paradigm, number);
            if (genderSlots == null)
                return null;

            var slot = ResolveGenderSlot(gender, isAnimate, genderSlots);
            if (slot == null)
                return null;

            return slot.TryGetValue(grammaticalCase, out var form) ? form : null;
        }

        private string? DelegateToAdjective(
            string lemma,
            NumeralData data,
            Case grammaticalCase,
            Gender? gender,
            Number? number,
            bool? isAnimate)
        {
            if (data.DeclensionPattern == null)
                return null;

            var request = new CzechWordRequest
            {
                Lemma = data.DelegationLemma ?? lemma,
                WordCategory = WordCategory.Adjective,
                Pattern = data.DeclensionPattern,
                Gender = gender ?? Gender.Masculine,
                Number = number ?? Number.Singular,
                Case = grammaticalCase,
                IsAnimate = isAnimate ?? true,
                Degree = Degree.Positive,
            };

            return _adjectiveService.GetForm(request).Form;
        }

        // The scale words are nouns and decline as such: sto after město, tisíc after stroj, milion after
        // hrad, miliarda after žena. The pronoun service has had DelegateToAdjective from the start; this is
        // its missing twin.
        private string? DelegateToNoun(
            string lemma,
            NumeralData data,
            Case grammaticalCase,
            Number? number)
        {
            if (data.DeclensionPattern == null)
                return null;

            var request = new CzechWordRequest
            {
                Lemma = data.DelegationLemma ?? lemma,
                WordCategory = WordCategory.Noun,
                Pattern = data.DeclensionPattern,
                Gender = data.Gender,
                Number = number ?? Number.Singular,
                Case = grammaticalCase,
                IsAnimate = data.IsAnimate ?? false,
            };

            return _nounService.GetForm(request).Form;
        }

        // Most numerals store their forms under Any because they do not distinguish number at all. Asking
        // for the requested number first and falling back to Any lets jeden — which does distinguish —
        // share one lookup with dva and pět, which do not.
        private static Dictionary<GenderSlot, Dictionary<Case, string>>? ResolveNumberSlot(
            NumeralParadigm paradigm,
            Number? number)
        {
            var requested = number == Number.Plural ? NumberSlot.Plural : NumberSlot.Singular;

            if (paradigm.Slots.TryGetValue(requested, out var exact))
                return exact;

            return paradigm.Slots.TryGetValue(NumberSlot.Any, out var any) ? any : null;
        }

        // Exact slot first, then the shared Other slot, then masculine animate. The last step is what
        // answers a caller that supplied no gender at all — pět has no gender to speak of.
        private static Dictionary<Case, string>? ResolveGenderSlot(
            Gender? gender,
            bool? isAnimate,
            Dictionary<GenderSlot, Dictionary<Case, string>> genderSlots)
        {
            var targetSlot = (gender, isAnimate) switch
            {
                (Gender.Masculine, false) => GenderSlot.MasculineInanimate,
                (Gender.Masculine, _) => GenderSlot.MasculineAnimate,
                (Gender.Feminine, _) => GenderSlot.Feminine,
                (Gender.Neuter, _) => GenderSlot.Neuter,
                _ => GenderSlot.Other
            };

            if (genderSlots.TryGetValue(targetSlot, out var slot))
                return slot;

            if (genderSlots.TryGetValue(GenderSlot.Other, out var other))
                return other;

            return genderSlots.TryGetValue(GenderSlot.MasculineAnimate, out var masculine) ? masculine : null;
        }

        private static string? SelectBestForm(NumeralCaseForms caseForms, NumeralFormOptions? options)
        {
            if (options == null)
                return caseForms.Default ?? caseForms.Colloquial ?? caseForms.Rare ?? caseForms.Paired;

            if (options.Paired)
                return caseForms.Paired ?? caseForms.Default ?? caseForms.Colloquial ?? caseForms.Rare;

            if (options.PreferColloquial)
                return caseForms.Colloquial ?? caseForms.Default ?? caseForms.Rare ?? caseForms.Paired;

            if (options.PreferRare)
                return caseForms.Rare ?? caseForms.Default ?? caseForms.Colloquial ?? caseForms.Paired;

            return caseForms.Default ?? caseForms.Colloquial ?? caseForms.Rare ?? caseForms.Paired;
        }
    }
}

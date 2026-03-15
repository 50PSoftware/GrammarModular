using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    public class CzechPronounService : ICzechPronounService, IInflectionService<CzechWordRequest>
    {
        private readonly Dictionary<string, PronounData> _pronouns;
        private readonly Dictionary<string, PronounParadigm> _paradigms;
        private readonly CzechAdjectiveDeclensionService _adjectiveService;

        public CzechPronounService(
            IPronounDataProvider provider,
            CzechAdjectiveDeclensionService adjectiveService)
        {
            _pronouns = provider.GetPronouns();
            _paradigms = provider.GetParadigms();
            _adjectiveService = adjectiveService;
        }

        // ── Veřejné API ────────────────────────────────────────────────

        public string? TryGetForm(string lemma, Case grammaticalCase)
            => TryGetForm(lemma, grammaticalCase, null, null, null, null);

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

            return new WordForm(form ?? request.Lemma);
        }

        // ── ICzechPronounService helpers ───────────────────────────────

        public IEnumerable<Case> GetAvailableCases(string lemma)
        {
            if (!_pronouns.TryGetValue(lemma, out var data))
                return Enumerable.Empty<Case>();

            if (data.InflectionClass == InflectionClass.Substantive && data.FixedForms != null)
                return data.FixedForms.Keys;

            // Pro paradigmatická a adjektivní zájmena vrátíme všechny pády
            return Enum.GetValues<Case>();
        }

        public bool IsAllowed(string lemma, Case grammaticalCase)
            => TryGetForm(lemma, grammaticalCase) != null;

        public PronounType? GetPronounType(string lemma)
            => _pronouns.TryGetValue(lemma, out var data) ? data.Type : null;

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

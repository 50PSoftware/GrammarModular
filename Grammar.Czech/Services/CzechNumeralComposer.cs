using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Represents one order-of-magnitude group of a spelled-out numeral.
    /// </summary>
    /// <param name="Multiplier">How many of the scale there are, or the value itself below a hundred.</param>
    /// <param name="ScaleLemma">The scale word being multiplied, or null for the tens and units.</param>
    public readonly record struct NumeralGroup(long Multiplier, string? ScaleLemma);

    /// <summary>
    /// Spells a number out as a declined Czech numeral.
    /// </summary>
    /// <remarks>
    /// The reference is the Internetová jazyková příručka ÚJČ, id=791. Its primary norm is that every part of
    /// a multi-word numeral declines — před třemi sty šedesáti pěti lety — which is what this produces.
    /// </remarks>
    public class CzechNumeralComposer
    {
        private static readonly (long Value, string Lemma)[] Scales =
        [
            (1_000_000_000_000L, "bilion"),
            (1_000_000_000L, "miliarda"),
            (1_000_000L, "milion"),
            (1_000L, "tisíc"),
            (100L, "sto")
        ];

        private readonly ICzechNumeralService _numeralService;
        private readonly CzechAdjectiveDeclensionService _adjectiveService;
        private readonly Dictionary<NumeralType, Dictionary<long, string>> _byTypeAndValue;
        private readonly Dictionary<long, string> _cardinalsByValue;
        private readonly Dictionary<long, string> _ordinalsByValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechNumeralComposer"/> type.
        /// </summary>
        public CzechNumeralComposer(
            ICzechNumeralService numeralService,
            INumeralDataProvider provider,
            CzechAdjectiveDeclensionService adjectiveService)
        {
            _numeralService = numeralService;
            _adjectiveService = adjectiveService;

            // Several lemmas can share a value within a type — dvojice and dvojka are both groups of two —
            // so the first one wins and the rest stay reachable by lemma through the service.
            _byTypeAndValue = provider.GetNumerals()
                .Where(entry => entry.Value.Value.HasValue)
                .GroupBy(entry => entry.Value.Type)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .GroupBy(entry => (long)entry.Value.Value!.Value)
                        .ToDictionary(byValue => byValue.Key, byValue => byValue.First().Key));

            _cardinalsByValue = LookupTable(NumeralType.Cardinal);
            _ordinalsByValue = LookupTable(NumeralType.Ordinal);
        }

        private Dictionary<long, string> LookupTable(NumeralType type)
            => _byTypeAndValue.TryGetValue(type, out var table) ? table : [];

        /// <summary>
        /// Names the value with the numeral of the requested semantic kind, declined.
        /// </summary>
        /// <param name="value">The value to name.</param>
        /// <param name="type">The semantic kind of numeral wanted.</param>
        /// <param name="grammaticalCase">The case the numeral stands in.</param>
        /// <param name="gender">The gender of the noun it goes with.</param>
        /// <param name="isAnimate">True when that noun is masculine animate; otherwise, false.</param>
        /// <param name="number">The number wanted, for the kinds that inflect for it.</param>
        /// <returns>The declined numeral of that kind.</returns>
        /// <remarks>
        /// Unlike cardinals and ordinals, the other kinds are not composed from parts — there is no
        /// multi-word sortal — so this is a lookup by value plus declension. A value the lexicon has no
        /// lemma for fails loudly rather than inventing one. Use <see cref="Compose(long, Case, Gender?, bool?)"/>
        /// for cardinals and <see cref="ComposeOrdinal"/> for ordinals, which do compose.
        /// </remarks>
        public string ComposeOfType(
            long value,
            NumeralType type,
            Case grammaticalCase,
            Gender? gender = null,
            bool? isAnimate = null,
            Number? number = null)
        {
            if (type == NumeralType.Cardinal)
            {
                return Compose(value, grammaticalCase, gender, isAnimate);
            }

            if (type == NumeralType.Ordinal)
            {
                return ComposeOrdinal(value, grammaticalCase, gender, isAnimate);
            }

            var lemma = LookupTable(type).TryGetValue(value, out var found)
                ? found
                : throw new InvalidOperationException(
                    $"Pro hodnotu {value} není ve slovníku číslovka druhu {type}.");

            return _numeralService.TryGetForm(lemma, grammaticalCase, gender, number, isAnimate, null) ?? lemma;
        }

        /// <summary>
        /// Spells the value out as a declined cardinal numeral.
        /// </summary>
        /// <param name="value">The value to spell out.</param>
        /// <param name="grammaticalCase">The case the whole numeral stands in.</param>
        /// <param name="gender">The gender of the noun being counted, which only one and two reflect.</param>
        /// <param name="isAnimate">True when the counted noun is masculine animate; otherwise, false.</param>
        /// <returns>The spelled-out numeral, its parts separated by single spaces.</returns>
        public string Compose(long value, Case grammaticalCase, Gender? gender = null, bool? isAnimate = null)
            => Compose(value, grammaticalCase, CompoundVariant.Preferred, gender, isAnimate);

        /// <summary>
        /// Spells the value out as a declined cardinal numeral under a chosen treatment of compounds.
        /// </summary>
        /// <param name="value">The value to spell out.</param>
        /// <param name="grammaticalCase">The case the whole numeral stands in.</param>
        /// <param name="variant">Which of the three standard treatments of a compound to follow.</param>
        /// <param name="gender">The gender of the noun being counted, which only one and two reflect.</param>
        /// <param name="isAnimate">True when the counted noun is masculine animate; otherwise, false.</param>
        /// <returns>The spelled-out numeral, its parts separated by single spaces.</returns>
        public string Compose(long value, Case grammaticalCase, CompoundVariant variant, Gender? gender = null, bool? isAnimate = null)
            => string.Join(' ', ComposeParts(value, grammaticalCase, gender, isAnimate, variant));

        /// <summary>
        /// Spells the value out as a declined cardinal numeral, one word per element.
        /// </summary>
        /// <param name="value">The value to spell out.</param>
        /// <param name="grammaticalCase">The case the whole numeral stands in.</param>
        /// <param name="gender">The gender of the noun being counted, which only one and two reflect.</param>
        /// <param name="isAnimate">True when the counted noun is masculine animate; otherwise, false.</param>
        /// <returns>The words of the spelled-out numeral, in surface order.</returns>
        public IReadOnlyList<string> ComposeParts(
            long value,
            Case grammaticalCase,
            Gender? gender = null,
            bool? isAnimate = null,
            CompoundVariant variant = CompoundVariant.Preferred)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Číslovku lze vypsat jen pro nezáporné číslo.");
            }

            // Zero decomposes into no groups at all, so it is named directly rather than built up.
            if (value == 0)
            {
                return [RenderBare(0, grammaticalCase, gender, isAnimate, inCompound: false)];
            }

            // The contracted variant is one word, unit before ten, and declines by the two-form rule that
            // every numeral from five up follows: pětadvacet, pětadvaceti.
            if (variant == CompoundVariant.Contracted && TryRenderContracted(value, grammaticalCase, out var contracted))
            {
                return [contracted];
            }

            var groups = Decompose(value);
            var words = new List<string>();

            foreach (var group in groups)
            {
                words.AddRange(RenderGroup(group, grammaticalCase, gender, isAnimate, groups.Count > 1, variant));
            }

            return words;
        }

        /// <summary>
        /// Spells the value out as a declined ordinal numeral: dvacátý pátý, tisící devítistý padesátý šestý.
        /// </summary>
        /// <param name="value">The value to spell out.</param>
        /// <param name="grammaticalCase">The case the whole numeral stands in.</param>
        /// <param name="gender">The gender of the noun being modified.</param>
        /// <param name="isAnimate">True when the modified noun is masculine animate; otherwise, false.</param>
        /// <returns>The spelled-out ordinal, its parts separated by single spaces.</returns>
        /// <remarks>
        /// Every part declines separately, per ÚJČ id=791. The parts come from the ordinal lexicon by exact
        /// value, so a value needing a component the lexicon does not have fails loudly rather than
        /// inventing a form.
        /// </remarks>
        public string ComposeOrdinal(long value, Case grammaticalCase, Gender? gender = null, bool? isAnimate = null)
            => ComposeOrdinal(value, grammaticalCase, CompoundVariant.Preferred, gender, isAnimate);

        /// <summary>
        /// Spells the value out as a declined ordinal numeral under a chosen treatment of compounds.
        /// </summary>
        /// <param name="value">The value to spell out.</param>
        /// <param name="grammaticalCase">The case the whole numeral stands in.</param>
        /// <param name="variant">Which treatment of a compound to follow.</param>
        /// <param name="gender">The gender of the noun being modified.</param>
        /// <param name="isAnimate">True when the modified noun is masculine animate; otherwise, false.</param>
        /// <returns>The spelled-out ordinal, its parts separated by single spaces.</returns>
        /// <remarks>
        /// An ordinal does not agree with anything it counts, so only <see cref="CompoundVariant.Contracted"/>
        /// changes anything here — it writes twenty-one to ninety-nine as one word, pětadvacátý, and prefixes
        /// a single hundred to it, stopadesátý. Anything it does not reach falls back to the spaced form.
        /// </remarks>
        public string ComposeOrdinal(
            long value,
            Case grammaticalCase,
            CompoundVariant variant,
            Gender? gender = null,
            bool? isAnimate = null)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Řadovou číslovku lze vypsat jen pro kladné číslo.");
            }

            if (variant == CompoundVariant.Contracted
                && TryRenderContractedOrdinal(value, grammaticalCase, gender, isAnimate, out var contracted))
            {
                return contracted;
            }

            var words = DecomposeOrdinalValues(value)
                .Select(component => LookupOrdinal(component, value))
                .Select(lemma => DeclineOrdinal(lemma, grammaticalCase, gender, isAnimate));

            return string.Join(' ', words);
        }

        /// <summary>
        /// Splits the value into order-of-magnitude groups, largest scale first.
        /// </summary>
        /// <param name="value">The value to split.</param>
        /// <returns>The groups making up the value.</returns>
        public IReadOnlyList<NumeralGroup> Decompose(long value)
        {
            var groups = new List<NumeralGroup>();
            var remaining = value;

            foreach (var (scaleValue, scaleLemma) in Scales)
            {
                var multiplier = remaining / scaleValue;

                if (multiplier == 0)
                {
                    continue;
                }

                groups.Add(new NumeralGroup(multiplier, scaleLemma));
                remaining %= scaleValue;
            }

            // Eleven through nineteen are single lexemes, not a ten plus a unit — so they are never split.
            if (remaining is >= 11 and <= 19)
            {
                groups.Add(new NumeralGroup(remaining, null));

                return groups;
            }

            var tens = remaining / 10 * 10;

            if (tens > 0)
            {
                groups.Add(new NumeralGroup(tens, null));
            }

            var units = remaining % 10;

            if (units > 0)
            {
                groups.Add(new NumeralGroup(units, null));
            }

            return groups;
        }

        /// <summary>
        /// Gets the agreement a spelled-out value imposes on the noun it counts.
        /// </summary>
        /// <param name="value">The value being spelled out.</param>
        /// <returns>The agreement the counted noun follows.</returns>
        /// <remarks>
        /// For compounds from twenty-one up ÚJČ id=792 admits three variants. This returns the genitive
        /// plural — dvacet jedna žáků bylo — which the příručka calls the more natural one.
        /// </remarks>
        public CardinalAgreement GetAgreement(long value) => GetAgreement(value, CompoundVariant.Preferred);

        /// <summary>
        /// Gets the agreement a spelled-out value imposes on the noun it counts, under a chosen variant.
        /// </summary>
        /// <param name="value">The value being spelled out.</param>
        /// <param name="variant">Which of the three standard treatments of a compound to follow.</param>
        /// <returns>The agreement the counted noun follows.</returns>
        public CardinalAgreement GetAgreement(long value, CompoundVariant variant)
        {
            // Variant A lets the last member govern, so twenty-four behaves like four: dvacet čtyři žáci byli.
            if (variant == CompoundVariant.AgreeingLastMember && value is > 20 and < 100 && value % 10 is >= 1 and <= 4)
            {
                return _numeralService.GetAgreementForValue(value % 10);
            }

            return _numeralService.GetAgreementForValue(value);
        }

        /// <summary>
        /// Renders the distributive construction: po jednom, po dvou, po pěti.
        /// </summary>
        /// <param name="value">The value distributed.</param>
        /// <returns>The preposition and the numeral in the locative.</returns>
        /// <remarks>
        /// Distributives are a construction rather than a class of their own — po plus the locative — which
        /// is why they are produced here and not stored in the lexicon.
        /// </remarks>
        public string ComposeDistributive(long value)
            => $"po {Compose(value, Case.Locative)}";

        /// <summary>
        /// Spells a fraction out in words: tři čtvrtiny, pět osmin, jedna polovina.
        /// </summary>
        /// <param name="numerator">The numerator, which counts the parts.</param>
        /// <param name="denominator">The denominator, which names them.</param>
        /// <param name="grammaticalCase">The case the whole fraction stands in.</param>
        /// <returns>The spelled-out fraction.</returns>
        /// <remarks>
        /// The denominator is an ordinary feminine noun and the numerator counts it like any other, so the
        /// same agreement governs here as anywhere: one half, two thirds, but five eighths in the genitive
        /// plural — jedna polovina, dvě třetiny, pět osmin.
        /// </remarks>
        public string ComposeFraction(long numerator, long denominator, Case grammaticalCase = Case.Nominative)
        {
            if (numerator <= 0 || denominator <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numerator), "Zlomek lze vypsat jen pro kladné čitatele a jmenovatele.");
            }

            var lemma = LookupTable(NumeralType.Fractional).TryGetValue(denominator, out var found)
                ? found
                : throw new InvalidOperationException($"Pro jmenovatele {denominator} není ve slovníku dílová číslovka.");

            return $"{Compose(numerator, grammaticalCase, Gender.Feminine, false)} {RenderCounted(lemma, numerator, grammaticalCase)}";
        }

        /// <summary>
        /// Spells a decimal out in words: jedna celá pět desetin, tři celé čtrnáct setin.
        /// </summary>
        /// <param name="value">The value to spell out.</param>
        /// <param name="grammaticalCase">The case the whole numeral stands in.</param>
        /// <returns>The spelled-out decimal.</returns>
        /// <remarks>
        /// Read as a whole part, the word celá standing for the unit, and a fraction named by however many
        /// decimal places there are — tenths, hundredths, thousandths. Both celá and the fraction are counted
        /// nouns, so both follow the numeral in front of them: jedna celá, but pět celých.
        /// </remarks>
        public string ComposeDecimal(decimal value, Case grammaticalCase = Case.Nominative)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Desetinné číslo lze vypsat jen pro nezápornou hodnotu.");
            }

            var whole = (long)decimal.Truncate(value);
            var places = BitConverter.GetBytes(decimal.GetBits(value)[3])[2];

            if (places == 0)
            {
                return Compose(whole, grammaticalCase);
            }

            var scale = (long)Math.Pow(10, places);
            var numerator = (long)((value - whole) * scale);

            var fractionLemma = LookupTable(NumeralType.Fractional).TryGetValue(scale, out var found)
                ? found
                : throw new InvalidOperationException(
                    $"Pro {places} desetinných míst není ve slovníku dílová číslovka pro {scale}.");

            var wholeWords = Compose(whole, grammaticalCase, Gender.Feminine, false);
            var unitWord = RenderUnitWord(whole, grammaticalCase);
            var fractionWords = Compose(numerator, grammaticalCase, Gender.Feminine, false);
            var fractionNoun = RenderCounted(fractionLemma, numerator, grammaticalCase);

            return $"{wholeWords} {unitWord} {fractionWords} {fractionNoun}";
        }

        // ── Privátní metody ────────────────────────────────────────────

        // A numeral-counted noun, shaped by whatever the numerator in front of it imposes.
        private string RenderCounted(string lemma, long counter, Case phraseCase)
        {
            var agreement = _numeralService.GetAgreementForValue(counter);
            var (nounCase, number) = _numeralService.ResolveCountedForm(agreement, phraseCase);

            return _numeralService.TryGetForm(lemma, nounCase, Gender.Feminine, number, false, null) ?? lemma;
        }

        // The word celá names the unit a decimal is a whole number of, and is counted like any noun:
        // nula celá, jedna celá, dvě celé, pět celých. Zero is the exception — nula celá, not nula celých —
        // because there the numeral names the unit itself rather than a quantity of them.
        private string RenderUnitWord(long whole, Case phraseCase)
        {
            var agreement = whole == 0
                ? CardinalAgreement.AgreesSingular
                : _numeralService.GetAgreementForValue(whole);

            var (unitCase, number) = _numeralService.ResolveCountedForm(agreement, phraseCase);

            return _adjectiveService.GetForm(new CzechWordRequest
            {
                Lemma = "celý",
                WordCategory = WordCategory.Adjective,
                Pattern = "mladý",
                Gender = Gender.Feminine,
                Number = number,
                Case = unitCase,
                IsAnimate = false,
                Degree = Degree.Positive,
            }).Form;
        }

        // A contracted ordinal ends in the same member the spaced one does, so the last member is declined
        // and the invariant prefix put in front of it: pěta + dvacátého gives pětadvacátého. That is also
        // why no contracted ordinal needs a lexicon entry of its own.
        private bool TryRenderContractedOrdinal(
            long value,
            Case grammaticalCase,
            Gender? gender,
            bool? isAnimate,
            out string contracted)
        {
            contracted = string.Empty;

            var prefix = string.Empty;
            var remaining = value;

            // A single hundred prefixes directly, with no joining vowel: stopadesátý.
            if (remaining is >= 101 and <= 199)
            {
                prefix += "sto";
                remaining -= 100;
            }

            if (remaining is >= 21 and <= 99 && remaining % 10 != 0)
            {
                if (!_cardinalsByValue.TryGetValue(remaining % 10, out var unit))
                {
                    return false;
                }

                prefix += unit + "a";
                remaining = remaining / 10 * 10;
            }

            if (prefix.Length == 0 || !_ordinalsByValue.TryGetValue(remaining, out var baseLemma))
            {
                return false;
            }

            contracted = prefix + DeclineOrdinal(baseLemma, grammaticalCase, gender, isAnimate);

            return true;
        }

        private string DeclineOrdinal(string lemma, Case grammaticalCase, Gender? gender, bool? isAnimate)
            => _numeralService.TryGetForm(
                lemma, grammaticalCase, gender ?? Gender.Masculine, Number.Singular, isAnimate ?? true, null) ?? lemma;

        // Unit before ten, joined by a: jedenadvacet, čtyřiadvacet, pětadvacet. Only the twenty-one to
        // ninety-nine range contracts, and the result declines by the same rule as pět — hence no lexicon
        // entry for any of them.
        private bool TryRenderContracted(long value, Case grammaticalCase, out string contracted)
        {
            contracted = string.Empty;

            if (value is < 21 or > 99 || value % 10 == 0)
            {
                return false;
            }

            if (!_cardinalsByValue.TryGetValue(value % 10, out var unit)
                || !_cardinalsByValue.TryGetValue(value / 10 * 10, out var ten))
            {
                return false;
            }

            var lemma = unit + "a" + ten;

            contracted = grammaticalCase is Case.Nominative or Case.Accusative or Case.Vocative
                ? lemma
                : lemma + "i";

            return true;
        }

        private IReadOnlyList<string> RenderGroup(
            NumeralGroup group,
            Case grammaticalCase,
            Gender? gender,
            bool? isAnimate,
            bool inCompound,
            CompoundVariant variant)
        {
            if (group.ScaleLemma is null)
            {
                // Variant A keeps the last member agreeing, so it is not treated as frozen: dvacet jeden žák.
                var frozen = inCompound && variant != CompoundVariant.AgreeingLastMember;

                return [RenderBare(group.Multiplier, grammaticalCase, gender, isAnimate, frozen)];
            }

            var scaleData = _numeralService.GetData(group.ScaleLemma)
                ?? throw new InvalidOperationException($"Číslovka '{group.ScaleLemma}' není ve slovníku.");

            // A single hundred, thousand or million is named on its own — sto, not jedno sto.
            if (group.Multiplier == 1)
            {
                return [RenderScale(group.ScaleLemma, scaleData, 1, grammaticalCase)];
            }

            // The multiplier agrees with the scale word, which is a noun of its own gender: dvě stě but
            // dva tisíce, because sto is neuter and tisíc masculine.
            var multiplier = _numeralService.TryGetForm(
                _cardinalsByValue.TryGetValue(group.Multiplier, out var lemma)
                    ? lemma
                    : throw new InvalidOperationException($"Pro hodnotu {group.Multiplier} není ve slovníku číslovka."),
                grammaticalCase,
                scaleData.Gender,
                Number.Plural,
                scaleData.IsAnimate,
                null)!;

            return [multiplier, RenderScale(group.ScaleLemma, scaleData, group.Multiplier, grammaticalCase)];
        }

        // Sto is the irregular one: two hundred is dvě stě, three and four hundred are sta, five hundred
        // upwards is set. Those forms are data, keyed by multiplier class. The other scale words are plain
        // nouns and take the number their own multiplier imposes on them, exactly as a counted noun would.
        private string RenderScale(string scaleLemma, NumeralData scaleData, long multiplier, Case grammaticalCase)
        {
            if (scaleData.Composite is not null && multiplier > 1)
            {
                var key = multiplier switch
                {
                    2 => "2",
                    3 or 4 => "3",
                    _ => "5"
                };

                if (scaleData.Composite.Forms.TryGetValue(key, out var forms)
                    && forms.TryGetValue(grammaticalCase, out var composite))
                {
                    return composite;
                }
            }

            if (multiplier == 1)
            {
                return _numeralService.TryGetForm(scaleLemma, grammaticalCase, scaleData.Gender, Number.Singular, scaleData.IsAnimate, null)!;
            }

            // Five thousand upwards puts the scale word in the genitive plural in the direct cases and
            // agrees with it elsewhere: pět tisíc, but s pěti tisíci.
            var isDirect = grammaticalCase is Case.Nominative or Case.Accusative or Case.Vocative;
            var scaleCase = multiplier >= 5 && isDirect ? Case.Genitive : grammaticalCase;

            return _numeralService.TryGetForm(scaleLemma, scaleCase, scaleData.Gender, Number.Plural, scaleData.IsAnimate, null)!;
        }

        // One and two are the only cardinals that reflect the gender of what they count. As the last member
        // of a compound they lose even that and freeze — ÚJČ id=792 has dvacet jedna žáků, not
        // dvacet jedna žák, because the compound governs the genitive plural instead of agreeing.
        private string RenderBare(long value, Case grammaticalCase, Gender? gender, bool? isAnimate, bool inCompound)
        {
            var lemma = _cardinalsByValue.TryGetValue(value, out var found)
                ? found
                : throw new InvalidOperationException($"Pro hodnotu {value} není ve slovníku číslovka.");

            if (inCompound && value == 1)
            {
                return "jedna";
            }

            var effectiveGender = inCompound ? Gender.Masculine : gender;
            var effectiveAnimate = inCompound ? false : isAnimate;
            var number = value < 2 ? Number.Singular : Number.Plural;

            return _numeralService.TryGetForm(lemma, grammaticalCase, effectiveGender, number, effectiveAnimate, null) ?? lemma;
        }

        private static IEnumerable<long> DecomposeOrdinalValues(long value)
        {
            var thousands = value / 1000 * 1000;

            if (thousands > 0)
            {
                yield return thousands;
            }

            var remaining = value % 1000;
            var hundreds = remaining / 100 * 100;

            if (hundreds > 0)
            {
                yield return hundreds;
            }

            remaining %= 100;

            if (remaining is >= 11 and <= 19)
            {
                yield return remaining;

                yield break;
            }

            var tens = remaining / 10 * 10;

            if (tens > 0)
            {
                yield return tens;
            }

            var units = remaining % 10;

            if (units > 0)
            {
                yield return units;
            }
        }

        private string LookupOrdinal(long component, long whole)
            => _ordinalsByValue.TryGetValue(component, out var lemma)
                ? lemma
                : throw new InvalidOperationException(
                    $"Pro hodnotu {whole} chybí ve slovníku řadová číslovka pro složku {component}.");
    }
}

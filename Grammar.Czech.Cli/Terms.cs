using Grammar.Core.Enums;
using Grammar.Czech.Models;
using System.Globalization;
using System.Text;

namespace Grammar.Czech.Cli
{
    /// <summary>
    /// Translates between the grammatical vocabulary a Czech user types and the library's enums.
    /// </summary>
    /// <remarks>
    /// One table for both directions, so a value the tool prints is a value the tool accepts. Input is
    /// matched without diacritics and without case, because a command line is where diacritics are the
    /// most awkward to type — <c>--pad akuzativ</c>, <c>--pad akuzativ</c> and <c>--pad 4</c> are the
    /// same request. Output always carries them.
    /// </remarks>
    public static class Terms
    {
        private static readonly Dictionary<Case, string> Cases = new()
        {
            [Case.Nominative] = "nominativ",
            [Case.Genitive] = "genitiv",
            [Case.Dative] = "dativ",
            [Case.Accusative] = "akuzativ",
            [Case.Vocative] = "vokativ",
            [Case.Locative] = "lokál",
            [Case.Instrumental] = "instrumentál",
        };

        private static readonly Dictionary<Number, string> Numbers = new()
        {
            [Number.Singular] = "jednotné",
            [Number.Plural] = "množné",
        };

        private static readonly Dictionary<Gender, string> Genders = new()
        {
            [Gender.Masculine] = "mužský",
            [Gender.Feminine] = "ženský",
            [Gender.Neuter] = "střední",
        };

        private static readonly Dictionary<Person, string> Persons = new()
        {
            [Person.First] = "1.",
            [Person.Second] = "2.",
            [Person.Third] = "3.",
        };

        private static readonly Dictionary<Tense, string> Tenses = new()
        {
            [Tense.Past] = "minulý",
            [Tense.Present] = "přítomný",
            [Tense.Future] = "budoucí",
        };

        private static readonly Dictionary<Modus, string> Moods = new()
        {
            [Modus.Indicative] = "oznamovací",
            [Modus.Imperative] = "rozkazovací",
            [Modus.Conditional] = "podmiňovací",
            [Modus.Conjunctive] = "spojovací",
        };

        private static readonly Dictionary<Voice, string> Voices = new()
        {
            [Voice.Active] = "činný",
            [Voice.Passive] = "trpný",
        };

        private static readonly Dictionary<VerbAspect, string> Aspects = new()
        {
            [VerbAspect.Perfective] = "dokonavý",
            [VerbAspect.Imperfective] = "nedokonavý",
        };

        private static readonly Dictionary<InformationStatus, string> Statuses = new()
        {
            [InformationStatus.Given] = "dané",
            [InformationStatus.New] = "nové",
            [InformationStatus.Contrastive] = "kontrastivní",
            [InformationStatus.Interrogative] = "tázací",
        };

        private static readonly Dictionary<SentenceType, string> SentenceTypes = new()
        {
            [SentenceType.Declarative] = "oznamovací",
            [SentenceType.Interrogative] = "tázací",
        };

        private static readonly Dictionary<WordCategory, string> Categories = new()
        {
            [WordCategory.Noun] = "podstatné jméno",
            [WordCategory.Adjective] = "přídavné jméno",
            [WordCategory.Pronoun] = "zájmeno",
            [WordCategory.Numerale] = "číslovka",
            [WordCategory.Verb] = "sloveso",
            [WordCategory.Adverb] = "příslovce",
            [WordCategory.Preposition] = "předložka",
            [WordCategory.Conjunction] = "spojka",
            [WordCategory.Particle] = "částice",
            [WordCategory.Interjection] = "citoslovce",
        };

        /// <summary>
        /// Names a word class in Czech.
        /// </summary>
        /// <param name="value">The word class.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(WordCategory value) =>
            Categories.GetValueOrDefault(value, value.ToString());

        // Funktory se nepřekládají — ACT a PAT jsou termíny FGD a v tomhle tvaru je nese i slovník.
        // Český popis je vedle nich, aby tabulka byla čitelná i pro toho, kdo zkratky nezná.
        private static readonly Dictionary<FgdFunctor, string> Functors = new()
        {
            [FgdFunctor.ACT] = "konatel",
            [FgdFunctor.PAT] = "patiens",
            [FgdFunctor.ADDR] = "adresát",
            [FgdFunctor.ORIG] = "původ",
            [FgdFunctor.EFF] = "výsledek",
            [FgdFunctor.DIR1] = "odkud",
            [FgdFunctor.DIR2] = "kudy",
            [FgdFunctor.DIR3] = "kam",
            [FgdFunctor.LOC] = "kde",
            [FgdFunctor.MANN] = "jak",
            [FgdFunctor.MEANS] = "čím",
            [FgdFunctor.BEN] = "pro koho",
            [FgdFunctor.CAUS] = "proč",
            [FgdFunctor.AIM] = "za jakým účelem",
            [FgdFunctor.TWHEN] = "kdy",
            [FgdFunctor.DIFF] = "rozdíl",
            [FgdFunctor.OBST] = "překážka",
            [FgdFunctor.INTT] = "záměr",
            [FgdFunctor.MAT] = "z čeho",
            [FgdFunctor.THL] = "jak dlouho",
            [FgdFunctor.EXT] = "rozsah",
            [FgdFunctor.CRIT] = "podle čeho",
            [FgdFunctor.ACMP] = "s kým",
            [FgdFunctor.COMPL] = "doplněk",
        };

        // Zkratky a číselné pády jsou synonyma navíc; do výpisu se nikdy nedostanou, jen se přijímají.
        private static readonly Dictionary<string, Case> CaseAliases = Aliases(Cases, new()
        {
            ["1"] = Case.Nominative,
            ["2"] = Case.Genitive,
            ["3"] = Case.Dative,
            ["4"] = Case.Accusative,
            ["5"] = Case.Vocative,
            ["6"] = Case.Locative,
            ["7"] = Case.Instrumental,
            ["lokativ"] = Case.Locative,
        });

        private static readonly Dictionary<string, Number> NumberAliases = Aliases(Numbers, new()
        {
            ["j"] = Number.Singular,
            ["sg"] = Number.Singular,
            ["mn"] = Number.Plural,
            ["pl"] = Number.Plural,
        });

        private static readonly Dictionary<string, Gender> GenderAliases = Aliases(Genders, new()
        {
            ["m"] = Gender.Masculine,
            ["z"] = Gender.Feminine,
            ["s"] = Gender.Neuter,
        });

        private static readonly Dictionary<string, Person> PersonAliases = Aliases(Persons, new()
        {
            ["1"] = Person.First,
            ["2"] = Person.Second,
            ["3"] = Person.Third,
        });

        private static readonly Dictionary<string, Tense> TenseAliases = Aliases(Tenses, []);
        private static readonly Dictionary<string, Modus> MoodAliases = Aliases(Moods, []);
        private static readonly Dictionary<string, Voice> VoiceAliases = Aliases(Voices, []);
        private static readonly Dictionary<string, VerbAspect> AspectAliases = Aliases(Aspects, []);
        private static readonly Dictionary<string, SentenceType> SentenceTypeAliases = Aliases(SentenceTypes, []);

        private static readonly Dictionary<string, InformationStatus> StatusAliases = Aliases(Statuses, new()
        {
            ["tema"] = InformationStatus.Given,
            ["rema"] = InformationStatus.New,
        });

        // Jednoslovné zkratky vedle školních názvů: 'podstatné jméno' se na příkazové řádce píše špatně
        // a v dialogu ještě hůř, protože mezera odděluje cíl od vlastnosti.
        private static readonly Dictionary<string, WordCategory> WordCategoryAliases = Aliases(Categories, new()
        {
            ["substantivum"] = WordCategory.Noun,
            ["podstatne"] = WordCategory.Noun,
            ["adjektivum"] = WordCategory.Adjective,
            ["pridavne"] = WordCategory.Adjective,
            ["pronomen"] = WordCategory.Pronoun,
            ["zajmeno"] = WordCategory.Pronoun,
            ["numerale"] = WordCategory.Numerale,
            ["cislovka"] = WordCategory.Numerale,
            ["verbum"] = WordCategory.Verb,
            ["adverbium"] = WordCategory.Adverb,
            ["prislovce"] = WordCategory.Adverb,
            ["prepozice"] = WordCategory.Preposition,
            ["predlozka"] = WordCategory.Preposition,
            ["konjunkce"] = WordCategory.Conjunction,
            ["spojka"] = WordCategory.Conjunction,
            ["partikule"] = WordCategory.Particle,
            ["castice"] = WordCategory.Particle,
            ["interjekce"] = WordCategory.Interjection,
            ["citoslovce"] = WordCategory.Interjection,
        });

        private static readonly Dictionary<Degree, string> Degrees = new()
        {
            [Degree.Positive] = "první",
            [Degree.Comparative] = "druhý",
            [Degree.Superlative] = "třetí",
        };

        private static readonly Dictionary<string, Degree> DegreeAliases = Aliases(Degrees, new()
        {
            ["1"] = Degree.Positive,
            ["2"] = Degree.Comparative,
            ["3"] = Degree.Superlative,
            ["pozitiv"] = Degree.Positive,
            ["komparativ"] = Degree.Comparative,
            ["superlativ"] = Degree.Superlative,
        });

        /// <summary>
        /// Gets the Czech name of the case.
        /// </summary>
        /// <param name="value">The case to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(Case value) => Cases[value];

        /// <summary>
        /// Gets the Czech name of the number.
        /// </summary>
        /// <param name="value">The number to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(Number value) => Numbers[value];

        /// <summary>
        /// Gets the Czech name of the gender.
        /// </summary>
        /// <param name="value">The gender to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(Gender value) => Genders[value];

        /// <summary>
        /// Gets the Czech name of the person.
        /// </summary>
        /// <param name="value">The person to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(Person value) => Persons[value];

        /// <summary>
        /// Gets the Czech name of the tense.
        /// </summary>
        /// <param name="value">The tense to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(Tense value) => Tenses[value];

        /// <summary>
        /// Gets the Czech name of the mood.
        /// </summary>
        /// <param name="value">The mood to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(Modus value) => Moods[value];

        /// <summary>
        /// Gets the Czech name of the voice.
        /// </summary>
        /// <param name="value">The voice to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(Voice value) => Voices[value];

        /// <summary>
        /// Gets the Czech name of the aspect.
        /// </summary>
        /// <param name="value">The aspect to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(VerbAspect value) => Aspects[value];

        /// <summary>
        /// Gets the Czech name of the communicative status.
        /// </summary>
        /// <param name="value">The status to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(InformationStatus value) => Statuses[value];

        /// <summary>
        /// Gets the Czech name of the sentence type.
        /// </summary>
        /// <param name="value">The sentence type to name.</param>
        /// <returns>The Czech name.</returns>
        public static string Name(SentenceType value) => SentenceTypes[value];

        /// <summary>
        /// Gets the Czech gloss of the FGD functor.
        /// </summary>
        /// <param name="value">The functor to gloss.</param>
        /// <returns>The Czech gloss.</returns>
        public static string Gloss(FgdFunctor value) => Functors.GetValueOrDefault(value, string.Empty);

        /// <summary>
        /// Parses a case written as a Czech name, an abbreviation or a number.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The case.</returns>
        /// <exception cref="CliException">Thrown when the text names no case.</exception>
        public static Case ParseCase(string text) => Parse(CaseAliases, text, "pád");

        /// <summary>
        /// Parses a grammatical number written as a Czech name or an abbreviation.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The number.</returns>
        /// <exception cref="CliException">Thrown when the text names no number.</exception>
        public static Number ParseNumber(string text) => Parse(NumberAliases, text, "číslo");

        /// <summary>
        /// Parses a gender written as a Czech name or an abbreviation.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The gender.</returns>
        /// <exception cref="CliException">Thrown when the text names no gender.</exception>
        public static Gender ParseGender(string text) => Parse(GenderAliases, text, "rod");

        /// <summary>
        /// Parses a word class written in Czech.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The word class.</returns>
        /// <exception cref="CliException">The text names no word class.</exception>
        public static WordCategory ParseWordCategory(string text) =>
            Parse(WordCategoryAliases, text, "slovní druh");

        /// <summary>
        /// Parses a degree of comparison written in Czech.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The degree.</returns>
        /// <exception cref="CliException">The text names no degree.</exception>
        public static Degree ParseDegree(string text) => Parse(DegreeAliases, text, "stupeň");

        /// <summary>
        /// Parses a person written as a Czech name or a number.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The person.</returns>
        /// <exception cref="CliException">Thrown when the text names no person.</exception>
        public static Person ParsePerson(string text) => Parse(PersonAliases, text, "osoba");

        /// <summary>
        /// Parses a tense written as a Czech name.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The tense.</returns>
        /// <exception cref="CliException">Thrown when the text names no tense.</exception>
        public static Tense ParseTense(string text) => Parse(TenseAliases, text, "čas");

        /// <summary>
        /// Parses a mood written as a Czech name.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The mood.</returns>
        /// <exception cref="CliException">Thrown when the text names no mood.</exception>
        public static Modus ParseMood(string text) => Parse(MoodAliases, text, "způsob");

        /// <summary>
        /// Parses a voice written as a Czech name.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The voice.</returns>
        /// <exception cref="CliException">Thrown when the text names no voice.</exception>
        public static Voice ParseVoice(string text) => Parse(VoiceAliases, text, "slovesný rod");

        /// <summary>
        /// Parses an aspect written as a Czech name.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The aspect.</returns>
        /// <exception cref="CliException">Thrown when the text names no aspect.</exception>
        public static VerbAspect ParseAspect(string text) => Parse(AspectAliases, text, "vid");

        /// <summary>
        /// Parses a communicative status written as a Czech name.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The status.</returns>
        /// <exception cref="CliException">Thrown when the text names no status.</exception>
        public static InformationStatus ParseStatus(string text) => Parse(StatusAliases, text, "aktuální členění");

        /// <summary>
        /// Parses a sentence type written as a Czech name.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The sentence type.</returns>
        /// <exception cref="CliException">Thrown when the text names no sentence type.</exception>
        public static SentenceType ParseSentenceType(string text) => Parse(SentenceTypeAliases, text, "druh věty");

        /// <summary>
        /// Parses an FGD functor written as its abbreviation.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The functor.</returns>
        /// <exception cref="CliException">Thrown when the text names no functor.</exception>
        public static FgdFunctor ParseFunctor(string text)
        {
            if (Enum.TryParse<FgdFunctor>(text.Trim(), ignoreCase: true, out var functor)
                && Enum.IsDefined(functor))
            {
                return functor;
            }

            throw new CliException(
                $"Funktor '{text}' neznám. Na výběr je: {string.Join(", ", Functors.Keys)}.");
        }

        /// <summary>
        /// Parses a truth value written as a Czech word.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>The truth value.</returns>
        /// <exception cref="CliException">Thrown when the text is neither yes nor no.</exception>
        public static bool ParseYesNo(string text) => Key(text) switch
        {
            "ano" or "a" or "1" or "true" => true,
            "ne" or "n" or "0" or "false" => false,
            _ => throw new CliException($"Z '{text}' nepoznám ano ani ne."),
        };

        private static string NameOf<TEnum>(TEnum value) where TEnum : struct, Enum => value switch
        {
            Case item => Cases[item],
            Number item => Numbers[item],
            Gender item => Genders[item],
            Tense item => Tenses[item],
            Modus item => Moods[item],
            Voice item => Voices[item],
            VerbAspect item => Aspects[item],
            InformationStatus item => Statuses[item],
            SentenceType item => SentenceTypes[item],
            _ => string.Empty,
        };

        private static TValue Parse<TValue>(Dictionary<string, TValue> aliases, string text, string category)
            where TValue : struct, Enum
        {
            if (aliases.TryGetValue(Key(text), out var value))
            {
                return value;
            }

            // Enumy se přijímají taky, protože přesně ty jména vidí ten, kdo čte kód knihovny.
            if (Enum.TryParse<TValue>(text.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            {
                return parsed;
            }

            throw new CliException(
                $"'{text}' není {category}. Na výběr je: {string.Join(", ", Distinct(aliases))}.");
        }

        private static IEnumerable<string> Distinct<TValue>(Dictionary<string, TValue> aliases)
            where TValue : struct, Enum => Enum.GetValues<TValue>().Select(value => NameOf(value));

        private static Dictionary<string, TValue> Aliases<TValue>(
            Dictionary<TValue, string> names, Dictionary<string, TValue> extra)
            where TValue : struct, Enum
        {
            var aliases = new Dictionary<string, TValue>(StringComparer.Ordinal);

            foreach (var (value, name) in names)
            {
                aliases[Key(name)] = value;
            }

            foreach (var (alias, value) in extra)
            {
                aliases[Key(alias)] = value;
            }

            return aliases;
        }

        /// <summary>
        /// Strips diacritics, case and a trailing full stop, so that what a user manages to type matches
        /// what the tool would have printed.
        /// </summary>
        /// <param name="text">The text to normalize.</param>
        /// <returns>The normalized text.</returns>
        public static string Plain(string text) => Key(text);

        /// <summary>
        /// Compares words the way a person addressing one means them: without diacritics and without
        /// case.
        /// </summary>
        /// <remarks>
        /// What a switch names and what the dictionary spells need not match character for character.
        /// Someone who wrote <c>ucitel</c> on the command line and got <c>učitel</c> back in the table
        /// must be able to correct it with either spelling, and the number is not always to hand.
        /// </remarks>
        public static IEqualityComparer<string> LemmaComparer { get; } = new FoldingComparer();

        private sealed class FoldingComparer : IEqualityComparer<string>
        {
            public bool Equals(string? left, string? right) =>
                left is null || right is null ? left is null && right is null : Key(left) == Key(right);

            public int GetHashCode(string value) => Key(value).GetHashCode(StringComparison.Ordinal);
        }

        // Klíč bez diakritiky a bez velikosti písmen: na příkazové řádce je 'ženský' to nejhůř psatelné
        // slovo z celé nabídky a 'zensky' má znamenat totéž.
        private static string Key(string text)
        {
            var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC).TrimEnd('.');
        }
    }
}

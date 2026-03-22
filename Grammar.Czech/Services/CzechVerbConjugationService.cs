using Grammar.Core.Enums;
using Grammar.Core.Enums.PhonologicalFeatures;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Konjugační služba pro česká slovesa.
    /// Zodpovídá výhradně za výběr správných koncovek a sestavení tvaru —
    /// analýza kmenů je delegována na <see cref="IVerbStructureResolver{TWord}"/>.
    /// </summary>
    public class CzechVerbConjugationService : IVerbInflectionService<CzechWordRequest>
    {
        private readonly IVerbDataProvider dataProvider;
        private readonly IVerbStructureResolver<CzechWordRequest> verbStructureResolver;
        private readonly ICzechParticleService czechParticleService;
        private readonly ICzechPrefixService czechPrefixService;
        private readonly IPhonemeRegistry phonemeRegistry;

        /// <summary>
        /// Mapování <see cref="VerbClass"/> na klíče generických vzorů v patterns.json.
        /// Používá se pouze pokud volající předá <see cref="CzechWordRequest.VerbClass"/>
        /// místo explicitního <see cref="CzechWordRequest.Pattern"/>.
        /// </summary>
        private readonly Dictionary<VerbClass, string> verbClassMap = new()
        {
            { VerbClass.Class1, "trida1" },
            { VerbClass.Class2, "trida2" },
            { VerbClass.Class3, "trida3" },
            { VerbClass.Class4, "trida4" },
            { VerbClass.Class5, "trida5" },
        };

        public CzechVerbConjugationService(
            IVerbDataProvider dataProvider,
            IVerbStructureResolver<CzechWordRequest> verbStructureResolver,
            ICzechParticleService czechParticleService,
            ICzechPrefixService czechPrefixService,
            IPhonemeRegistry phonemeRegistry)
        {
            this.dataProvider          = dataProvider;
            this.verbStructureResolver = verbStructureResolver;
            this.czechParticleService  = czechParticleService;
            this.czechPrefixService    = czechPrefixService;
            this.phonemeRegistry       = phonemeRegistry;
        }

        #region Public API

        /// <summary>
        /// Vrátí základní morfologický tvar slovesa (bez auxiliárních sloves,
        /// reflexiv a kondicionálních partikulí). Složené fráze sestavuje
        /// <see cref="CzechVerbPhraseBuilderService"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Pasivní imperativ, pasivum od "být", nebo chybějící data v paradigmatu.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Neznámý vzor, nepodporovaný rod nebo čas.
        /// </exception>
        public WordForm GetBasicForm(CzechWordRequest word)
        {
            if (word.Modus == Modus.Imperative && word.Voice == Voice.Passive)
                throw new InvalidOperationException(
                    "Passive form does not exist in imperative.");

            if (word.Voice == Voice.Passive
                && word.Lemma.Equals("být", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Impossible to create passive for verb 'být'.");

            // VerbClass hint → přepiš Pattern na klíč třídy, pokud pattern ještě není znám
            if (word.VerbClass.HasValue
                && (word.Pattern == null || !dataProvider.GetPatterns().ContainsKey(word.Pattern.ToLower())))
            {
                if (!verbClassMap.TryGetValue(word.VerbClass.Value, out var mappedKey))
                    throw new InvalidOperationException(
                        $"Unknown verb class {word.VerbClass.Value}.");
                word.Pattern = mappedKey;
            }

            var pattern    = ResolvePattern(word);
            var verbStruct = verbStructureResolver.AnalyzeVerbStructure(word);
            var numberKey  = word.Number == Number.Singular ? "singular" : "plural";

            if (word.Tense == null && word.Modus == Modus.Indicative)
                throw new ArgumentException("Tense must be specified for indicative mood.");

            // Perfektivní sloveso v přítomném čase → tvar je fakticky budoucí
            var effectiveTense =
                word.Tense == Tense.Present && pattern.Aspect == VerbAspect.Perfective
                    ? Tense.Future
                    : word.Tense;

            return (word.Voice, word.Modus, effectiveTense) switch
            {
                (Voice.Passive, _, _)     => BuildPassiveForm(word, pattern, verbStruct, numberKey),
                (_, Modus.Conditional, _) => BuildConditionalForm(pattern, verbStruct, numberKey, word.Gender),
                (_, Modus.Imperative, _)  => BuildImperativeForm(word, verbStruct),
                (_, _, Tense.Past)        => BuildPastForm(pattern, verbStruct, numberKey, word.Gender),
                _                         => BuildPresentFutureForm(word, pattern, verbStruct, numberKey, effectiveTense),
            };
        }

        /// <summary>
        /// Odhadne slovesný vid z lemmatu.
        /// Není implementováno — čeká na valenční slovník (<see cref="IValencyProvider"/>).
        /// </summary>
        public VerbAspect GuessVerbAspect(string lemma)
        {
            throw new NotImplementedException(
                "GuessVerbAspect is not implemented. Unblocked by valency dictionary.");
        }

        /// <summary>
        /// Odhadne slovesnou třídu z infinitivní přípony.
        /// Vrátí <c>null</c> pokud je lemma přímo klíčem v datech (žádný odhad není potřeba),
        /// nebo pokud jde o trida1 (neprediktabilní kmeny — musí být v irregulars.json).
        /// </summary>
        public VerbClass? GuessVerbClass(string lemma)
        {
            // Lemma je přímo pattern v datech — žádný odhad není potřeba
            if (dataProvider.GetPatterns().ContainsKey(lemma.ToLower())
                || dataProvider.GetIrregulars().ContainsKey(lemma.ToLower()))
                return null;

            // Pořadí je kritické — "ovat" musí předcházet "at"
            if (lemma.EndsWith("ovat")) return VerbClass.Class3;
            if (lemma.EndsWith("it") || lemma.EndsWith("ít")
             || lemma.EndsWith("et") || lemma.EndsWith("ět")) return VerbClass.Class4;
            if (lemma.EndsWith("at") || lemma.EndsWith("át")) return VerbClass.Class5;
            if (lemma.EndsWith("nout")) return VerbClass.Class2;

            // trida1 (nést, brát, péct...) nelze spolehlivě odvodit z infinitivu
            return null;
        }

        /// <summary>
        /// Odhadne vzor slovesa. Fallback: vrátí lemma jako klíč.
        /// </summary>
        public string GuessVerbPattern(string lemma) => lemma.ToLower();

        #endregion Public API

        #region Pattern Resolution

        /// <summary>
        /// Vrátí <see cref="VerbPattern"/> pro daný request.
        /// <para>
        /// Regulars (trida1–trida5) → přímá shoda v patterns.json, žádný Merge.<br/>
        /// Named patterns (nese, dělá, být...) → irregulars.json,
        /// případně Merge s bázovým vzorem přes <c>inheritsFrom</c>.
        /// </para>
        /// </summary>
        private VerbPattern ResolvePattern(CzechWordRequest word)
        {
            var key = word.Pattern!.ToLower();

            if (dataProvider.GetPatterns().TryGetValue(key, out var regular))
                return regular;

            if (dataProvider.GetIrregulars().TryGetValue(key, out var irregular))
            {
                if (!string.IsNullOrEmpty(irregular.InheritsFrom)
                    && dataProvider.GetPatterns().TryGetValue(
                        irregular.InheritsFrom.ToLower(), out var basePattern))
                    return Merge(basePattern, irregular);

                return irregular;
            }

            throw new NotSupportedException($"Verb pattern '{word.Pattern}' not found.");
        }

        /// <summary>
        /// Přepíše vlastnosti bázového vzoru hodnotami z nepravidelného vzoru
        /// (pokud nejsou null). Zachovává InheritsFrom, přepisuje Aspect a vše ostatní.
        /// </summary>
        private static VerbPattern Merge(VerbPattern @base, VerbPattern irregular) =>
            @base with
            {
                Stem              = irregular.Stem              ?? @base.Stem,
                FutureStem        = irregular.FutureStem        ?? @base.FutureStem,
                PresentStem       = irregular.PresentStem       ?? @base.PresentStem,
                PastStem          = irregular.PastStem          ?? @base.PastStem,
                PassiveStem       = irregular.PassiveStem       ?? @base.PassiveStem,
                ImperativeStem    = irregular.ImperativeStem    ?? @base.ImperativeStem,
                Aspect            = irregular.Aspect,
                Present           = irregular.Present           ?? @base.Present,
                Future            = irregular.Future            ?? @base.Future,
                PastParticiple    = irregular.PastParticiple    ?? @base.PastParticiple,
                PassiveParticiple = irregular.PassiveParticiple ?? @base.PassiveParticiple,
            };

        #endregion Pattern Resolution

        #region Build Methods

        /// <summary>
        /// Sestaví tvar pasivního participia (trpný rod).
        /// Kmen dostane z <see cref="VerbStructure.PassiveStem"/>,
        /// fallback na <see cref="VerbStructure.PastStem"/>.
        /// </summary>
        private WordForm BuildPassiveForm(
            CzechWordRequest word, VerbPattern pattern,
            VerbStructure verbStruct, string numberKey)
        {
            var genderKey = ResolveGenderKey(word.Gender);
            var stem      = verbStruct.PassiveStem ?? verbStruct.PastStem;

            // Heuristické úpravy kmene pasiva.
            // Jde o lexikální výjimky, které nelze pokrýt obecným pravidlem přes phoneme registry.
            if (stem.EndsWith("sk"))
                stem = stem[..^2] + "ště";  // tisk  → tišt(ěn)
            else if (stem.EndsWith("s"))
                stem = stem[..^1] + "š";    // pros  → proš(en)
            else if (word.Lemma == "kvést")
                stem = "květ";              // kvést → květ(en) — nepravidelné

            if (!pattern.PassiveParticiple.TryGetValue(genderKey, out var passiveDict)
                || !passiveDict.TryGetValue(numberKey, out var ending))
                throw new InvalidOperationException(
                    $"Passive participle ending not found for {genderKey} {numberKey}.");

            return new WordForm(PrefixedForm(verbStruct.Prefix, stem, ending));
        }

        /// <summary>
        /// Sestaví tvar kondicionálu (podmiňovací způsob).
        /// Používá minulý kmen a participiální koncovku; auxiliární sloveso
        /// přidá <see cref="CzechVerbPhraseBuilderService"/>.
        /// </summary>
        private static WordForm BuildConditionalForm(
            VerbPattern pattern, VerbStructure verbStruct,
            string numberKey, Gender? gender)
        {
            var genderKey = ResolveGenderKey(gender);

            if (!pattern.PastParticiple.TryGetValue(genderKey, out var participleDict)
                || !participleDict.TryGetValue(numberKey, out var ending))
                throw new InvalidOperationException(
                    $"Past participle not found for {genderKey} {numberKey}.");

            return new WordForm(PrefixedForm(verbStruct.Prefix, verbStruct.PastStem, ending));
        }

        /// <summary>
        /// Sestaví imperativní tvar.
        /// </summary>
        /// <remarks>
        /// Výběr kmene: <see cref="VerbStructure.ImperativeStem"/> (explicitní z dat nebo
        /// odvozený resolverem), fallback na <see cref="VerbStructure.PresentStem"/>.
        ///
        /// Pravidla sufixu (CzechEncy IMPERATIV):
        /// <list type="bullet">
        ///   <item>Dvě souhlásky na konci kmene → 2sg: +i; pl: +ěme/+ěte pokud DTN, jinak +me/+te.</item>
        ///   <item>Jedna souhláska (před ní vokál) → 2sg: +Ø; pl: +me/+te.</item>
        ///   <item>Vokál na konci kmene (kupuj, dělej) → 2sg: +Ø; pl: +me/+te.</item>
        /// </list>
        /// </remarks>
        private WordForm BuildImperativeForm(CzechWordRequest word, VerbStructure verbStruct)
        {
            var prefix   = verbStruct.Prefix ?? string.Empty;
            var baseStem = verbStruct.ImperativeStem ?? verbStruct.PresentStem;

            var twoConsonants       = MorphologyHelper.EndsWithTwoConsonants(baseStem);
            var finalConsonantIsDtn = twoConsonants && IsDtn(baseStem[^1]);

            string result = (word.Number, word.Person) switch
            {
                (Number.Singular, Person.Second) when twoConsonants       => baseStem + "i",
                (Number.Singular, Person.Second)                          => baseStem,
                (Number.Plural,   Person.First)  when finalConsonantIsDtn => baseStem + "ěme",
                (Number.Plural,   Person.First)                           => baseStem + "me",
                (Number.Plural,   Person.Second) when finalConsonantIsDtn => baseStem + "ěte",
                (Number.Plural,   Person.Second)                          => baseStem + "te",
                _ => throw new InvalidOperationException(
                    "Imperative exists only for 2nd person (sg/pl) and 1st person plural.")
            };

            if (word.HasReflexive.HasValue && word.HasReflexive.Value)
                result += $" {czechParticleService.GetReflexive(word.Case == Case.Dative)}";

            return new WordForm($"{prefix}{result}!");
        }

        /// <summary>
        /// Sestaví tvar minulého času (l-ové participium).
        /// </summary>
        private static WordForm BuildPastForm(
            VerbPattern pattern, VerbStructure verbStruct,
            string numberKey, Gender? gender)
        {
            var genderKey = ResolveGenderKey(gender);

            if (!pattern.PastParticiple.TryGetValue(genderKey, out var participleDict)
                || !participleDict.TryGetValue(numberKey, out var ending))
                throw new InvalidOperationException(
                    $"Past participle not found for {genderKey} {numberKey}.");

            return new WordForm(PrefixedForm(verbStruct.Prefix, verbStruct.PastStem, ending));
        }

        /// <summary>
        /// Sestaví tvar přítomného nebo budoucího času (syntetický).
        /// <para>
        /// <b>Výběr kmene pro budoucí čas:</b>
        /// "být" má v JSON explicitní <c>futureStem</c> ("bud") → budu/budeš/bude...
        /// Čteme ho přímo z <see cref="VerbPattern"/>, kde přirozeně patří.
        /// Pro všechna ostatní slovesa je <c>pattern.FutureStem</c> null
        /// → fallback na <see cref="VerbStructure.PresentStem"/>.
        /// </para>
        /// </summary>
        private static WordForm BuildPresentFutureForm(
            CzechWordRequest word, VerbPattern pattern,
            VerbStructure verbStruct, string numberKey, Tense? effectiveTense)
        {
            // Opisné budoucí pro imperfektivní slovesa — vrátíme infinitiv,
            // frázi "bude dělat" sestaví CzechVerbPhraseBuilderService
            if (effectiveTense == Tense.Future
                && word.Aspect == VerbAspect.Imperfective
                && !word.Lemma.Equals("být", StringComparison.OrdinalIgnoreCase))
                return new WordForm(word.Lemma);

            var tenseForms = effectiveTense switch
            {
                Tense.Present => pattern.Present,
                Tense.Future  => pattern.Future ?? pattern.Present,
                _ => throw new InvalidOperationException(
                    $"Unsupported tense: {effectiveTense}.")
            };

            var personKey = word.Person.ToString();
            var pDict = numberKey switch
            {
                "singular" => tenseForms.Singular?.ToDictionary(),
                "plural"   => tenseForms.Plural?.ToDictionary(),
                _ => null
            };

            if (pDict == null || !pDict.TryGetValue(personKey, out var ending))
                throw new InvalidOperationException(
                    $"Ending not found for {effectiveTense} {numberKey} {personKey}.");

            // FutureStem z patternu (platí pouze pro "být": "bud" → budu/budeš/bude...).
            // Pro všechna ostatní slovesa je null → fallback na PresentStem.
            var stem = (effectiveTense == Tense.Future && pattern.FutureStem != null)
                ? pattern.FutureStem
                : verbStruct.PresentStem;

            return new WordForm(PrefixedForm(verbStruct.Prefix, stem, ending));
        }

        #endregion Build Methods

        #region Helpers

        /// <summary>
        /// Returns <c>true</c> when the character is a DTN consonant (Alveolar Nasal or Plosive:
        /// d, t, n) — the context in which a following suffix <c>e</c> must be written as <c>ě</c>.
        /// Decision is delegated to <see cref="IPhonemeRegistry"/> per project principle #2.
        /// </summary>
        private bool IsDtn(char c)
        {
            var phoneme = phonemeRegistry.Get(c);
            return phoneme is { Place: ArticulationPlace.Alveolar }
                && phoneme.Manner is ArticulationManner.Nasal or ArticulationManner.Plosive;
        }

        /// <summary>
        /// Převede <see cref="Gender"/> na řetězcový klíč pro indexování do paradigmat.
        /// </summary>
        private static string ResolveGenderKey(Gender? gender) => gender switch
        {
            Gender.Masculine => "masculine",
            Gender.Feminine  => "feminine",
            Gender.Neuter    => "neuter",
            _ => throw new NotSupportedException($"Unsupported gender: {gender}.")
        };

        /// <summary>
        /// Sestaví finální tvar: (prefix +) stem + ending.
        /// Centralizuje opakující se prefixovou logiku ze všech build metod.
        /// </summary>
        private static string PrefixedForm(string? prefix, string stem, string ending)
        {
            var full = string.IsNullOrEmpty(prefix) ? stem : prefix + stem;
            return MorphologyHelper.ApplyFormEnding(full, ending);
        }

        #endregion Helpers
    }
}

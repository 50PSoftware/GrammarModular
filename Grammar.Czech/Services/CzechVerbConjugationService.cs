using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    public class CzechVerbConjugationService : IVerbInflectionService<CzechWordRequest>
    {
        private readonly CzechPrefixService prefixService;
        private readonly CzechParticleService particleService;

        private readonly Dictionary<VerbClass, string> verbClassMap = new Dictionary<VerbClass, string>()
        {
            { VerbClass.Class1, "trida1" },
            { VerbClass.Class2, "trida2" },
            { VerbClass.Class3, "trida3" },
            { VerbClass.Class4, "trida4" },
            { VerbClass.Class5, "trida5" },
        };

        private readonly IVerbDataProvider dataProvider;
        private readonly IWordStructureResolver<CzechWordRequest> wordStructureResolver;
        private readonly ICzechParticleService czechParticleService;
        private readonly ICzechPrefixService czechPrefixService;

        private VerbPattern Merge(VerbPattern @base, VerbPattern irregular) =>
        @base with
        {
            Stem = irregular.Stem ?? @base.Stem,
            FutureStem = irregular.FutureStem ?? @base.FutureStem,
            PresentStem = irregular.PresentStem ?? @base.PresentStem,
            PastStem = irregular.PastStem ?? @base.PastStem,
            PassiveStem = irregular.PassiveStem ?? @base.PassiveStem,
            Aspect = irregular.Aspect,
            Present = irregular.Present ?? @base.Present,
            Future = irregular.Future ?? @base.Future,
            PastParticiple = irregular.PastParticiple ?? @base.PastParticiple,
            PassiveParticiple = irregular.PassiveParticiple ?? @base.PassiveParticiple
        };

        private WordForm GetImperativeForm(CzechWordRequest word)
        {
            var lemma = word.Lemma;
            var wordStructure = wordStructureResolver.AnalyzeStructure(word);
            var (prefix, stem) = (wordStructure.Prefix, wordStructure.Root + wordStructure.DerivationSuffix);

            var hasIrregular = dataProvider.GetIrregulars().TryGetValue(word.Pattern!.ToLower(), out var irregularPattern);
            if (hasIrregular && !string.IsNullOrEmpty(irregularPattern!.ImperativeStem))
            {
                var baseImperative = irregularPattern.ImperativeStem;

                string result = word.Number switch
                {
                    Number.Singular when word.Person == Person.Second => baseImperative,
                    Number.Plural when word.Person == Person.First => baseImperative + "me",
                    Number.Plural when word.Person == Person.Second => baseImperative + "te",
                    _ => throw new InvalidOperationException("Imperative exists only for 2nd person (sg/pl) and 1st person plural.")
                };

                if (word.HasReflexive.HasValue && word.HasReflexive.Value)
                {
                    result += $" {particleService.GetReflexive(word.Case == Case.Dative)}";
                }

                return new WordForm($"{prefix}{result}");
            }

            if (hasIrregular)
            {
                var baseStem = irregularPattern!.ImperativeStem ?? irregularPattern!.Stem ?? word.Lemma;

                string baseImperative = baseStem;
                if (word.Number == Number.Singular && word.Person == Person.Second)
                {
                    if (MorphologyHelper.EndsWithTwoConsonants(baseStem))
                    {
                        baseImperative += "i";
                    }
                }
                else if (word.Number == Number.Plural && word.Person == Person.First)
                {
                    baseImperative += "me";
                }
                else if (word.Number == Number.Plural && word.Person == Person.Second)
                {
                    baseImperative += "te";
                }
                else
                {
                    throw new InvalidOperationException("Imperative exists only for 2nd person (sg/pl) and 1st person plural.");
                }

                if (word.HasReflexive.HasValue && word.HasReflexive.Value)
                {
                    baseImperative += $" {particleService.GetReflexive(word.Case == Case.Dative)}";
                }

                return new WordForm($"{prefix}{baseImperative}");
            }

            // Heuristická pravidla:
            var imperativeForm = (word.Number, word.Person) switch
            {
                (Number.Singular, Person.Second) => $"{stem}",
                (Number.Plural, Person.First) => $"{stem}me",
                (Number.Plural, Person.Second) => $"{stem}te",
                _ => throw new InvalidOperationException("Imperative exists only for 2nd person (sg/pl) and 1st person plural.")
            };

            if (word.HasReflexive.HasValue && word.HasReflexive.Value)
            {
                imperativeForm += $" {particleService.GetReflexive(word.Case == Case.Dative)}";
            }

            return new WordForm($"{prefix}{imperativeForm}!");
        }

        private WordForm GetPassiveForm(CzechWordRequest word, VerbPattern pattern, string genderKey, string numberKey, string stem)
        {
            var lemma = word.Lemma;
            var prefix = wordStructureResolver.AnalyzeStructure(word).Prefix;

            string ending = string.Empty;
            if (pattern.PassiveParticiple.TryGetValue(genderKey, out var participleDict) && participleDict.TryGetValue(numberKey, out var participle))
            {
                ending = participle;
            }

            // Heuristická úprava kmene:
            if (stem.EndsWith("sk"))
            {
                stem = stem[..^2] + "ště"; // tisk → tištěn
            }
            else if (stem.EndsWith("s"))
            {
                stem = stem[..^1] + "š"; // pros → proš
            }
            else if (lemma == "kvést")
            {
                stem = "květ"; // nepravidelné
            }

            return new WordForm(prefix + MorphologyHelper.ApplyFormEnding(stem, ending));
        }

        public CzechVerbConjugationService(IVerbDataProvider dataProvider, IWordStructureResolver<CzechWordRequest> wordStructureResolver, ICzechParticleService czechParticleService, ICzechPrefixService czechPrefixService)
        {
            this.dataProvider = dataProvider;
            this.wordStructureResolver = wordStructureResolver;
            this.czechParticleService = czechParticleService;
            this.czechPrefixService = czechPrefixService;
        }

        public WordForm GetBasicForm(CzechWordRequest word)
        {
            if (word.Modus == Modus.Imperative && word.Voice == Voice.Passive)
            {
                throw new InvalidOperationException("Passive form does not exist in imperative.");
            }

            if (word.Voice == Voice.Passive && word.Lemma.Equals("být", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Impossible to create passive for verb 'být'.");
            }

            if (word.VerbClass.HasValue && !dataProvider.GetPatterns().ContainsKey(word.Pattern.ToLower()))
            {
                if (!verbClassMap.TryGetValue(word.VerbClass.Value, out var mappedPattern))
                    throw new InvalidOperationException($"Unknown verb class {word.VerbClass.Value}");

                word.Pattern = mappedPattern;
            }

            if (!dataProvider.GetPatterns().TryGetValue(word.Pattern.ToLower(), out var pattern))
            {
                if (dataProvider.GetIrregulars().TryGetValue(word.Pattern.ToLower(), out var irregularPattern))
                {
                    if (!string.IsNullOrEmpty(irregularPattern.InheritsFrom) && dataProvider.GetPatterns().TryGetValue(irregularPattern.InheritsFrom.ToLower(), out var inheritedPattern))
                    {
                        pattern = Merge(inheritedPattern, irregularPattern);
                    }
                    else
                    {
                        pattern = irregularPattern;
                    }
                }
            }

            var wordStructure = wordStructureResolver.AnalyzeStructure(word);
            var numberKey = word.Number == Number.Singular ? "singular" : "plural";
            var personKey = word.Person.ToString();

            if (word.Tense == Tense.Present && pattern.Aspect == VerbAspect.Perfective)
            {
                word.Tense = Tense.Future;
            }

            var tenseKey = word.Tense.ToString().ToLower();

            string? GetGenderKey(CzechWordRequest word, out string? genderKey)
            {
                genderKey = word.Gender switch
                {
                    Gender.Masculine => "masculine",
                    Gender.Feminine => "feminine",
                    Gender.Neuter => "neuter",
                    _ => throw new NotSupportedException("Unsupported gender.")
                };

                return genderKey;
            }

            if (word.Voice == Voice.Passive)
            {
                GetGenderKey(word, out var genderKey);

                return GetPassiveForm(word, pattern!, genderKey!, numberKey, pattern.PassiveStem ?? pattern.Stem);
            }

            if (word.Modus == Modus.Conditional)
            {
                GetGenderKey(word, out var genderKey);
                var participle = pattern.PastParticiple[genderKey][numberKey];

                var (prefix, stem) = (wordStructure.Prefix, pattern.PastStem ?? pattern.Stem);

                if (!string.IsNullOrEmpty(prefix))
                {
                    stem = prefix + stem;
                }

                return new WordForm(MorphologyHelper.ApplyFormEnding(stem, participle));
            }

            if (word.Modus == Modus.Imperative)
            {
                return GetImperativeForm(word);
            }

            if (word.Tense == Tense.Past)
            {
                if (!pattern.PastParticiple.TryGetValue(GetGenderKey(word, out var genderKey), out var participleDict) ||
                    !participleDict.TryGetValue(numberKey, out var participle))
                {
                    throw new InvalidOperationException($"Past participle not found for {genderKey} {numberKey}.");
                }

                var (prefix, stem) = (wordStructure.Prefix, wordStructure.Root + wordStructure.DerivationSuffix);

                if (!string.IsNullOrEmpty(prefix))
                {
                    stem = prefix + stem;
                }

                return new WordForm(MorphologyHelper.ApplyFormEnding(stem, participle));
            }
            else
            {
                var tenseForms = word.Tense switch
                {
                    Tense.Present => pattern.Present,
                    Tense.Future => pattern.Future ?? pattern.Present,
                    _ => throw new InvalidOperationException("Unsuported tense.")
                };

                // Person dictionary
                Dictionary<string, string>? pDict = numberKey switch
                {
                    "singular" => tenseForms.Singular?.ToDictionary(),
                    "plural" => tenseForms.Plural?.ToDictionary(),
                    _ => null
                };

                if (pDict == null || !pDict.TryGetValue(personKey.ToString(), out var ending))
                    throw new InvalidOperationException($"Ending not found for {tenseKey} {numberKey} person {personKey}");

                var (prefix, stem) = (wordStructure.Prefix, wordStructure.Root + wordStructure.DerivationSuffix);

                if (!string.IsNullOrEmpty(prefix))
                {
                    stem = prefix + stem;
                }

                if (word.Tense == Tense.Future && word.Lemma != "být")
                {
                    if (word.Aspect == VerbAspect.Perfective)
                    {
                        return new WordForm(MorphologyHelper.ApplyFormEnding(stem, ending));
                    }
                    else
                    {
                        return new WordForm(word.Lemma);
                    }
                }
                else
                {
                    return new WordForm(MorphologyHelper.ApplyFormEnding(stem, ending));
                }
            }
        }

        public VerbAspect GuessVerbAspect(string lemma)
        {
            throw new NotImplementedException("Guessing aspect is not implemented yet. Consider implementing a heuristic based on prefixes or known patterns.");
            return prefixService.HasPerfectivePrefix(lemma)
                ? VerbAspect.Perfective
                : VerbAspect.Imperfective;
        }

        public VerbClass? GuessVerbClass(string lemma)
        {
            if (dataProvider.GetPatterns().ContainsKey(lemma.ToLower()))
            {
                return null;
            }

            if (lemma.EndsWith("ovat"))
                return VerbClass.Class3;

            if (lemma.EndsWith("it") || lemma.EndsWith("et") || lemma.EndsWith("ět"))
                return VerbClass.Class2;

            if (lemma.EndsWith("at") || lemma.EndsWith("át"))
                return VerbClass.Class1;

            // volitelně další:
            if (lemma.EndsWith("nout"))
                return VerbClass.Class4;

            if (lemma.EndsWith("ít"))
                return VerbClass.Class5;

            return null;
        }

        public string GuessVerbPattern(string lemma)
        {
            // Můžeš to doplnit vlastním seznamem známých vzorů
            return lemma.ToLower(); // fallback: pattern = infinitiv
        }
    }
}

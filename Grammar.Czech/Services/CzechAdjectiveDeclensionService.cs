using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Builds Czech adjective forms, including comparative and superlative stems, from adjective patterns and orthographic rules.
    /// </summary>
    public class CzechAdjectiveDeclensionService : IInflectionService<CzechWordRequest>
    {
        private readonly IAdjectiveDataProvider dataProvider;
        private readonly IWordStructureResolver<CzechWordRequest> wordStructureResolver;
        private readonly ICzechPhonologyService czechPhonologyService;
        private readonly ICzechOrthographyService orthographyService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechAdjectiveDeclensionService"/> type.
        /// </summary>
        public CzechAdjectiveDeclensionService(IAdjectiveDataProvider dataProvider, IWordStructureResolver<CzechWordRequest> wordStructureResolver, ICzechPhonologyService czechPhonologyService, ICzechOrthographyService orthographyService)
        {
            this.dataProvider = dataProvider;
            this.wordStructureResolver = wordStructureResolver;
            this.czechPhonologyService = czechPhonologyService;
            this.orthographyService = orthographyService;
        }

        /// <summary>
        /// Builds the requested inflected form.
        /// </summary>
        /// <param name="word">The Czech word request containing the lemma and requested grammatical categories.</param>
        /// <returns>The generated adjective form.</returns>
        public WordForm GetForm(CzechWordRequest word)
        {
            if (word.Degree != null && word.Degree != Degree.Positive)
            {
                word.Pattern = "jarní";
            }

            if (!dataProvider.GetPatterns().TryGetValue(word.Pattern.ToLower(), out var pattern))
            {
                throw new NotSupportedException($"Adjective pattern '{word.Pattern}' not found.");
            }

            var numberKey = word.Number == Number.Singular ? "singular" : "plural";
            var genderKey = word.Gender switch
            {
                Gender.Masculine when word.IsAnimate == true => "MasculineAnimate",
                Gender.Masculine when word.IsAnimate == false => "MasculineInanimate",
                _ => word.Gender.ToString()
            };

            var caseIndex = (int)word.Case - 1;

            if (!pattern.Endings.TryGetValue(numberKey, out var genderDict) ||
                !genderDict.TryGetValue(genderKey, out var endings))
            {
                throw new InvalidOperationException($"Ending not found for {numberKey} {genderKey}.");
            }

            if (caseIndex < 0 || caseIndex >= endings.Count)
            {
                throw new IndexOutOfRangeException("Invalid case index for adjective.");
            }

            var wordSructure = wordStructureResolver.AnalyzeStructure(word);
            var (prefix, stem) = (wordSructure.Prefix, wordSructure.Root + wordSructure.DerivationSuffix);

            if (word.Degree != null && word.Degree != Degree.Positive)
            {
                if (_supletives.TryGetValue(word.Lemma, out var supletiveStem))
                {
                    stem = supletiveStem + "š";
                }
                else
                {
                    stem = BuildComparativeStem(stem);
                }
            }

            var supPrefix = (word.Degree == Degree.Superlative) ? "nej" : string.Empty;
            return new WordForm(supPrefix + prefix + MorphologyHelper.ApplyFormEnding(stem, endings[caseIndex]));
        }

        /// <summary>
        /// Chooses the adjective pattern key from the lemma ending.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The adjective pattern key inferred from the lemma.</returns>
        public string GuessAdjectivePattern(string lemma)
        {
            if (lemma.EndsWith("ý") || lemma.EndsWith("á") || lemma.EndsWith("é") || lemma.EndsWith("í"))
            {
                return lemma.EndsWith("í") ? "jarní" : "mladý";
            }

            return "mladý"; // fallback na tvrdý vzor
        }

        private string BuildComparativeStem(string baseStem)
        {
            if (baseStem.EndsWith("k") || baseStem.EndsWith("ch") || baseStem.EndsWith("h"))
            {
                var softened = czechPhonologyService.ApplySoftening(baseStem, PalatalizationContext.First);
                return baseStem.EndsWith("k") ? softened : softened + "š";
            }

            if (baseStem.EndsWith("n"))
            {
                //return czechPhonologyService.ApplySoftConsonantBeforeE(baseStem) + "jš";

                var ortographicVowel = orthographyService.ApplyJotationOrthography("-e").TrimStart('-');

                return baseStem + ortographicVowel + "jš";
            }

            var group1 = new[] { "d", "t", "s", "z", "r" };
            if (group1.Any(s => baseStem.EndsWith(s) && !MorphologyHelper.EndsWithTwoConsonants(baseStem)))
            {
                return baseStem + "š";
            }

            if (baseStem.EndsWith("l"))
            {
                return baseStem + "ejš";
            }

            return baseStem + "ějš";
        }

        private static readonly Dictionary<string, string> _supletives = new()
        {
            { "dobrý", "lep" },
            { "malý", "men" },
            { "velký", "vět" },
            { "zlý", "hor" },
            { "špatný", "hor" },
            { "dlouhý", "del" },
        };
    }
}

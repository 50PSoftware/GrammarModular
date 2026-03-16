using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    public class CzechWordStructureResolver : IWordStructureResolver<CzechWordRequest>
    {
        private readonly IVerbDataProvider verbDataProvider;
        private readonly INounDataProvider nounDataProvider;
        private readonly CzechPrefixService prefixService;
        private readonly IPhonologyService<CzechWordRequest> phonologyService;

        private readonly Dictionary<WordCategory, Func<CzechWordRequest, WordStructure>> analyzers;

        public CzechWordStructureResolver(IVerbDataProvider verbDataProvider, INounDataProvider nounDataProvider, CzechPrefixService prefixService, IPhonologyService<CzechWordRequest> phonologyService)
        {
            this.verbDataProvider = verbDataProvider;
            this.nounDataProvider = nounDataProvider;
            this.prefixService = prefixService;
            this.phonologyService = phonologyService;

            analyzers = new Dictionary<WordCategory, Func<CzechWordRequest, WordStructure>>
            {
                { WordCategory.Noun, AnalyzeNoun },
                { WordCategory.Verb, AnalyzeVerb },
                { WordCategory.Adjective, AnalyzeAdjective },
                { WordCategory.Pronoun, AnalyzePronoun }
            };
        }

        public WordStructure AnalyzeStructure(CzechWordRequest wordRequest)
        {
            ValidateRequest(wordRequest);

            if (!analyzers.TryGetValue(wordRequest.WordCategory, out var analyzer))
            {
                throw new NotSupportedException($"Word category '{wordRequest.WordCategory}' is not supported.");
            }

            var result = analyzer(wordRequest);

            return result;
        }

        private void ValidateRequest(CzechWordRequest wordRequest)
        {
            if (string.IsNullOrEmpty(wordRequest.Lemma))
            {
                throw new ArgumentException("Lemma cannot be null or empty.", nameof(wordRequest));
            }

            if (string.IsNullOrEmpty(wordRequest.Pattern))
            {
                throw new ArgumentException("Pattern cannost be empty.", nameof(wordRequest));
            }
        }

        #region Noun

        private WordStructure AnalyzeNoun(CzechWordRequest wordRequest)
        {
            var lemma = wordRequest.Lemma;
            var pattern = wordRequest.Pattern;

            var root = ExtractNounRoot(lemma, wordRequest);

            var derivationSuffix = DetectNounDerivationSuffix(lemma, pattern!, wordRequest);

            if (!string.IsNullOrEmpty(derivationSuffix))
            {
                if (root.EndsWith(derivationSuffix))
                {
                    root = root[..^derivationSuffix.Length];
                }
            }

            return new WordStructure
            {
                Root = root,
                DerivationSuffix = derivationSuffix
            };
        }

        private string ExtractNounRoot(string lemma, CzechWordRequest request)
        {
            // vzor píseň
            if (lemma.EndsWith("eň"))
                return lemma[..^2] + "n";

            string root;

            if (lemma.Length > 1 && !MorphologyHelper.IsConsonant(lemma[^1]))
            {
                // Feminine and neuter nouns often end with a vowel, so we can try removing it to find the root
                root = lemma[..^1];
            }
            else
            {
                // For masculine nouns, the lemma often ends with a consonant, so we can return it as is
                root = lemma;
            }

            var hasMobileVowel = MorphologyHelper.EndsWithVowelConsonantVowelConsonant(lemma);

            if (hasMobileVowel && !(request.Case == Case.Nominative && request.Number == Number.Singular))
            {
                root = phonologyService.RemoveMobileVowel(root, true);
            }
            else if (request.Gender == Gender.Masculine &&
                !(request.Case == Case.Nominative &&
                request.Number == Number.Singular))
            {
                var hasMobileVowelIrregular = nounDataProvider.GetIrregulars().TryGetValue(request.Lemma.ToLower(), out var irregular) && irregular.HasMobileVowel;
                root = phonologyService.RemoveMobileVowel(root, hasMobileVowelIrregular);
            }

            return root;
        }

        private string? DetectNounDerivationSuffix(string lemma, string pattern, CzechWordRequest request)
        {
            if (pattern == "žena" && lemma.EndsWith("ka") && lemma.Length > 2)
            {
                return "k";
            }

            return null;
        }

        #endregion Noun

        #region Verb

        private WordStructure AnalyzeVerb(CzechWordRequest wordRequest)
        {
            var prefix = ExtractPrefix(wordRequest.Lemma);
            var lemmaWithoutPrefix = prefix != null ? wordRequest.Lemma.Substring(prefix.Length) : wordRequest.Lemma;

            var stem = GetVerbStem(wordRequest, lemmaWithoutPrefix);

            return new WordStructure
            {
                Prefix = prefix,
                Root = stem
            };
        }

        private string? ExtractPrefix(string lemma)
        {
            return prefixService.FindPerfectivePrefix(lemma);
        }

        private string GetVerbStem(CzechWordRequest wordRequest, string lemmaWithoutPrefix)
        {
            var pattern = wordRequest.Pattern;

            if (verbDataProvider.GetIrregulars().TryGetValue(pattern.ToLower(), out var irregular))
            {
                return GetStemFromPattern(wordRequest, irregular);
            }

            if (verbDataProvider.GetPatterns().TryGetValue(pattern!.ToLower(), out var regularPattern))
            {
                return GetStemFromPattern(wordRequest, regularPattern);
            }

            return ExtractVerbStemHeuristic(lemmaWithoutPrefix);
        }

        private string GetStemFromPattern(CzechWordRequest request, VerbPattern pattern)
        {
            return request.Tense switch
            {
                Tense.Present when pattern.Aspect == VerbAspect.Imperfective => pattern.PresentStem ?? pattern.Stem,
                Tense.Future when pattern.Aspect == VerbAspect.Perfective => pattern.FutureStem ?? pattern.PresentStem ?? pattern.Stem,
                Tense.Future when pattern.Aspect == VerbAspect.Imperfective && request.Lemma == "být" => pattern.FutureStem ?? throw new InvalidOperationException("Missing Future stem for 'být'"),
                Tense.Past => pattern.PastStem ?? pattern.Stem,
                _ => pattern.Stem
            };
        }

        private string ExtractVerbStemHeuristic(string lemmaWithoutPrefix)
        {
            // Heruistiky s -t
            if (lemmaWithoutPrefix.EndsWith("at") || lemmaWithoutPrefix.EndsWith("át"))
            {
                return lemmaWithoutPrefix[..^2]; // dělat
            }

            if (lemmaWithoutPrefix.EndsWith("it") || lemmaWithoutPrefix.EndsWith("ít"))
            {
                return lemmaWithoutPrefix[..^2]; // prosit
            }

            if (lemmaWithoutPrefix.EndsWith("et") || lemmaWithoutPrefix.EndsWith("ět"))
            {
                return lemmaWithoutPrefix[..^2]; // trpět, sázet
            }

            if (lemmaWithoutPrefix.EndsWith("ovat"))
            {
                return lemmaWithoutPrefix[..^4]; // kupovat
            }

            if (lemmaWithoutPrefix.EndsWith("nout"))
            {
                return lemmaWithoutPrefix[..^4]; // tisknout
            }

            return lemmaWithoutPrefix;
        }

        #endregion Verb

        #region Adjective

        private WordStructure AnalyzeAdjective(CzechWordRequest wordRequest)
        {
            var lemma = wordRequest.Lemma;
            var root = ExtractAdjectiveRoot(lemma);
            return new WordStructure
            {
                Root = root
            };
        }

        private string ExtractAdjectiveRoot(string lemma)
        {
            if (lemma.EndsWith("ý") || lemma.EndsWith("á") || lemma.EndsWith("é") ||
                lemma.EndsWith("í"))
            {
                return lemma[..^1];
            }

            if (lemma.EndsWith("ův") || lemma.EndsWith("in"))
            {
                return lemma[..^2];
            }

            return lemma;
        }

        #endregion Adjective

        #region Pronoun

        private WordStructure AnalyzePronoun(CzechWordRequest wordRequest)
        {
            return new WordStructure
            {
                Root = wordRequest.Lemma
            };
        }

        #endregion Pronoun
    }
}

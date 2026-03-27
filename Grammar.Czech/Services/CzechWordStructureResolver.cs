using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Analyses the internal morphological structure of a Czech word request,
    /// resolving the inflectional root, derivational suffix, and verb stems needed for inflection.
    /// </summary>
    public class CzechWordStructureResolver : IWordStructureResolver<CzechWordRequest>, IVerbStructureResolver<CzechWordRequest>
    {
        private readonly IVerbDataProvider verbDataProvider;
        private readonly INounDataProvider nounDataProvider;
        private readonly CzechPrefixService prefixService;
        private readonly IPhonologyService<CzechWordRequest> phonologyService;
        private readonly ICzechRootProvider rootProvider;

        private readonly Dictionary<WordCategory, Func<CzechWordRequest, WordStructure>> analyzers;

        /// <summary>
        /// Initializes a new instance of <see cref="CzechWordStructureResolver"/>.
        /// </summary>
        public CzechWordStructureResolver(
            IVerbDataProvider verbDataProvider,
            INounDataProvider nounDataProvider,
            CzechPrefixService prefixService,
            IPhonologyService<CzechWordRequest> phonologyService,
            ICzechRootProvider rootProvider)
        {
            this.verbDataProvider = verbDataProvider;
            this.nounDataProvider = nounDataProvider;
            this.prefixService = prefixService;
            this.phonologyService = phonologyService;
            this.rootProvider = rootProvider;

            analyzers = new Dictionary<WordCategory, Func<CzechWordRequest, WordStructure>>
            {
                { WordCategory.Noun,      AnalyzeNoun      },
                { WordCategory.Adjective, AnalyzeAdjective },
                { WordCategory.Pronoun,   AnalyzePronoun   }
            };
        }

        #region Structure Analysis

        /// <inheritdoc/>
        public WordStructure AnalyzeStructure(CzechWordRequest wordRequest)
        {
            ValidateRequest(wordRequest);

            if (!analyzers.TryGetValue(wordRequest.WordCategory, out var analyzer))
            {
                throw new NotSupportedException(
                    $"Word category '{wordRequest.WordCategory}' is not supported.");
            }

            return analyzer(wordRequest);
        }

        private static void ValidateRequest(CzechWordRequest wordRequest)
        {
            if (string.IsNullOrEmpty(wordRequest.Lemma))
            {
                throw new ArgumentException("Lemma cannot be null or empty.", nameof(wordRequest));
            }

            if (string.IsNullOrEmpty(wordRequest.Pattern))
            {
                throw new ArgumentException("Pattern cannot be empty.", nameof(wordRequest));
            }
        }

        #endregion Structure Analysis

        #region Noun

        private WordStructure AnalyzeNoun(CzechWordRequest wordRequest)
        {
            var lemma = wordRequest.Lemma;

            // PRIMARY PATH: roots.json lookup.
            // When a lemma has a phonologically active DerivationSuffix registered,
            // the derivational root IS the correct inflectional root base, because:
            //   stem (in DeclensionService) = Root + DerivationSuffix
            //   e.g. "student" + "k" = "studentk" → epenthesis → "studentek" ✓
            var rootEntry = rootProvider.GetCzechByLemma(lemma);
            if (rootEntry is not null)
            {
                rootEntry.Derivations.TryGetValue(lemma, out var link);

                if (link?.DerivationSuffix is not null)
                {
                    var rootFromLexicon = ApplyMobileVowelToRoot(rootEntry.Root, wordRequest);
                    return new WordStructure
                    {
                        Root             = rootFromLexicon,
                        DerivationSuffix = link.DerivationSuffix
                    };
                }
            }

            // FALLBACK PATH: heuristic root extraction for lemmata without a roots.json entry,
            // or lemmata in roots.json that have no active DerivationSuffix (e.g. "mládí").
            var root = ExtractNounRoot(lemma, wordRequest);
            return new WordStructure
            {
                Root = root
            };
        }

        /// <summary>
        /// Applies mobile vowel removal to a root string read from the root lexicon,
        /// using the same rules as <see cref="ExtractNounRoot"/>.
        /// </summary>
        private string ApplyMobileVowelToRoot(string root, CzechWordRequest request)
        {
            if (request.Case == Case.Nominative && request.Number == Number.Singular)
            {
                return root;
            }

            var hasMobileVowel = MorphologyHelper.EndsWithVowelConsonantVowelConsonant(root);
            if (hasMobileVowel)
            {
                return phonologyService.RemoveMobileVowel(root, true);
            }

            if (request.Gender == Gender.Masculine)
            {
                var hasMobileVowelIrregular =
                    nounDataProvider.GetIrregulars().TryGetValue(request.Lemma.ToLower(), out var irregular)
                    && irregular.HasMobileVowel;

                return phonologyService.RemoveMobileVowel(root, hasMobileVowelIrregular);
            }

            return root;
        }

        private string ExtractNounRoot(string lemma, CzechWordRequest request)
        {
            // vzor píseň: "píseň" → "písn"
            if (lemma.EndsWith("eň"))
            {
                return lemma[..^2] + "n";
            }

            string root;

            if (lemma.Length > 1 && !MorphologyHelper.IsConsonant(lemma[^1]))
            {
                // Feminine and neuter nouns end with a vowel in nom.sg. — strip it
                root = lemma[..^1];
            }
            else
            {
                // Masculine nouns end with a consonant — lemma is the root directly
                root = lemma;
            }

            var hasMobileVowel = MorphologyHelper.EndsWithVowelConsonantVowelConsonant(lemma);

            if (hasMobileVowel && !(request.Case == Case.Nominative && request.Number == Number.Singular))
            {
                root = phonologyService.RemoveMobileVowel(root, true);
            }
            else if (request.Gender == Gender.Masculine &&
                     !(request.Case == Case.Nominative && request.Number == Number.Singular))
            {
                var hasMobileVowelIrregular =
                    nounDataProvider.GetIrregulars().TryGetValue(request.Lemma.ToLower(), out var irregular)
                    && irregular.HasMobileVowel;

                root = phonologyService.RemoveMobileVowel(root, hasMobileVowelIrregular);
            }

            return root;
        }

        #endregion Noun

        #region Verb

        private string? ExtractPrefix(string lemma) => prefixService.FindVerbalPrefix(lemma);

        /// <inheritdoc/>
        public VerbStructure AnalyzeVerbStructure(CzechWordRequest request)
        {
            var prefix    = ExtractPrefix(request.Lemma);
            var lemmaBase = prefix != null ? request.Lemma[prefix.Length..] : request.Lemma;

            // Named patterns (nese, dělá, být…) — explicit stems in irregulars.json
            if (verbDataProvider.GetIrregulars().TryGetValue(request.Pattern!.ToLower(), out var namedPattern)
                && namedPattern.Stem != null)
            {
                return BuildFromExplicitStems(prefix, namedPattern);
            }

            if (prefix != null
                && verbDataProvider.GetIrregulars().TryGetValue(lemmaBase.ToLower(), out var basePattern)
                && basePattern.Stem != null)
            {
                return BuildFromExplicitStems(prefix, basePattern);
            }

            // Generic classes (trida1–trida5) — derive stems from infinitive
            if (verbDataProvider.GetPatterns().TryGetValue(request.Pattern!.ToLower(), out var classPattern))
            {
                return DeriveFromInfinitive(prefix, lemmaBase, request.Pattern!.ToLower(), classPattern.Aspect);
            }

            throw new NotSupportedException(
                $"Verb pattern '{request.Pattern}' not found in data. " +
                $"Add it to irregulars.json or use a trida1–trida5 class pattern.");
        }

        private VerbStructure BuildFromExplicitStems(string? prefix, VerbPattern pattern) =>
            new()
            {
                Prefix         = prefix,
                PresentStem    = pattern.PresentStem    ?? pattern.Stem!,
                PastStem       = pattern.PastStem       ?? pattern.Stem!,
                PassiveStem    = pattern.PassiveStem,
                ImperativeStem = pattern.ImperativeStem,
                Aspect         = pattern.Aspect
            };

        private VerbStructure DeriveFromInfinitive(
            string? prefix, string lemma, string patternKey, VerbAspect aspect)
        {
            return patternKey switch
            {
                "trida5" => DeriveTrida5(prefix, lemma, aspect),
                "trida4" => DeriveTrida4(prefix, lemma, aspect),
                "trida3" => DeriveTrida3(prefix, lemma, aspect),
                "trida2" => DeriveTrida2(prefix, lemma, aspect),
                "trida1" => DeriveTrida1(prefix, lemma, aspect),
                _        => throw new NotSupportedException($"Unknown pattern class: '{patternKey}'")
            };
        }

        // trida5: dělat → PresentStem: děl, PastStem: děla, ImperativeStem: dělej
        private VerbStructure DeriveTrida5(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("at") || lemma.EndsWith("át"))
            {
                var presentStem = lemma[..^2];
                return new()
                {
                    Prefix         = prefix,
                    PresentStem    = presentStem,
                    PastStem       = lemma[..^1],
                    PassiveStem    = lemma[..^1],
                    ImperativeStem = presentStem + "ej",
                    Aspect         = aspect
                };
            }

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        // trida4: prosit → PresentStem: pros, PastStem: prosi
        //         trpět  → PresentStem: trp,  PastStem: trpě
        private VerbStructure DeriveTrida4(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("it")  || lemma.EndsWith("ít") ||
                lemma.EndsWith("et")  || lemma.EndsWith("ět"))
            {
                return new()
                {
                    Prefix      = prefix,
                    PresentStem = lemma[..^2],
                    PastStem    = lemma[..^1],
                    PassiveStem = lemma[..^1],
                    Aspect      = aspect
                };
            }

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        // trida3: kupovat → PresentStem: kupu, PastStem: kupova, ImperativeStem: kupuj
        private VerbStructure DeriveTrida3(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("ovat"))
            {
                var presentStem = lemma[..^4] + "u";
                return new()
                {
                    Prefix         = prefix,
                    PresentStem    = presentStem,
                    PastStem       = lemma[..^1],
                    PassiveStem    = lemma[..^1],
                    ImperativeStem = presentStem + "j",
                    Aspect         = aspect
                };
            }

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        // trida2: tisknout → PresentStem: tisk, ImperativeStem: tiskn
        // ⚠ PastStem is approximate — add pastStem to irregulars.json for motion verbs.
        private VerbStructure DeriveTrida2(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("nout"))
            {
                var presentStem = lemma[..^4];
                return new()
                {
                    Prefix         = prefix,
                    PresentStem    = presentStem,
                    PastStem       = presentStem,
                    ImperativeStem = presentStem + "n",
                    Aspect         = aspect
                };
            }

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        // trida1: nést, brát, péct… — stems NOT derivable from infinitive.
        // All practical trida1 patterns must be in irregulars.json.
        private VerbStructure DeriveTrida1(string? prefix, string lemma, VerbAspect aspect)
        {
            var stem = lemma switch
            {
                _ when lemma.EndsWith("st") => lemma[..^2],
                _ when lemma.EndsWith("zt") => lemma[..^2],
                _ when lemma.EndsWith("ct") => lemma[..^2],
                _ when lemma.EndsWith("ít") => lemma[..^2],
                _                           => lemma
            };

            return new()
            {
                Prefix      = prefix,
                PresentStem = stem,
                PastStem    = stem,
                Aspect      = aspect
            };
        }

        private VerbStructure UnknownInfinitiveFallback(string? prefix, string lemma, VerbAspect aspect) =>
            new()
            {
                Prefix      = prefix,
                PresentStem = lemma,
                PastStem    = lemma,
                Aspect      = aspect
            };

        #endregion Verb

        #region Adjective

        private WordStructure AnalyzeAdjective(CzechWordRequest wordRequest) =>
            new() { Root = ExtractAdjectiveRoot(wordRequest.Lemma) };

        private static string ExtractAdjectiveRoot(string lemma)
        {
            if (lemma.EndsWith("ý") || lemma.EndsWith("á") ||
                lemma.EndsWith("é") || lemma.EndsWith("í"))
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

        private static WordStructure AnalyzePronoun(CzechWordRequest wordRequest) =>
            new() { Root = wordRequest.Lemma };

        #endregion Pronoun
    }
}

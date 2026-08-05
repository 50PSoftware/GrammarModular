using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Analyzes Czech word structure for noun and verb inflection.
    /// </summary>
    public class CzechWordStructureResolver : IWordStructureResolver<CzechWordRequest>, IVerbStructureResolver<CzechWordRequest>
    {
        private readonly IVerbDataProvider verbDataProvider;
        private readonly INounDataProvider nounDataProvider;
        private readonly CzechPrefixService prefixService;
        private readonly IPhonologyService<CzechWordRequest> phonologyService;
        private readonly IPhonemeRegistry _registry;
        private readonly IEpenthesisRuleEvaluator<CzechWordRequest> _epenthesisRuleEvaluator;

        private readonly Dictionary<WordCategory, Func<CzechWordRequest, WordStructure>> analyzers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechWordStructureResolver"/> type.
        /// </summary>
        public CzechWordStructureResolver(
            IVerbDataProvider verbDataProvider,
            INounDataProvider nounDataProvider,
            CzechPrefixService prefixService,
            IPhonologyService<CzechWordRequest> phonologyService,
            IPhonemeRegistry registry,
            IEpenthesisRuleEvaluator<CzechWordRequest> epenthesisRuleEvaluator)
        {
            this.verbDataProvider = verbDataProvider;
            this.nounDataProvider = nounDataProvider;
            this.prefixService = prefixService;
            this.phonologyService = phonologyService;
            _registry = registry;
            _epenthesisRuleEvaluator = epenthesisRuleEvaluator;

            analyzers = new Dictionary<WordCategory, Func<CzechWordRequest, WordStructure>>
            {
                { WordCategory.Noun,      AnalyzeNoun      },
                { WordCategory.Adjective, AnalyzeAdjective },
                { WordCategory.Pronoun,   AnalyzePronoun   }
            };
        }

        #region Structure Analysis

        /// <summary>
        /// Analyzes the morphological structure of the requested word.
        /// </summary>
        /// <param name="wordRequest">The word request to analyze or inflect.</param>
        /// <returns>The analyzed root, prefix, and suffix structure.</returns>
        public WordStructure AnalyzeStructure(CzechWordRequest wordRequest)
        {
            ValidateRequest(wordRequest);

            // No category means something bypassed MorphologyEngine, which fills it and refuses what it
            // cannot. Reported like any other category with no analyzer.
            if (wordRequest.WordCategory is not { } category
                || !analyzers.TryGetValue(category, out var analyzer))
            {
                throw new NotSupportedException(
                    $"Word category '{wordRequest.WordCategory?.ToString() ?? "neuvedeno"}' is not supported.");
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
            var pattern = wordRequest.Pattern!;

            var root = ExtractNounRoot(lemma, wordRequest);

            var derivationSuffix = DetectNounDerivationSuffix(lemma, pattern);

            // Ordinal: under cs-CZ a trailing "ch" is one collation unit, so EndsWith("h") is false for
            // "mouch" and leaves a spurious derivation suffix to be re-appended.
            if (!string.IsNullOrEmpty(derivationSuffix) && root.EndsWith(derivationSuffix, StringComparison.Ordinal))
            {
                if (_epenthesisRuleEvaluator.ShouldApplyEpenthesis(root[..^derivationSuffix.Length], derivationSuffix, wordRequest))
                {
                    root = root[..^derivationSuffix.Length];
                }
                else
                {
                    derivationSuffix = null;
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

            // Lexikon má přednost před heuristikou
            bool hasMobileE = request.HasMobileE
                ?? MorphologyHelper.HasLikelyMobileE(lemma); // fallback

            if (hasMobileE && !(request.Case == Case.Nominative && request.Number == Number.Singular))
            {
                root = phonologyService.RemoveMobileE(root, true);
            }

            return root;
        }

        private string? DetectNounDerivationSuffix(string lemma, string pattern)
        {
            if (pattern == "žena" && !MorphologyHelper.EndsWithVowelConsonantVowelConsonant(lemma) && lemma.Length > 2)
            {
                var derivationSuffix = lemma[^2];
                var phoneme = _registry.Get(derivationSuffix);
                return lemma[^2].ToString();
            }

            if (pattern == "město" && lemma.EndsWith("o") && lemma.Length > 2)
            {
                var beforeO = lemma[..^1];
                if (_registry.IsConsonant(beforeO[^1]))
                    return beforeO[^1].ToString();
            }

            return null;
        }

        #endregion Noun

        #region Verb

        private string? ExtractPrefix(string lemma) => prefixService.FindVerbalPrefix(lemma);

        /// <summary>
        /// Analyzes stems and affixes needed to conjugate the requested verb.
        /// </summary>
        /// <param name="request">The Czech word request to process.</param>
        /// <returns>The analyzed verb stems and prefix data.</returns>
        public VerbStructure AnalyzeVerbStructure(CzechWordRequest request)
        {
            var prefix = ExtractPrefix(request.Lemma);
            var lemmaBase = prefix != null ? request.Lemma[prefix.Length..] : request.Lemma;

            // Named patterns (nese, dělá, být…) — explicit stems in irregulars.json
            if (verbDataProvider.GetIrregulars().TryGetValue(request.Pattern!.ToLower(), out var namedPattern)
                && namedPattern.Stem != null)
            {
                // The stems belong to the unprefixed verb, so only a prefix actually stripped off a
                // derivative may be prepended: vidět opens with v, but idět is no form, giving *vvidí.
                var derivedFrom = IsPrefixedDerivative(lemmaBase, request.Pattern!.ToLower(), namedPattern)
                    ? prefix
                    : null;

                return BuildFromExplicitStems(derivedFrom, namedPattern);
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

        /// <summary>
        /// Determines whether the lemma is a prefixed derivative of the named pattern rather than the
        /// pattern's own verb, whose lemma merely opens with the same letters as some prefix.
        /// </summary>
        /// <param name="lemmaBase">The lemma with the candidate prefix already stripped.</param>
        /// <param name="patternKey">The key the pattern is stored under, which is its infinitive for most entries.</param>
        /// <param name="pattern">The named pattern the request asked for.</param>
        /// <returns><see langword="true"/> when the prefix belongs to the lemma; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// A derivative keeps the base verb right behind the prefix, so what remains after stripping opens
        /// with one of the pattern's own forms: odnést leaves nést, vyprosit leaves prosit. A lemma that
        /// only looks prefixed leaves a fragment instead: prosit leaves sit, spát leaves pát.
        /// </remarks>
        private static bool IsPrefixedDerivative(string lemmaBase, string patternKey, VerbPattern pattern)
        {
            // Ordinal comparison for the same reason as in AnalyzeNoun: cs-CZ collation treats digraphs
            // as one unit, so a culture-aware StartsWith answers on collation units, not on characters.
            bool OpensWith(string? candidate) =>
                !string.IsNullOrEmpty(candidate) && lemmaBase.StartsWith(candidate, StringComparison.Ordinal);

            return OpensWith(pattern.Infinitive)
                || OpensWith(patternKey)
                || OpensWith(pattern.Stem)
                || OpensWith(pattern.PresentStem);
        }

        private VerbStructure BuildFromExplicitStems(string? prefix, VerbPattern pattern) =>
            new()
            {
                Prefix = prefix,
                PresentStem = pattern.PresentStem ?? pattern.Stem!,
                PastStem = pattern.PastStem ?? pattern.Stem!,
                PassiveStem = pattern.PassiveStem,
                ImperativeStem = pattern.ImperativeStem,
                Aspect = pattern.Aspect
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
                _ => throw new NotSupportedException($"Unknown pattern class: '{patternKey}'")
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
                    Prefix = prefix,
                    PresentStem = presentStem,
                    PastStem = lemma[..^1],
                    PassiveStem = lemma[..^1],
                    ImperativeStem = presentStem + "ej",
                    Aspect = aspect
                };
            }

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        // trida4: prosit → PresentStem: pros, PastStem: prosi
        //         trpět  → PresentStem: trp,  PastStem: trpě
        private VerbStructure DeriveTrida4(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("it") || lemma.EndsWith("ít") ||
                lemma.EndsWith("et") || lemma.EndsWith("ět"))
            {
                var hasThemeI = lemma.EndsWith("it") || lemma.EndsWith("ít");

                return new()
                {
                    Prefix = prefix,
                    PresentStem = lemma[..^2],
                    PastStem = lemma[..^1],
                    // The two halves of the class part company here. An -ět verb carries its theme
                    // vowel into the participle — trpě-n, vidě-n — while an -it verb drops it and
                    // iotates the consonant instead: pros-it gives proše-n, not *prosi-n.
                    PassiveStem = hasThemeI ? IotatePassiveStem(lemma[..^2]) : lemma[..^1],
                    Aspect = aspect
                };
            }

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        /// <summary>
        /// Applies the iotation an -it verb undergoes before the passive participle ending.
        /// </summary>
        /// <param name="root">The infinitive with its theme vowel and the -t already removed.</param>
        /// <returns>The passive stem including the connecting vowel the ending attaches to.</returns>
        /// <remarks>
        /// The connecting vowel comes back with the stem because that is how the named patterns already
        /// state it — prosí carries <c>proše</c> and nese <c>nese</c>, each with a bare -n ending — and a
        /// class that answered differently would need its own ending row for no gain.
        /// <para>
        /// It is ě only where the consonant survives unchanged and the digraph has to carry the softness:
        /// změn-it gives změněn, while koup-it gives koupen. The clusters resolve to šť and žď, which are
        /// spelt with t and d in front of that ě — pust-it gives puštěn.
        /// </para>
        /// <para>
        /// t→c and d→z are regular in this position — placen, ztracen, hozen, narozen — unlike in
        /// derivation, where they are lexical. A verb that departs from any of this states its own
        /// passiveStem in irregulars.json, the way cítit does for cítěn.
        /// </para>
        /// </remarks>
        private static string IotatePassiveStem(string root)
        {
            // Clusters are read before their last consonant, which on its own would answer differently:
            // the t of pust would give *pucen rather than puštěn.
            if (root.EndsWith("st", StringComparison.Ordinal)) return root[..^2] + "ště";
            if (root.EndsWith("zd", StringComparison.Ordinal)) return root[..^2] + "ždě";

            return root switch
            {
                _ when root.EndsWith('s') => root[..^1] + "še",
                _ when root.EndsWith('z') => root[..^1] + "že",
                _ when root.EndsWith('t') => root[..^1] + "ce",
                _ when root.EndsWith('d') => root[..^1] + "ze",
                _ when root.EndsWith('n') => root + "ě",
                _ => root + "e"
            };
        }

        // trida3: kupovat → PresentStem: kupu, PastStem: kupova, ImperativeStem: kupuj
        private VerbStructure DeriveTrida3(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("ovat"))
            {
                var presentStem = lemma[..^4] + "u";
                return new()
                {
                    Prefix = prefix,
                    PresentStem = presentStem,
                    PastStem = lemma[..^1],
                    PassiveStem = lemma[..^1],
                    ImperativeStem = presentStem + "j",
                    Aspect = aspect
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
                    Prefix = prefix,
                    PresentStem = presentStem,
                    PastStem = presentStem,
                    ImperativeStem = presentStem + "n",
                    Aspect = aspect
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
                _ => lemma
            };

            return new()
            {
                Prefix = prefix,
                PresentStem = stem,
                PastStem = stem,
                Aspect = aspect
            };
        }

        private VerbStructure UnknownInfinitiveFallback(string? prefix, string lemma, VerbAspect aspect) =>
            new()
            {
                Prefix = prefix,
                PresentStem = lemma,
                PastStem = lemma,
                Aspect = aspect
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

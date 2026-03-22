using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    public class CzechWordStructureResolver : IWordStructureResolver<CzechWordRequest>, IVerbStructureResolver<CzechWordRequest>
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
                { WordCategory.Adjective, AnalyzeAdjective },
                { WordCategory.Pronoun, AnalyzePronoun }
            };
        }

        #region Structure Analysis

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

        #endregion Structure Analysis

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

        private string? ExtractPrefix(string lemma)
        {
            return prefixService.FindVerbalPrefix(lemma);
        }

        public VerbStructure AnalyzeVerbStructure(CzechWordRequest request)
        {
            var prefix = ExtractPrefix(request.Lemma);
            var lemmaBase = prefix != null
                ? request.Lemma[prefix.Length..]
                : request.Lemma;

            // Pojmenované patterny (nese, dělá, být...) — mají explicitní kmeny v JSON
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

            // Generické třídy (trida1–trida5) — kmeny derivujeme z infinitivu
            if (verbDataProvider.GetPatterns().TryGetValue(request.Pattern!.ToLower(), out var classPattern))
            {
                return DeriveFromInfinitive(prefix, lemmaBase, request.Pattern!.ToLower(), classPattern.Aspect);
            }

            // Neznámý pattern — nouzový fallback
            throw new NotSupportedException(
                $"Verb pattern '{request.Pattern}' not found in data. " +
                $"Add it to irregulars.json or use a trida1–trida5 class pattern.");
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
        // Koncovky: -ám, -áš, -á | past: -l | imp: -Ø (2sg), -me (1pl), -te (2pl)
        // "děl"   + "ám"  = "dělám"  ✓
        // "děla"  + "l"   = "dělal"  ✓
        // "dělej" + ""    = "dělej"  ✓
        private VerbStructure DeriveTrida5(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("at") || lemma.EndsWith("át"))
            {
                var presentStem = lemma[..^2];  // dělat → děl
                return new()
                {
                    Prefix         = prefix,
                    PresentStem    = presentStem,
                    PastStem       = lemma[..^1],            // dělat → děla
                    PassiveStem    = lemma[..^1],            // děla  + -n = dělan
                    ImperativeStem = presentStem + "ej",     // děl   + ej = dělej
                    Aspect         = aspect
                };
            }

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        // trida4: prosit → PresentStem: pros, PastStem: prosi
        //         trpět → PresentStem: trp,  PastStem: trpě
        // Koncovky: -ím, -íš, -í | past: -l
        // "pros" + "ím" = "prosím" ✓ | "prosi" + "l" = "prosil" ✓
        // "trp"  + "ím" = "trpím"  ✓ | "trpě"  + "l" = "trpěl"  ✓
        //
        // Imperativ trida4: PresentStem končí na jednu souhlásku → suffix -Ø
        // "pros" + ""  = "pros!"  ✓ | "trp" + "" = "trp!" ✓
        // ImperativeStem se nestaví — BuildImperativeForm použije PresentStem jako fallback.
        private VerbStructure DeriveTrida4(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("it") || lemma.EndsWith("ít") ||
                lemma.EndsWith("et") || lemma.EndsWith("ět"))
                return new()
                {
                    Prefix      = prefix,
                    PresentStem = lemma[..^2],   // prosit → pros, trpět → trp
                    PastStem    = lemma[..^1],   // prosit → prosi, trpět → trpě
                    PassiveStem = lemma[..^1],   // prosi + -n = prosin (dle kontextu)
                    Aspect      = aspect
                };

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        // trida3: kupovat → PresentStem: kupu, PastStem: kupova, ImperativeStem: kupuj
        // Logika: "kupovat"[..^4] = "kup" + "u" = "kupu"
        //         "kupu"   + "-je"  = "kupuje"  ✓
        //         "kupova" + "-l"   = "kupoval" ✓
        //         "kupuj"  + ""     = "kupuj!"  ✓
        // Alternace ov→uj je morfologicky systematická pro -ovat; phoneme registry ji nepokrývá.
        private VerbStructure DeriveTrida3(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("ovat"))
            {
                var presentStem = lemma[..^4] + "u";   // kupovat → kupu
                return new()
                {
                    Prefix         = prefix,
                    PresentStem    = presentStem,
                    PastStem       = lemma[..^1],       // kupovat → kupova
                    PassiveStem    = lemma[..^1],       // kupova  + -n = kupován
                    ImperativeStem = presentStem + "j", // kupu    + j  = kupuj
                    Aspect         = aspect
                };
            }

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        // trida2: tisknout → PresentStem: tisk, ImperativeStem: tiskn
        // Koncovky přítomného času: -nu, -neš, -ne  (tematická "n" je SOUČÁSTÍ KONCOVKY)
        // "tisk" + "-ne"  = "tiskne"  ✓
        // "tisk" + "-l"   = "tiskl"   ✓  (best-effort; pohybová slovesa → irregulars.json)
        //
        // Imperativ: tematická "n" přechází do kmene:
        // "tiskn" → EndsWithTwoConsonants → +i → "tiskni!" ✓
        // "tiskn" + "ěme" = "tiskněme!" ✓  (DTN pravidlo v BuildImperativeForm)
        //
        // ⚠ KNOWN LIMITATION: PastStem je approximate.
        //   "minout" → past "minul" ≠ "min" + "l"
        //   Správné řešení: přidat pastStem do irregulars.json pro pohybová slovesa.
        private VerbStructure DeriveTrida2(string? prefix, string lemma, VerbAspect aspect)
        {
            if (lemma.EndsWith("nout"))
            {
                var presentStem = lemma[..^4];  // tisknout → tisk
                return new()
                {
                    Prefix         = prefix,
                    PresentStem    = presentStem,
                    PastStem       = presentStem,           // best-effort
                    ImperativeStem = presentStem + "n",     // tisk + n = tiskn
                    Aspect         = aspect
                };
            }

            return UnknownInfinitiveFallback(prefix, lemma, aspect);
        }

        // trida1: nést, brát, péct... — kmeny jsou NEPREDIKTABILNÍ z infinitivu.
        // Všechny praktické trida1 vzory musí být v irregulars.json (nese, bere, peče...).
        // Tato metoda je jen nouzový fallback pro neznámá slovesa.
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

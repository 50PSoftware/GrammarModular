using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies adverb forms and their use as clause constituents.
    /// </summary>
    /// <remarks>
    /// NESČ classifies adverbs as uninflected, so the positive degree is the lemma and comparison is the
    /// only morphology under test here.
    /// </remarks>
    [TestClass]
    public sealed class AdverbTests
    {
        private static CzechWordFormComposer composer = null!;
        private static CzechSentenceBuilder builder = null!;
        private static ICzechAdverbService adverbs = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            composer = provider.GetRequiredService<CzechWordFormComposer>();
            builder = provider.GetRequiredService<CzechSentenceBuilder>();
            adverbs = provider.GetRequiredService<ICzechAdverbService>();
        }

        private static CzechWordRequest Adverb(string lemma, Degree? degree = null, bool? preferShort = null) => new()
        {
            Lemma = lemma,
            WordCategory = WordCategory.Adverb,
            Degree = degree,
            PrefersShortForm = preferShort
        };

        #region Forms

        /// <summary>
        /// The positive degree is the lemma, and an adverb that was never registered still passes through —
        /// an uninflected word needs no paradigm to be usable.
        /// </summary>
        /// <param name="lemma">The adverb lemma.</param>
        [DataTestMethod]
        [DataRow("rychle")]
        [DataRow("dobře")]
        [DataRow("dnes")]
        [DataRow("jak")]
        [DataRow("nesmyslně")]
        public void GetFullForm_PositiveDegree_ReturnsTheLemma(string lemma)
        {
            Assert.AreEqual(lemma, composer.GetFullForm(Adverb(lemma)).Form);
        }

        /// <summary>
        /// Regular and irregular comparatives alike come from the data, and the superlative is nej- on top.
        /// </summary>
        /// <param name="lemma">The adverb lemma.</param>
        /// <param name="comparative">The expected comparative.</param>
        /// <param name="superlative">The expected superlative.</param>
        [DataTestMethod]
        // regular -eji/-ěji
        [DataRow("rychle", "rychleji", "nejrychleji")]
        [DataRow("krásně", "krásněji", "nejkrásněji")]
        [DataRow("často", "častěji", "nejčastěji")]
        // palatalization before the suffix
        [DataRow("hladce", "hladčeji", "nejhladčeji")]
        [DataRow("hezky", "hezčeji", "nejhezčeji")]
        // suppletive and otherwise irregular
        [DataRow("dobře", "lépe", "nejlépe")]
        [DataRow("špatně", "hůře", "nejhůře")]
        [DataRow("brzy", "dříve", "nejdříve")]
        [DataRow("dlouho", "déle", "nejdéle")]
        [DataRow("málo", "méně", "nejméně")]
        public void GetFullForm_ComparedDegrees_UseTheRegisteredForms(string lemma, string comparative, string superlative)
        {
            Assert.AreEqual(comparative, composer.GetFullForm(Adverb(lemma, Degree.Comparative)).Form);
            Assert.AreEqual(superlative, composer.GetFullForm(Adverb(lemma, Degree.Superlative)).Form);
        }

        /// <summary>
        /// Several irregulars carry a doublet, the short form being the colloquial one. It is used only
        /// when asked for, and only where usage has one.
        /// </summary>
        /// <param name="lemma">The adverb lemma.</param>
        /// <param name="expected">The expected short comparative.</param>
        [DataTestMethod]
        [DataRow("dobře", "líp")]
        [DataRow("špatně", "hůř")]
        [DataRow("brzy", "dřív")]
        [DataRow("málo", "míň")]
        public void GetFullForm_ShortComparative_UsedOnlyWhenAskedFor(string lemma, string expected)
        {
            Assert.AreEqual(expected, composer.GetFullForm(Adverb(lemma, Degree.Comparative, preferShort: true)).Form);
        }

        /// <summary>
        /// An adverb without a doublet ignores the preference rather than inventing a short form.
        /// </summary>
        [TestMethod]
        public void GetFullForm_ShortComparativeWhereNoneExists_FallsBackToTheLongForm()
        {
            Assert.AreEqual("rychleji", composer.GetFullForm(Adverb("rychle", Degree.Comparative, preferShort: true)).Form);
        }

        /// <summary>
        /// Negative adverbs are lexical, not the positive ones with a prefix, and they are not compared.
        /// </summary>
        /// <param name="lemma">The negative adverb.</param>
        [DataTestMethod]
        [DataRow("nikdy")]
        [DataRow("nikde")]
        [DataRow("nikam")]
        [DataRow("nijak")]
        [DataRow("odnikud")]
        public void GetFullForm_NegativeAdverb_IsALemmaOfItsOwn(string lemma)
        {
            Assert.AreEqual(lemma, composer.GetFullForm(Adverb(lemma)).Form);
            Assert.IsFalse(adverbs.IsComparable(lemma));
        }

        #endregion Forms

        #region Comparison is data, not a rule

        /// <summary>
        /// An unregistered adverb gets its comparative from the rule, so the dictionary is not a gate on
        /// comparison — only on the irregulars.
        /// </summary>
        /// <param name="lemma">The unregistered adverb.</param>
        /// <param name="comparative">The comparative the rule should produce.</param>
        [DataTestMethod]
        [DataRow("nesmyslně", "nesmyslněji")]
        [DataRow("bláznivě", "bláznivěji")]
        [DataRow("mrzutě", "mrzutěji")]
        [DataRow("ledabyle", "ledabyleji")]
        public void GetFullForm_ComparativeOfUnregisteredAdverb_IsDerived(string lemma, string comparative)
        {
            Assert.AreEqual(comparative, composer.GetFullForm(Adverb(lemma, Degree.Comparative)).Form);
            Assert.AreEqual("nej" + comparative, composer.GetFullForm(Adverb(lemma, Degree.Superlative)).Form);
        }

        /// <summary>
        /// The rule reproduces the regular comparatives in the data, and everything it misses is an
        /// irregular the reference lists as one. That partition is what makes deriving safe, so it is
        /// measured rather than assumed — if a regular entry stops matching, the rule and the data have
        /// drifted apart and one of them is wrong.
        /// </summary>
        [TestMethod]
        public void DerivationRule_MissesOnlyTheKnownIrregulars()
        {
            var data = new ServiceCollection()
                .AddCzechGrammarServices()
                .BuildServiceProvider()
                .GetRequiredService<IAdverbDataProvider>()
                .GetAdverbs();

            // The ÚJČ irregular list, plus the same kind of word it does not name explicitly.
            var expectedIrregulars = new HashSet<string>
            {
                "dobře", "zle", "špatně", "brzy", "dlouho", "dlouze", "vysoko", "vysoce", "málo",
                "těžko", "těžce", "hluboko", "hluboce", "široko", "široce", "úzko", "úzce",
                "daleko", "blízko", "nízko", "rád", "hodně",

                // The rule produces snadněji, which IJP gives as correct beside snáze — so this is a
                // difference in which form the data keeps as primary, not a wrong derivation.
                "snadno"
            };

            var compared = data.Where(entry => entry.Value.Comparative is not null).ToList();
            var missed = compared
                .Where(entry => !DerivesTo(entry.Key, entry.Value.Comparative!))
                .Select(entry => entry.Key)
                .ToList();

            CollectionAssert.AreEquivalent(
                expectedIrregulars.Intersect(data.Keys).ToList(),
                missed,
                "Množina výjimek se rozešla s tím, co pravidlo netrefí.");

            Assert.IsTrue(compared.Count - missed.Count >= 70,
                "Pravidlo má pokrývat drtivou většinu pravidelných komparativů.");
        }

        // Mirrors the service's rule; the point of the test is that the data agrees with it.
        private static bool DerivesTo(string lemma, string comparative)
        {
            const string vowels = "aáeéěiíyýoóuúů";

            if (lemma.Length < 3)
            {
                return false;
            }

            var stem = vowels.Contains(lemma[^1]) ? lemma[..^1] : lemma;

            stem = stem[^1] switch
            {
                'c' or 'k' => stem[..^1] + 'č',
                'h' => stem[..^1] + 'ž',
                _ => stem
            };

            return comparative == stem + ("dtnbpmvf".Contains(stem[^1]) ? "ěji" : "eji");
        }

        /// <summary>
        /// An adverb that is not compared at all says so rather than producing "nejdnes".
        /// </summary>
        [TestMethod]
        public void GetFullForm_ComparativeOfNonGradableAdverb_Throws()
        {
            Assert.IsFalse(adverbs.IsComparable("dnes"));
            Assert.ThrowsException<InvalidOperationException>(
                () => composer.GetFullForm(Adverb("dnes", Degree.Comparative)));
        }

        /// <summary>
        /// The two members of a pair formed from one adjective are separate adverbs, and both are registered.
        /// </summary>
        /// <param name="first">The -o member.</param>
        /// <param name="second">The -e/-ě member.</param>
        /// <param name="comparative">The comparative they share.</param>
        [DataTestMethod]
        [DataRow("dlouho", "dlouze", "déle")]
        [DataRow("vysoko", "vysoce", "výše")]
        [DataRow("těžko", "těžce", "tíže")]
        [DataRow("široko", "široce", "šíře")]
        [DataRow("úzko", "úzce", "úže")]
        public void GetFullForm_BothMembersOfAPair_AreRegisteredAndShareTheComparative(
            string first, string second, string comparative)
        {
            Assert.AreEqual(first, composer.GetFullForm(Adverb(first)).Form);
            Assert.AreEqual(second, composer.GetFullForm(Adverb(second)).Form);
            Assert.AreEqual(comparative, composer.GetFullForm(Adverb(first, Degree.Comparative)).Form);
            Assert.AreEqual(comparative, composer.GetFullForm(Adverb(second, Degree.Comparative)).Form);
        }

        /// <summary>
        /// A comparative built a different way is an alternative, not a short variant of the primary —
        /// snadněji is not a clipping of snáze the way hůř is of hůře.
        /// </summary>
        /// <param name="lemma">The adverb lemma.</param>
        /// <param name="alternative">The competing comparative.</param>
        [DataTestMethod]
        [DataRow("snadno", "snadněji")]
        [DataRow("hluboko", "hloub")]
        [DataRow("široko", "šířeji")]
        [DataRow("úzko", "úžeji")]
        public void GetComparativeVariants_CompetingFormation_IsKeptAsAnAlternative(string lemma, string alternative)
        {
            CollectionAssert.Contains(adverbs.GetComparativeVariants(lemma).ToList(), alternative);
        }

        /// <summary>
        /// The clipped doublet and the competing formation are told apart: asking for the short form of
        /// hluboko gives the primary back, because hloub is not a clipping of hlouběji.
        /// </summary>
        [TestMethod]
        public void GetFullForm_CompetingFormationIsNotTreatedAsAShortVariant()
        {
            Assert.AreEqual("hlouběji", composer.GetFullForm(Adverb("hluboko", Degree.Comparative, preferShort: true)).Form);
            Assert.AreEqual("hůř", composer.GetFullForm(Adverb("špatně", Degree.Comparative, preferShort: true)).Form);
        }

        /// <summary>
        /// An unregistered or uncompared adverb has no variants rather than an exception — this is a query.
        /// </summary>
        [TestMethod]
        public void GetComparativeVariants_UnregisteredOrUncompared_IsEmpty()
        {
            Assert.AreEqual(0, adverbs.GetComparativeVariants("nesmyslně").Count);
            Assert.AreEqual(0, adverbs.GetComparativeVariants("dnes").Count);
        }

        /// <summary>
        /// The adjective an adverb comes from is recorded, so the mapping works in both directions.
        /// </summary>
        /// <param name="adjective">The base adjective.</param>
        /// <param name="adverb">The adverb expected from it.</param>
        [DataTestMethod]
        [DataRow("rychlý", "rychle")]
        [DataRow("dobrý", "dobře")]
        [DataRow("hezký", "hezky")]
        [DataRow("nízký", "nízko")]
        [DataRow("jednoduchý", "jednoduše")]
        public void GetAdverbsFor_Adjective_ReturnsItsAdverb(string adjective, string adverb)
        {
            CollectionAssert.Contains(adverbs.GetAdverbsFor(adjective).ToList(), adverb);
        }

        /// <summary>
        /// One adjective can yield two adverbs, which is why the mapping is a lookup and not a dictionary —
        /// and why deriving it by rule was never going to work.
        /// </summary>
        /// <param name="adjective">The base adjective.</param>
        /// <param name="first">One derived adverb.</param>
        /// <param name="second">The other.</param>
        [DataTestMethod]
        [DataRow("dlouhý", "dlouho", "dlouze")]
        [DataRow("vysoký", "vysoko", "vysoce")]
        [DataRow("těžký", "těžko", "těžce")]
        [DataRow("široký", "široko", "široce")]
        [DataRow("lehký", "lehko", "lehce")]
        public void GetAdverbsFor_AdjectiveWithTwoAdverbs_ReturnsBoth(string adjective, string first, string second)
        {
            var derived = adverbs.GetAdverbsFor(adjective).ToList();

            Assert.AreEqual(2, derived.Count, $"Od '{adjective}' se očekávala dvě příslovce.");
            CollectionAssert.Contains(derived, first);
            CollectionAssert.Contains(derived, second);
        }

        /// <summary>
        /// An adjective with no registered adverb comes back empty rather than throwing — this is a query.
        /// </summary>
        [TestMethod]
        public void GetAdverbsFor_UnregisteredAdjective_IsEmpty()
        {
            Assert.AreEqual(0, adverbs.GetAdverbsFor("nesmyslný").Count);
        }

        /// <summary>
        /// Structural checks over the whole adverb file, so a slip in a hand-written entry fails here
        /// rather than surfacing as a strange word in a sentence.
        /// </summary>
        [TestMethod]
        public void AdverbData_IsStructurallySound()
        {
            var data = new ServiceCollection()
                .AddCzechGrammarServices()
                .BuildServiceProvider()
                .GetRequiredService<IAdverbDataProvider>()
                .GetAdverbs();

            foreach (var (lemma, entry) in data)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(lemma), "Prázdné lemma.");

                if (entry.Comparative is null)
                {
                    Assert.IsNull(entry.ComparativeShort, $"'{lemma}' má krátký komparativ bez komparativu.");
                    Assert.AreEqual(0, entry.ComparativeAlternatives.Count, $"'{lemma}' má varianty bez komparativu.");
                    continue;
                }

                Assert.AreNotEqual(lemma, entry.Comparative, $"'{lemma}' má komparativ shodný s lemmatem.");
                Assert.IsFalse(entry.Comparative.StartsWith("nej", StringComparison.Ordinal),
                    $"'{lemma}' má v komparativu superlativní předponu — superlativ se skládá až za běhu.");

                if (entry.ComparativeShort is not null)
                {
                    Assert.AreNotEqual(entry.Comparative, entry.ComparativeShort,
                        $"'{lemma}' má krátký tvar shodný s dlouhým.");
                    Assert.IsTrue(entry.ComparativeShort.Length < entry.Comparative.Length,
                        $"'{lemma}': krátký tvar '{entry.ComparativeShort}' není kratší než '{entry.Comparative}'.");
                }

                CollectionAssert.DoesNotContain(entry.ComparativeAlternatives.ToList(), entry.Comparative,
                    $"'{lemma}' opakuje primární komparativ mezi variantami.");
            }
        }

        /// <summary>
        /// Every adjective named as a base is named consistently, so the reverse lookup cannot point at a
        /// lemma that differs only by a typo.
        /// </summary>
        [TestMethod]
        public void AdverbData_EveryBaseAdjectiveResolvesBackToItsAdverbs()
        {
            var data = new ServiceCollection()
                .AddCzechGrammarServices()
                .BuildServiceProvider()
                .GetRequiredService<IAdverbDataProvider>()
                .GetAdverbs();

            foreach (var (lemma, entry) in data.Where(pair => pair.Value.DerivedFrom is not null))
            {
                CollectionAssert.Contains(
                    adverbs.GetAdverbsFor(entry.DerivedFrom!).ToList(),
                    lemma,
                    $"'{lemma}' se hlásí k '{entry.DerivedFrom}', ale zpětné vyhledání ho nevrací.");
            }
        }

        /// <summary>
        /// One representative of every derivation pattern in the data, each checked against IJP.
        /// </summary>
        /// <remarks>
        /// The comparatives were written by applying the -ěji/-eji rule by hand, so what is verified here is
        /// the pattern rather than every entry: -ěji after d, t, n and the labials, -eji elsewhere, r → ř
        /// before it, and the palatalization that -ce and -ky bring. IJP gives tvrději, chytřeji, prudčeji,
        /// hezčeji and dál/dále directly; the entries built the same way are only as good as the rule.
        /// </remarks>
        /// <param name="lemma">The adverb lemma.</param>
        /// <param name="comparative">The expected comparative.</param>
        [DataTestMethod]
        // d/t/n and the labials keep ě — IJP: tvrdě → tvrději
        [DataRow("tvrdě", "tvrději")]
        [DataRow("čistě", "čistěji")]
        [DataRow("pevně", "pevněji")]
        [DataRow("hloupě", "hloupěji")]
        [DataRow("zdravě", "zdravěji")]
        // elsewhere plain e
        [DataRow("mile", "mileji")]
        [DataRow("vesele", "veseleji")]
        // r softens to ř — IJP: chytře → chytřeji
        [DataRow("chytře", "chytřeji")]
        [DataRow("ostře", "ostřeji")]
        // -ce and -ky palatalize — IJP: prudce → prudčeji, hezky → hezčeji, and hladčeji is IJP's own example
        [DataRow("prudce", "prudčeji")]
        [DataRow("hladce", "hladčeji")]
        [DataRow("hezky", "hezčeji")]
        [DataRow("lehce", "lehčeji")]
        public void GetFullForm_RegularlyComparedAdverbs_FollowTheSuffixRule(string lemma, string comparative)
        {
            Assert.AreEqual(comparative, composer.GetFullForm(Adverb(lemma, Degree.Comparative)).Form);
            Assert.AreEqual("nej" + comparative, composer.GetFullForm(Adverb(lemma, Degree.Superlative)).Form);
        }

        /// <summary>
        /// The irregular comparatives, checked against the list IJP id=410 gives in full.
        /// </summary>
        /// <param name="lemma">The adverb lemma.</param>
        /// <param name="comparative">The expected comparative.</param>
        /// <param name="shortForm">The expected clipped variant, or null where the list gives none.</param>
        [DataTestMethod]
        [DataRow("dobře", "lépe", "líp")]
        [DataRow("špatně", "hůře", "hůř")]
        [DataRow("zle", "hůře", "hůř")]
        [DataRow("brzy", "dříve", "dřív")]
        [DataRow("dlouho", "déle", null)]
        [DataRow("dlouze", "déle", null)]
        [DataRow("vysoko", "výše", "výš")]
        [DataRow("vysoce", "výše", "výš")]
        [DataRow("málo", "méně", "míň")]
        [DataRow("těžko", "tíže", "tíž")]
        [DataRow("těžce", "tíže", "tíž")]
        [DataRow("snadno", "snáze", "snáz")]
        [DataRow("široko", "šíře", "šíř")]
        [DataRow("široce", "šíře", "šíř")]
        [DataRow("úzko", "úže", null)]
        [DataRow("úzce", "úže", null)]
        [DataRow("daleko", "dále", "dál")]
        public void GetFullForm_IrregularComparatives_MatchTheUjcList(string lemma, string comparative, string? shortForm)
        {
            Assert.AreEqual(comparative, composer.GetFullForm(Adverb(lemma, Degree.Comparative)).Form);

            Assert.AreEqual(
                shortForm ?? comparative,
                composer.GetFullForm(Adverb(lemma, Degree.Comparative, preferShort: true)).Form,
                $"Krátký tvar '{lemma}'.");
        }

        #endregion Comparison is data, not a rule

        #region In a clause

        private static CzechWordRequest Verb(string lemma, string pattern, Person person, ReflexiveType reflexive = ReflexiveType.None) => new()
        {
            Lemma = lemma,
            Pattern = pattern,
            WordCategory = WordCategory.Verb,
            Modus = Modus.Indicative,
            Tense = Tense.Present,
            Aspect = VerbAspect.Imperfective,
            Voice = Voice.Active,
            Person = person,
            Number = Number.Singular,
            ReflexiveType = reflexive
        };

        /// <summary>
        /// An adverb of manner fills the MANN functor, which until now had nothing that could realize it.
        /// </summary>
        [TestMethod]
        public void Build_MannerAdverb_FillsTheFunctorThatHadNothingToRealizeIt()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third),
                Elements = [ClauseElement.Of(Adverb("rychle"), FgdFunctor.MANN, InformationStatus.New)]
            };

            Assert.AreEqual("Dělá rychle.", builder.Build(clause));
        }

        /// <summary>
        /// The plan that introduced interrogative focus could not build its own example, because the
        /// wh-word is an adverb. It builds now.
        /// </summary>
        [TestMethod]
        public void Build_InterrogativeAdverb_ProducesTheWhQuestionTheSentenceTypePlanCouldNot()
        {
            var clause = new CzechClause
            {
                SentenceType = SentenceType.Interrogative,
                Predicate = Verb("mít", "mít", Person.Second, ReflexiveType.ReflexivumTantum_Se),
                Elements = [ClauseElement.Of(Adverb("jak"), FgdFunctor.MANN, InformationStatus.Interrogative)],
                Terminator = "?"
            };

            Assert.AreEqual("Jak se máš?", builder.Build(clause));
        }

        /// <summary>
        /// A temporal adverb no longer has to be faked as a noun in the nominative.
        /// </summary>
        [TestMethod]
        public void Build_TemporalAdverb_NeedsNoNounWorkaround()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third, ReflexiveType.ReflexivumTantum_Se),
                Elements = [ClauseElement.Of(Adverb("dnes"), FgdFunctor.TWHEN, InformationStatus.Given)]
            };

            Assert.AreEqual("Dnes se dělá.", builder.Build(clause));
        }

        /// <summary>
        /// A degree adverb modifying another adverb — the constituent's head is itself an adverb.
        /// </summary>
        [TestMethod]
        public void Build_DegreeAdverbModifyingAnAdverb_Composes()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third),
                Elements =
                [
                    ClauseElement.Of(Adverb("rychle"), [Adverb("velmi")], FgdFunctor.MANN, InformationStatus.New)
                ]
            };

            Assert.AreEqual("Dělá velmi rychle.", builder.Build(clause));
        }

        /// <summary>
        /// A degree adverb modifying an adjective inside a noun phrase.
        /// </summary>
        [TestMethod]
        public void Build_DegreeAdverbInsideANounPhrase_Composes()
        {
            var student = new CzechWordRequest
            {
                Lemma = "student",
                Pattern = "pán",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                IsAnimate = true,
                Number = Number.Singular,
                Case = Case.Nominative
            };

            var adjective = new CzechWordRequest
            {
                Lemma = "rychlý",
                Pattern = "mladý",
                WordCategory = WordCategory.Adjective,
                Degree = Degree.Positive
            };

            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third),
                Elements =
                [
                    ClauseElement.Of(student, [Adverb("velmi"), adjective], FgdFunctor.ACT, InformationStatus.Given)
                ]
            };

            Assert.AreEqual("Velmi rychlý student dělá.", builder.Build(clause));
        }

        /// <summary>
        /// An adverb inside a prepositional phrase must not be marked as standing after the preposition —
        /// it has no form that could vary by it, and the flag is meaningless on an uninflected word.
        /// </summary>
        [TestMethod]
        public void Build_AdverbModifierInsidePrepositionalPhrase_IsNotGivenNominalCategories()
        {
            var skola = new CzechWordRequest
            {
                Lemma = "škola",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Case = Case.Locative
            };

            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third),
                Elements =
                [
                    new ClauseElement
                    {
                        Preposition = "v",
                        Word = skola,
                        Modifiers = [Adverb("velmi")],
                        Functor = FgdFunctor.LOC,
                        Status = InformationStatus.New
                    }
                ]
            };

            Assert.AreEqual("Dělá ve velmi škole.", builder.Build(clause));
        }

        /// <summary>
        /// A relative adverb introduces a relative clause. It is uninflected and is not an argument of that
        /// clause, so nothing agrees with the antecedent through it — the clause keeps its own person.
        /// </summary>
        /// <param name="relativizer">The relative adverb.</param>
        /// <param name="expected">The expected sentence.</param>
        [DataTestMethod]
        [DataRow("kde", "Student, kde dělám, dělá.")]
        [DataRow("kdy", "Student, kdy dělám, dělá.")]
        [DataRow("jak", "Student, jak dělám, dělá.")]
        public void Build_RelativeAdverb_IntroducesTheClauseWithoutAgreement(string relativizer, string expected)
        {
            var relativePredicate = Verb("dělat", "dělá", Person.First);

            var antecedent = new ClauseElement
            {
                Word = new CzechWordRequest
                {
                    Lemma = "student",
                    Pattern = "pán",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    IsAnimate = true,
                    Number = Number.Singular,
                    Case = Case.Nominative
                },
                Functor = FgdFunctor.ACT,
                Status = InformationStatus.Given,
                Relative = new RelativeAttachment
                {
                    Relativizer = relativizer,
                    Clause = new CzechClause { Predicate = relativePredicate }
                }
            };

            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third),
                Elements = [antecedent]
            };

            Assert.AreEqual(expected, builder.Build(clause));
        }

        /// <summary>
        /// The relative adverb takes the first position of its clause, so the clitic cluster follows it,
        /// exactly as it does after a relative pronoun or a subordinating conjunction.
        /// </summary>
        [TestMethod]
        public void Build_RelativeAdverbWithClitic_PlacesTheClusterAfterIt()
        {
            var relativePredicate = Verb("dělat", "dělá", Person.First, ReflexiveType.ReflexivumTantum_Se);

            var antecedent = new ClauseElement
            {
                Word = new CzechWordRequest
                {
                    Lemma = "student",
                    Pattern = "pán",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    IsAnimate = true,
                    Number = Number.Singular,
                    Case = Case.Nominative
                },
                Functor = FgdFunctor.ACT,
                Status = InformationStatus.Given,
                Relative = new RelativeAttachment
                {
                    Relativizer = "kde",
                    Clause = new CzechClause { Predicate = relativePredicate }
                }
            };

            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third),
                Elements = [antecedent]
            };

            Assert.AreEqual("Student, kde se dělám, dělá.", builder.Build(clause));
        }

        /// <summary>
        /// An adverb that does not relativize is reported, the same as a pronoun that is not relative.
        /// </summary>
        [TestMethod]
        public void Build_NonRelativeAdverbAsRelativizer_Throws()
        {
            Assert.IsFalse(adverbs.IsRelative("dnes"));
            Assert.IsTrue(adverbs.IsRelative("kde"));

            var antecedent = new ClauseElement
            {
                Word = new CzechWordRequest
                {
                    Lemma = "student",
                    Pattern = "pán",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    IsAnimate = true,
                    Number = Number.Singular,
                    Case = Case.Nominative
                },
                Functor = FgdFunctor.ACT,
                Status = InformationStatus.Given,
                Relative = new RelativeAttachment
                {
                    Relativizer = "dnes",
                    Clause = new CzechClause { Predicate = Verb("dělat", "dělá", Person.Third) }
                }
            };

            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third),
                Elements = [antecedent]
            };

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(clause));
            StringAssert.Contains(exception.Message, "vztažné příslovce");
        }

        #endregion In a clause
    }
}

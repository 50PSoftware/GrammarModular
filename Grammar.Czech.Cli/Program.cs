namespace Grammar.Cli
{
    using Grammar.Core.Enums;
    using Grammar.Czech;
    using Grammar.Czech.Enums;
    using Grammar.Czech.Models;
    using Grammar.Czech.Models.Syntax;
    using Grammar.Czech.Services;
    using Microsoft.Extensions.DependencyInjection;
    using System.Data.SqlTypes;

    /// <summary>
    /// Runs the sample command-line entry point for Czech morphology.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();

            var provider = services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true });
            var engine = provider.GetRequiredService<MorphologyEngine>();
            var composer = provider.GetRequiredService<CzechWordFormComposer>();

            //var studentRequest = new CzechWordRequest
            //{
            //    Lemma = "student",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    Pattern = "pán",
            //    IsAnimate = true
            //};

            //var studentkaRequest = new CzechWordRequest
            //{
            //    Lemma = "studentka",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Feminine,
            //    Number = Number.Singular,
            //    Pattern = "žena",
            //};

            //var womanRequest = new CzechWordRequest
            //{
            //    Lemma = "žena",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Feminine,
            //    Number = Number.Singular,
            //    Pattern = "žena",
            //};

            //var dogRequest = new CzechWordRequest
            //{
            //    Lemma = "pes",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    Pattern = "pán",
            //    IsAnimate = true,
            //    HasMobileE = true
            //};

            //var studentikRequest = new CzechWordRequest
            //{
            //    Lemma = "studentík",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    Pattern = "pán",
            //    IsAnimate = true,
            //};

            //var hochRequest = new CzechWordRequest
            //{
            //    Lemma = "hoch",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    Pattern = "pán",
            //    IsAnimate = true,
            //};

            //var horseRequest = new CzechWordRequest
            //{
            //    Lemma = "kůň",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    Pattern = "muž",
            //    IsAnimate = true
            //};

            //var houseRequest = new CzechWordRequest
            //{
            //    Lemma = "dům",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    Pattern = "hrad",
            //    IsAnimate = false
            //};

            //var forestRequest = new CzechWordRequest
            //{
            //    Lemma = "les",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    Pattern = "les",
            //    IsAnimate = false
            //};

            //var píseňRequest = new CzechWordRequest
            //{
            //    Lemma = "píseň",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Feminine,
            //    Number = Number.Singular,
            //    Pattern = "píseň"
            //};

            //PrintNounForms(composer, studentRequest);
            //PrintNounForms(composer, studentkaRequest);
            //PrintNounForms(composer, womanRequest);
            //PrintNounForms(composer, dogRequest);
            //PrintNounForms(composer, studentikRequest);
            //PrintNounForms(composer, hochRequest);
            //PrintNounForms(composer, horseRequest);
            //PrintNounForms(composer, houseRequest);
            //PrintNounForms(composer, forestRequest);
            //PrintNounForms(composer, píseňRequest);

            //var doRequest = new CzechWordRequest
            //{
            //    Lemma = "dělat",
            //    WordCategory = WordCategory.Verb,
            //    Gender = Gender.Masculine,
            //    Aspect = VerbAspect.Imperfective,
            //    Pattern = "dělá",
            //};

            //var carryRequest = new CzechWordRequest
            //{
            //    Lemma = "nést",
            //    WordCategory = WordCategory.Verb,
            //    Gender = Gender.Masculine,
            //    Aspect = VerbAspect.Imperfective,
            //    Pattern = "nese",
            //};

            //PrintVerbForms(composer, doRequest);
            //PrintVerbForms(composer, carryRequest);

            //var negativeCarryRequest = new CzechWordRequest
            //{
            //    Lemma = carryRequest.Lemma,
            //    WordCategory = carryRequest.WordCategory,
            //    Gender = carryRequest.Gender,
            //    Aspect = carryRequest.Aspect,
            //    Pattern = carryRequest.Pattern,
            //    IsNegative = true,
            //};

            //PrintVerbForms(composer, negativeCarryRequest);
            //PrintVerbForms(composer, carryRequest, Modus.Imperative);

            //var meRequest = new CzechWordRequest
            //{
            //    Lemma = "já",
            //    WordCategory = WordCategory.Pronoun,
            //};

            //var sheRequest = new CzechWordRequest
            //{
            //    Lemma = "ona",
            //    WordCategory = WordCategory.Pronoun,
            //};

            //var theyRequest = new CzechWordRequest
            //{
            //    Lemma = "ona_",
            //    WordCategory = WordCategory.Pronoun,
            //};

            //var myRequest = new CzechWordRequest
            //{
            //    Lemma = "můj",
            //    WordCategory = WordCategory.Pronoun,
            //};

            //// PronounHard — paradigm lookup
            //var tenRequest = new CzechWordRequest
            //{
            //    Lemma = "ten",
            //    WordCategory = WordCategory.Pronoun,
            //    Case = Case.Genitive,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    IsAnimate = true,
            //};

            //// AdjectiveHard — delegace
            //var mujRequest = new CzechWordRequest
            //{
            //    Lemma = "můj",
            //    WordCategory = WordCategory.Pronoun,
            //    Case = Case.Genitive,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    IsAnimate = true,
            //};

            //var someoneRequest = new CzechWordRequest
            //{
            //    Lemma = "někdo",
            //    WordCategory = WordCategory.Pronoun,
            //    Case = Case.Genitive,
            //};

            //PrintPronounForms(engine, meRequest);
            //PrintPronounForms(engine, sheRequest);
            //PrintPronounForms(engine, theyRequest);
            //PrintPronounForms(engine, myRequest);
            //Console.WriteLine("{0} -> {1}", tenRequest.Lemma, engine.GetForm(tenRequest).Form);
            //Console.WriteLine("{0} -> {1}", mujRequest.Lemma, engine.GetForm(mujRequest).Form);
            //Console.WriteLine("{0} -> {1}", someoneRequest.Lemma, engine.GetForm(someoneRequest).Form);

            //var přijmoutRequest = new CzechWordRequest
            //{
            //    Lemma = "přijmout",
            //    WordCategory = WordCategory.Verb,
            //    Gender = Gender.Masculine,
            //    Aspect = VerbAspect.Perfective,
            //    VerbClass = VerbClass.Class2
            //};

            //var odmíntouRequest = new CzechWordRequest
            //{
            //    Lemma = "odmítnout",
            //    WordCategory = WordCategory.Verb,
            //    Gender = Gender.Masculine,
            //    Aspect = VerbAspect.Perfective,
            //    VerbClass = VerbClass.Class2
            //};

            //PrintVerbForms(composer, přijmoutRequest);
            //PrintVerbForms(composer, odmíntouRequest);

            //var projevitRequest = new CzechWordRequest
            //{
            //    Lemma = "projevit",
            //    WordCategory = WordCategory.Verb,
            //    Gender = Gender.Feminine,
            //    Aspect = VerbAspect.Perfective,
            //    VerbClass = VerbClass.Class4
            //};

            //PrintVerbForms(composer, projevitRequest);

            //var appleRequest = new CzechWordRequest
            //{
            //    Lemma = "jablko",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Neuter,
            //    Case = Case.Genitive,
            //    Number = Number.Plural,
            //    Pattern = "město"
            //};

            //Console.WriteLine(composer.GetFullForm(appleRequest).Form);

            //var pnaWordRequest = new CzechWordRequest
            //{
            //    Lemma = "terminátor",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Masculine,
            //    Case = Case.Dative,
            //    Number = Number.Singular,
            //    Pattern = "pán",
            //    IsAnimate = true,
            //    HasMobileE = false
            //};

            //var chlap = new CzechWordRequest
            //{
            //    Lemma = "chlap",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Masculine,
            //    Number = Number.Singular,
            //    Case = Case.Dative,
            //    Pattern = "pán",
            //    IsAnimate = true
            //};

            //Console.WriteLine(composer.GetFullForm(pnaWordRequest).Form);
            //Console.WriteLine(composer.GetFullForm(chlap).Form);

            //var kaRequest = new CzechWordRequest
            //{
            //    Lemma = "vzpomínka",
            //    WordCategory = WordCategory.Noun,
            //    Gender = Gender.Feminine,
            //    Number = Number.Plural,
            //    Case = Case.Genitive,
            //    Pattern = "žena"
            //};

            //Console.WriteLine(composer.GetFullForm(kaRequest).Form);

            //kaRequest.Lemma = "studentka";
            //Console.WriteLine(composer.GetFullForm(kaRequest).Form);

            //kaRequest.Lemma = "kresba";
            //Console.WriteLine(composer.GetFullForm(kaRequest).Form);

            //kaRequest.Lemma = "lebka";
            //Console.WriteLine(composer.GetFullForm(kaRequest).Form);

            //kaRequest.Lemma = "bavlnka";
            //Console.WriteLine(composer.GetFullForm(kaRequest).Form);

            //kaRequest.Lemma = "sestra";
            //Console.WriteLine(composer.GetFullForm(kaRequest).Form);

            //kaRequest.Lemma = "pravda";
            //Console.WriteLine(composer.GetFullForm(kaRequest).Form);

            PrintSentences(provider.GetRequiredService<CzechSentenceBuilder>());

            var nameRequest = new CzechWordRequest
            {
                Lemma = "Mendominátor",
                Case = Case.Vocative,
                Number = Number.Singular,
                Gender = Gender.Masculine,
                IsAnimate = true,
                Pattern = "pán",
                WordCategory = WordCategory.Noun,
                HasMobileE = false
            };

            Console.WriteLine(composer.GetFullForm(nameRequest).Form);
        }

        /// <summary>
        /// Prints one clause in several information structures to show what the sentence builder decides:
        /// the order of constituents and where the clitic cluster lands.
        /// </summary>
        private static void PrintSentences(CzechSentenceBuilder sentenceBuilder)
        {
            var klara = new CzechWordRequest
            {
                Lemma = "Klára",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Case = Case.Nominative,
                Pattern = "žena"
            };

            var cestina = new CzechWordRequest
            {
                Lemma = "čeština",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Case = Case.Accusative,
                Pattern = "žena"
            };

            var vecer = new CzechWordRequest
            {
                Lemma = "večer",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Case = Case.Nominative,
                IsAnimate = false,
                Pattern = "hrad"
            };

            var ucitSe = new CzechWordRequest
            {
                Lemma = "učit",
                WordCategory = WordCategory.Verb,
                Pattern = "trida4",
                Tense = Tense.Present,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Aspect = VerbAspect.Imperfective,
                ReflexiveType = ReflexiveType.ReflexivumTantum_Se,

                // Výchozí kategorie pro věty bez podmětu; se subjektem je shoda přepíše.
                Person = Person.Third,
                Number = Number.Singular,
                Gender = Gender.Feminine
            };

            var subject = ClauseElement.Of(klara, FgdFunctor.ACT, InformationStatus.Given);
            var @object = ClauseElement.Of(cestina, FgdFunctor.PAT, InformationStatus.New);
            var time = ClauseElement.Of(vecer, FgdFunctor.TWHEN, InformationStatus.Given);

            var clause = new CzechClause { Predicate = ucitSe, Elements = [subject, @object] };

            // Klitikum za podmětem.
            Console.WriteLine(sentenceBuilder.Build(clause));

            // Klitikum za prvním konstituentem, ne za oběma předslovesnými.
            Console.WriteLine(sentenceBuilder.Build(clause with { Elements = [subject, time, @object] }));

            // Bez podmětu drží klitikum druhou pozici za slovesem.
            Console.WriteLine(sentenceBuilder.Build(clause with { Elements = [@object] }));

            // Fronting nepodmětu — kvůli tomuhle builder vznikl.
            Console.WriteLine(sentenceBuilder.Build(clause with { Elements = [time, @object] }));

            // Kondicionál: částice předchází reflexivum a celý klastr se stěhuje spolu.
            Console.WriteLine(sentenceBuilder.Build(clause with
            {
                Predicate = ucitSe with { Modus = Modus.Conditional },
                Elements = [subject, @object]
            }));

            // Minulý čas ve 3. osobě pomocné sloveso nemá.
            Console.WriteLine(sentenceBuilder.Build(clause with
            {
                Predicate = ucitSe with { Tense = Tense.Past },
                Elements = [subject, @object]
            }));

            // Ve 2. osobě se pomocné sloveso s reflexivem stahuje: jsi + se → ses.
            Console.WriteLine(sentenceBuilder.Build(clause with
            {
                Predicate = ucitSe with { Tense = Tense.Past, Person = Person.Second },
                Elements = [@object]
            }));

            // Krátké zájmeno opouští pořadí konstituentů a řadí se do klastru za reflexivum.
            var jeho = new CzechWordRequest
            {
                Lemma = "on",
                WordCategory = WordCategory.Pronoun,
                Case = Case.Accusative,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                IsAnimate = true
            };

            Console.WriteLine(sentenceBuilder.Build(clause with
            {
                Predicate = ucitSe with { Tense = Tense.Past },
                Elements = [subject, ClauseElement.Of(jeho, FgdFunctor.PAT, InformationStatus.New)]
            }));
        }

        private static void PrintWordInfo(CzechWordRequest request)
        {
            Console.WriteLine("{0}:", request.Lemma.Trim('_'));
        }

        private static void PrintNounForms(CzechWordFormComposer composer, CzechWordRequest request)
        {
            PrintWordInfo(request);
            foreach (var cNumber in Enum.GetValues<Number>())
            {
                Console.WriteLine("\t{0}:", cNumber.ToString().ToLowerInvariant());
                foreach (var cCase in Enum.GetValues<Case>())
                {
                    request.Number = cNumber;
                    request.Case = cCase;
                    var result = composer.GetFullForm(request);
                    Console.WriteLine($"\t\t{cCase}: {result.Form}");
                }
            }
        }

        private static void PrintVerbForms(CzechWordFormComposer composer, CzechWordRequest request, Modus modus = Modus.Conjunctive)
        {
            PrintWordInfo(request);
            foreach (var cTense in Enum.GetValues<Tense>())
            {
                foreach (var cNumber in Enum.GetValues<Number>())
                {
                    foreach (var cPerson in Enum.GetValues<Person>())
                    {
                        request.Tense = cTense;

                        if (modus == Modus.Imperative
                            && (cPerson is Person.Third
                            || cPerson is Person.First && cNumber is Number.Singular
                            || cPerson is Person.Second && cNumber is Number.Singular or Number.Plural))
                        {
                            continue;
                        }
                        else
                        {
                            request.Number = cNumber;
                            request.Person = cPerson;
                        }

                        request.Modus = modus;
                        var result = composer.GetFullForm(request);
                        Console.WriteLine("\t({1};{2};{3};{4};{5};{6}): {0}", result.Form, request.Tense, request.Number, request.Person, request.Modus, request.Gender, request.Aspect);
                    }
                }
            }
        }

        private static void PrintPronounForms(MorphologyEngine engine, CzechWordRequest request)
        {
            PrintWordInfo(request);
            foreach (var cCase in Enum.GetValues<Case>())
            {
                if (cCase == Case.Vocative)
                {
                    continue;
                }

                request.Case = cCase;
                var result = engine.GetForm(request);
                Console.WriteLine("\t{1}: {0}", result.Form, cCase);
            }
        }
    }
}

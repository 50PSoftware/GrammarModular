namespace Grammar.Cli
{
    using Grammar.Core.Enums;
    using Grammar.Czech;
    using Grammar.Czech.Models;
    using Grammar.Czech.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();

            var provider = services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true });
            var engine = provider.GetRequiredService<MorphologyEngine>();
            var composer = provider.GetRequiredService<CzechWordFormComposer>();

            var studentRequest = new CzechWordRequest
            {
                Lemma = "student",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Pattern = "pán",
                IsAnimate = true
            };

            var studentkaRequest = new CzechWordRequest
            {
                Lemma = "studentka",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Pattern = "žena",
            };

            var womanRequest = new CzechWordRequest
            {
                Lemma = "žena",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Pattern = "žena",
            };

            var dogRequest = new CzechWordRequest
            {
                Lemma = "pes",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Pattern = "pán",
                IsAnimate = true,
            };

            var studentikRequest = new CzechWordRequest
            {
                Lemma = "studentík",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Pattern = "pán",
                IsAnimate = true,
            };

            var hochRequest = new CzechWordRequest
            {
                Lemma = "hoch",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Pattern = "pán",
                IsAnimate = true,
            };

            var horseRequest = new CzechWordRequest
            {
                Lemma = "kůň",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Pattern = "muž",
                IsAnimate = true
            };

            var houseRequest = new CzechWordRequest
            {
                Lemma = "dům",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Pattern = "hrad",
                IsAnimate = false
            };

            var forestRequest = new CzechWordRequest
            {
                Lemma = "les",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Pattern = "les",
                IsAnimate = false
            };

            var píseňRequest = new CzechWordRequest
            {
                Lemma = "píseň",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Pattern = "píseň"
            };

            PrintNounForms(composer, studentRequest);
            PrintNounForms(composer, studentkaRequest);
            PrintNounForms(composer, womanRequest);
            PrintNounForms(composer, dogRequest);
            PrintNounForms(composer, studentikRequest);
            PrintNounForms(composer, hochRequest);
            PrintNounForms(composer, horseRequest);
            PrintNounForms(composer, houseRequest);
            PrintNounForms(composer, forestRequest);
            PrintNounForms(composer, píseňRequest);

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

            var meRequest = new CzechWordRequest
            {
                Lemma = "já",
                WordCategory = WordCategory.Pronoun,
            };

            var sheRequest = new CzechWordRequest
            {
                Lemma = "ona",
                WordCategory = WordCategory.Pronoun,
            };

            var theyRequest = new CzechWordRequest
            {
                Lemma = "ona_",
                WordCategory = WordCategory.Pronoun,
            };

            var myRequest = new CzechWordRequest
            {
                Lemma = "můj",
                WordCategory = WordCategory.Pronoun,
            };

            // PronounHard — paradigm lookup
            var tenRequest = new CzechWordRequest
            {
                Lemma = "ten",
                WordCategory = WordCategory.Pronoun,
                Case = Case.Genitive,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                IsAnimate = true,
            };

            // AdjectiveHard — delegace
            var mujRequest = new CzechWordRequest
            {
                Lemma = "můj",
                WordCategory = WordCategory.Pronoun,
                Case = Case.Genitive,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                IsAnimate = true,
            };

            var someoneRequest = new CzechWordRequest
            {
                Lemma = "někdo",
                WordCategory = WordCategory.Pronoun,
                Case = Case.Genitive,
            };

            PrintPronounForms(engine, meRequest);
            PrintPronounForms(engine, sheRequest);
            PrintPronounForms(engine, theyRequest);
            PrintPronounForms(engine, myRequest);
            Console.WriteLine("{0} -> {1}", tenRequest.Lemma, engine.GetForm(tenRequest).Form);
            Console.WriteLine("{0} -> {1}", mujRequest.Lemma, engine.GetForm(mujRequest).Form);
            Console.WriteLine("{0} -> {1}", someoneRequest.Lemma, engine.GetForm(someoneRequest).Form);
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

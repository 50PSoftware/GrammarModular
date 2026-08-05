namespace Grammar.Cli
{
    using Grammar.Core.Enums;
    using Grammar.Czech;
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

            PrintSentences(provider.GetRequiredService<CzechSentenceBuilder>());

            var teacherReq = new CzechWordRequest
            {
                Lemma = "učitel",
                WordCategory = WordCategory.Noun,
                Pattern = "učitel",
                IsAnimate = true
            };

            PrintNounForms(composer, teacherReq);
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
            var ho = new CzechWordRequest
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
                Elements = [subject, ClauseElement.Of(ho, FgdFunctor.PAT, InformationStatus.New)]
            }));

            // Ukazovací "to" je podle NESČ nestálá klitika: do povinného klastru nepatří a řadí se podle
            // aktuálního členění. Odkazuje zpátky, takže je téma — druhá pozice zůstává reflexivu.
            var to = new CzechWordRequest
            {
                Lemma = "ten",
                WordCategory = WordCategory.Pronoun,
                Case = Case.Accusative,
                Gender = Gender.Neuter,
                Number = Number.Singular
            };

            Console.WriteLine(sentenceBuilder.Build(clause with
            {
                Predicate = ucitSe with { Tense = Tense.Past },
                Elements = [subject, ClauseElement.Of(to, FgdFunctor.PAT, InformationStatus.Given)]
            }));

            // Víceslovný konstituent: přívlastek se shoduje s řídícím slovem a klastr jde až za celou frázi.
            var mlada = new CzechWordRequest
            {
                Lemma = "mladý",
                Pattern = "mladý",
                WordCategory = WordCategory.Adjective,
                Degree = Degree.Positive
            };

            Console.WriteLine(sentenceBuilder.Build(clause with
            {
                Elements = [ClauseElement.Of(klara, [mlada], FgdFunctor.ACT, InformationStatus.Given), @object]
            }));

            // Předložková fráze je jeden konstituent a předložka se vokalizuje podle následujícího slova.
            var skola = new CzechWordRequest
            {
                Lemma = "škola",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Case = Case.Locative,
                Pattern = "žena"
            };

            var veSkole = ClauseElement.Of("v", skola, FgdFunctor.LOC, InformationStatus.New);

            Console.WriteLine(sentenceBuilder.Build(clause with { Elements = [subject, veSkole] }));

            // Fronting celé fráze — klastr jde až za ni, ne za předložku.
            Console.WriteLine(sentenceBuilder.Build(clause with
            {
                Elements = [veSkole with { Status = InformationStatus.Given }, @object]
            }));

            // Souřadicí spojka stojí mimo klauzi, takže si ta druhá drží vlastní druhou pozici.
            Console.WriteLine(sentenceBuilder.Build(new Coordination("a",
            [
                clause with { Elements = [subject, @object] },
                clause with { Predicate = ucitSe with { Tense = Tense.Past }, Elements = [veSkole] }
            ])));

            // Podřadicí spojka první pozici obsazuje — klitikum jde hned za ni, před podmět.
            Console.WriteLine(sentenceBuilder.Build(new Subordination(
                clause with { Predicate = ucitSe with { ReflexiveType = ReflexiveType.None }, Elements = [subject, @object] },
                "protože",
                clause with { Elements = [veSkole] })));

            // Vztažná věta: zájmeno se shoduje s Klárou v rodě a čísle, pád si bere ze své role.
            var ktera = subject with
            {
                Relative = new RelativeAttachment
                {
                    Relativizer = "který",
                    Case = Case.Nominative,
                    Clause = new CzechClause { Predicate = ucitSe with { Tense = Tense.Past }, Elements = [veSkole] }
                }
            };

            Console.WriteLine(sentenceBuilder.Build(clause with { Elements = [ktera, @object] }));

            // Valenční rámec: u argumentů se pád nezadává, plyne ze slovesa.
            var kniha = new CzechWordRequest
            {
                Lemma = "kniha",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Pattern = "žena"
            };

            var zena = new CzechWordRequest
            {
                Lemma = "žena",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Pattern = "žena"
            };

            var davat = new CzechWordRequest
            {
                Lemma = "dávat",
                WordCategory = WordCategory.Verb,
                Pattern = "trida5",
                Tense = Tense.Past,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Aspect = VerbAspect.Imperfective,
                Person = Person.Third,
                Number = Number.Singular,
                Gender = Gender.Feminine
            };

            Console.WriteLine(sentenceBuilder.Build(new CzechClause
            {
                Predicate = davat,
                Elements =
                [
                    subject,
                    ClauseElement.Of(zena, FgdFunctor.ADDR, InformationStatus.New),
                    ClauseElement.Of(kniha, FgdFunctor.PAT, InformationStatus.New)
                ]
            }));

            // Předložku i její pád nese taky rámec.
            var jit = davat with { Lemma = "jít", Pattern = "jít" };

            Console.WriteLine(sentenceBuilder.Build(new CzechClause
            {
                Predicate = jit,
                FrameLabel = "motion",
                Elements = [subject, ClauseElement.Of(skola with { Case = null }, FgdFunctor.DIR3, InformationStatus.New)]
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

        private static void PrintAdjectiveForms(CzechWordFormComposer composer, CzechWordRequest request, Degree degree = Degree.Positive)
        {
            PrintWordInfo(request);
            foreach(var cGender in Enum.GetValues<Gender>())
            {
                Console.WriteLine("\t{0}:", cGender);
                foreach (var cNumber in Enum.GetValues<Number>())
                {
                    Console.WriteLine("\t\t{0}:", cNumber.ToString());
                    foreach (var cCase in Enum.GetValues<Case>())
                    {
                        request.Gender = cGender;
                        request.Number = cNumber;
                        request.Case = cCase;
                        request.Degree = degree;
                        var result = composer.GetFullForm(request);
                        Console.WriteLine($"\t\t\t{cCase}: {result.Form}");
                    }
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

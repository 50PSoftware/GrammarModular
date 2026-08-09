using Grammar.Czech.Cli.Interaction;
using Grammar.Czech.Cli.Rendering;
using Grammar.Czech.Cli.Sentence;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace Grammar.Czech.Cli.Commands
{
    /// <summary>
    /// Defines <c>veta</c>: lemmas in, a Czech sentence out.
    /// </summary>
    /// <remarks>
    /// Every question the review asks has a switch that answers it in advance, and the two write into
    /// the same record. That is what lets the command be used from a script, where nobody is there to be
    /// asked — with <c>--bez-dotazu</c> an open question is an error naming the switch that settles it,
    /// rather than a prompt to a terminal that is not attached.
    /// </remarks>
    public static class SentenceCommand
    {
        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <param name="lexicon">The root's lexicon option, read when the services are built.</param>
        /// <returns>The command.</returns>
        public static Command Create(Option<FileInfo?> lexicon)
        {
            var lemmas = new Argument<string[]>("lemmata")
            {
                Description = "Slova věty v základním tvaru: podmět, sloveso v infinitivu, předmět, …",
                Arity = ArgumentArity.OneOrMore,
            };

            var role = WordOption("--role", "Role členu podle FGD: --role kniha=PAT");
            var status = WordOption("--cleneni", "Dané nebo nové: --cleneni kniha=nove");
            var kase = WordOption("--pad", "Pád natvrdo, přebije rámec: --pad kniha=dativ");
            var gender = WordOption("--rod", "Jmenný rod: --rod Klara=zensky");
            var number = WordOption("--cislo", "Číslo jména: --cislo kniha=mnozne");
            var pattern = WordOption("--vzor", "Vzor skloňování: --vzor Klara=zena");
            var animate = WordOption("--zivotne", "Životnost: --zivotne pes=ano");
            var preposition = WordOption("--predlozka", "Předložka členu: --predlozka skola=v");
            var modifier = WordOption("--privlastek", "Shodný přívlastek: --privlastek Klara=mlada");

            var verb = new Option<string?>("--sloveso")
            {
                Description = "Které ze zadaných sloves je přísudek.",
            };

            var attach = new Option<string[]>("--pripojit")
            {
                Description = "Ke které klauzi se další věší: --pripojit 3=1. Bez toho visí na předchozí.",
                Arity = ArgumentArity.OneOrMore,
                AllowMultipleArgumentsPerToken = true,
            };

            // Přepínače přísudku berou buď holou hodnotu pro celé souvětí, nebo 'klauze=hodnota' pro
            // jednu klauzi. Číslo tu znamená klauzi, ne slovo — přísudek se adresuje klauzí, protože
            // každá má právě jeden.
            var frame = PredicateOption("--ramec", "Který význam slovesa se má vzít: --ramec motion, --ramec 2=motion.");
            var tense = PredicateOption("--cas", "Čas přísudku: minuly, pritomny, budouci. Nebo --cas 2=minuly.");
            var mood = PredicateOption("--zpusob", "Způsob: oznamovaci, rozkazovaci, podminovaci.");
            var voice = PredicateOption("--slovesny-rod", "Slovesný rod: cinny, trpny.");
            var aspect = PredicateOption("--vid", "Vid: dokonavy, nedokonavy.");
            var person = PredicateOption("--osoba", "Osoba přísudku ve větě bez podmětu: 1, 2, 3.");
            var predicateNumber = PredicateOption("--cislo-prisudku", "Číslo přísudku ve větě bez podmětu.");
            var reflexive = PredicateOption("--zvratne", "Zvratné se/si u slovesa mimo slovník.");
            var negative = FlagOption("--zapor", "Zápor přísudku. Bez hodnoty celá věta, '--zapor 2' jen druhá klauze.");

            var dropSubject = FlagOption(
                "--vypustit-podmet",
                "Vypustit podmětové zájmeno, když nenese důraz: 'čtu' místo 'já čtu'.");
            var sentenceType = new Option<string?>("--typ") { Description = "Druh věty: oznamovaci, tazaci." };
            var terminator = new Option<string?>("--konec") { Description = "Koncové znaménko věty." };

            var quiet = new Option<bool>("--bez-dotazu")
            {
                Description = "Nic se nedoptávat. Otevřená otázka je pak chyba, ne dotaz.",
            };

            var json = new Option<bool>("--json")
            {
                Description = "Vypsat větu i s rozborem strojově.",
            };

            var command = new Command("veta", "Poskládá českou větu ze zadaných lemmat.")
            {
                lemmas,
                role, status, kase, gender, number, pattern, animate, preposition, modifier,
                verb, attach, frame, tense, mood, voice, aspect, person, predicateNumber, reflexive,
                negative, dropSubject, sentenceType, terminator, quiet, json,
            };

            command.Aliases.Add("věta");

            command.SetAction(parse =>
            {
                var overrides = new DraftOverrides
                {
                    PredicateLemma = parse.GetValue(verb),
                    SentenceType = Optional(parse.GetValue(sentenceType), Terms.ParseSentenceType),
                    Terminator = parse.GetValue(terminator),
                };

                AssignPredicate(overrides, "ramec", parse.GetValue(frame));
                AssignPredicate(overrides, "cas", parse.GetValue(tense));
                AssignPredicate(overrides, "zpusob", parse.GetValue(mood));
                AssignPredicate(overrides, "rod", parse.GetValue(voice));
                AssignPredicate(overrides, "vid", parse.GetValue(aspect));
                AssignPredicate(overrides, "osoba", parse.GetValue(person));
                AssignPredicate(overrides, "cislo", parse.GetValue(predicateNumber));
                AssignPredicate(overrides, "zvratne", parse.GetValue(reflexive));
                AssignFlag(parse, overrides, "zapor", negative);
                AssignFlag(parse, overrides, "podmet", dropSubject);

                Assign(overrides, "role", parse.GetValue(role));
                Assign(overrides, "cleneni", parse.GetValue(status));
                Assign(overrides, "pad", parse.GetValue(kase));
                Assign(overrides, "rod", parse.GetValue(gender));
                Assign(overrides, "cislo", parse.GetValue(number));
                Assign(overrides, "vzor", parse.GetValue(pattern));
                Assign(overrides, "zivotne", parse.GetValue(animate));
                Assign(overrides, "predlozka", parse.GetValue(preposition));
                Assign(overrides, "privlastek", parse.GetValue(modifier));

                foreach (var assignment in parse.GetValue(attach) ?? [])
                {
                    OverrideParser.AssignAttachment(overrides, assignment);
                }

                return Run(
                    parse.GetValue(lemmas) ?? [],
                    overrides,
                    Services.Build(parse.GetValue(lexicon)),
                    interactive: !parse.GetValue(quiet) && !parse.GetValue(json) && !Console.IsInputRedirected,
                    json: parse.GetValue(json));
            });

            return command;
        }

        private static int Run(
            IReadOnlyList<string> lemmas,
            DraftOverrides overrides,
            IServiceProvider services,
            bool interactive,
            bool json)
        {
            var builder = services.GetRequiredService<DraftBuilder>();
            var composer = services.GetRequiredService<SentenceComposer>();

            var draft = interactive
                ? services.GetRequiredService<ReviewLoop>().Run(lemmas, overrides)
                : builder.Build(lemmas, overrides);

            // Odejít z dialogu bez věty není chyba, jen se nic nevypíše.
            if (draft is null)
            {
                return 1;
            }

            if (draft.Gaps() is { Count: > 0 } gaps)
            {
                throw new CliException(string.Join(Environment.NewLine, gaps));
            }

            var sentence = composer.Compose(draft);

            Console.WriteLine(json ? JsonView.Render(draft, sentence) : sentence);

            // Poznámky jdou na chybový výstup, aby `gramatika veta … > soubor` obsahoval jen větu.
            if (!json)
            {
                foreach (var note in draft.Notes)
                {
                    Console.Error.WriteLine(note);
                }
            }

            return 0;
        }

        private static Option<string[]> WordOption(string name, string description) => new(name)
        {
            Description = description,
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = true,
        };

        // Same shape as a word switch, and told apart by which switch it is: one addresses a
        // constituent by its position in the word list, the other a predicate by its clause.
        private static Option<string[]> PredicateOption(string name, string description) => new(name)
        {
            Description = description,
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = true,
        };

        // A yes-or-no switch that need not be answered: writing it is the answer. It still takes a
        // clause number, so that the thing which used to be a bare flag keeps working as one.
        private static Option<string[]> FlagOption(string name, string description) => new(name)
        {
            Description = description,
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true,
        };

        private static void Assign(DraftOverrides overrides, string property, string[]? assignments)
        {
            foreach (var assignment in assignments ?? [])
            {
                OverrideParser.Assign(overrides, property, assignment);
            }
        }

        private static void AssignPredicate(DraftOverrides overrides, string property, string[]? arguments)
        {
            foreach (var argument in arguments ?? [])
            {
                OverrideParser.AssignPredicate(overrides, property, argument);
            }
        }

        // Written without a value it answers itself, and written with a bare number it answers itself
        // about that clause — so '--zapor' and '--zapor 2' both read the way they look.
        private static void AssignFlag(
            ParseResult parse, DraftOverrides overrides, string property, Option<string[]> option)
        {
            if (parse.GetResult(option) is null)
            {
                return;
            }

            var arguments = parse.GetValue(option) ?? [];

            if (arguments.Length == 0)
            {
                OverrideParser.AssignPredicate(overrides, property, "ano");

                return;
            }

            foreach (var argument in arguments)
            {
                OverrideParser.AssignPredicate(
                    overrides, property, argument.Contains('=') ? argument : $"{argument}=ano");
            }
        }

        private static TValue? Optional<TValue>(string? text, Func<string, TValue> parse)
            where TValue : struct => text is null ? null : parse(text);
    }
}

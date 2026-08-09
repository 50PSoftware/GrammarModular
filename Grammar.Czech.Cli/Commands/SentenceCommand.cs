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

            var frame = new Option<string?>("--ramec")
            {
                Description = "Který význam slovesa se má vzít, když jich slovník vede víc.",
            };

            var tense = new Option<string?>("--cas") { Description = "Čas přísudku: minuly, pritomny, budouci." };
            var mood = new Option<string?>("--zpusob") { Description = "Způsob: oznamovaci, rozkazovaci, podminovaci." };
            var voice = new Option<string?>("--slovesny-rod") { Description = "Slovesný rod: cinny, trpny." };
            var aspect = new Option<string?>("--vid") { Description = "Vid: dokonavy, nedokonavy." };
            var person = new Option<string?>("--osoba") { Description = "Osoba přísudku ve větě bez podmětu: 1, 2, 3." };
            var predicateNumber = new Option<string?>("--cislo-prisudku") { Description = "Číslo přísudku ve větě bez podmětu." };
            var reflexive = new Option<string?>("--zvratne") { Description = "Zvratné se/si u slovesa mimo slovník." };
            var negative = new Option<bool>("--zapor") { Description = "Zápor přísudku." };

            var dropSubject = new Option<bool>("--vypustit-podmet")
            {
                Description = "Vypustit podmětové zájmeno, když nenese důraz: 'čtu' místo 'já čtu'.",
            };
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
                    FrameLabel = parse.GetValue(frame),
                    Tense = Optional(parse.GetValue(tense), Terms.ParseTense),
                    Mood = Optional(parse.GetValue(mood), Terms.ParseMood),
                    Voice = Optional(parse.GetValue(voice), Terms.ParseVoice),
                    Aspect = Optional(parse.GetValue(aspect), Terms.ParseAspect),
                    Person = Optional(parse.GetValue(person), Terms.ParsePerson),
                    Number = Optional(parse.GetValue(predicateNumber), Terms.ParseNumber),
                    SentenceType = Optional(parse.GetValue(sentenceType), Terms.ParseSentenceType),
                    Terminator = parse.GetValue(terminator),
                    IsNegative = parse.GetValue(negative) ? true : null,
                    DropSubject = parse.GetValue(dropSubject) ? true : null,
                };

                if (parse.GetValue(reflexive) is { } stated)
                {
                    overrides.ReflexiveType = OverrideParser.ParseReflexive(stated);
                }

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

        private static void Assign(DraftOverrides overrides, string property, string[]? assignments)
        {
            foreach (var assignment in assignments ?? [])
            {
                OverrideParser.Assign(overrides, property, assignment);
            }
        }

        private static TValue? Optional<TValue>(string? text, Func<string, TValue> parse)
            where TValue : struct => text is null ? null : parse(text);
    }
}

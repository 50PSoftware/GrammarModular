using Grammar.Czech.Cli.Rendering;
using Grammar.Czech.Cli.Sentence;

namespace Grammar.Czech.Cli.Interaction
{
    /// <summary>
    /// Runs the tool as a session: sentence after sentence, each one correctable where it stands.
    /// </summary>
    /// <remarks>
    /// The one-shot <c>veta</c> command builds one sentence from the words it was started with, and
    /// once that sentence is confirmed the process is over. Which is the wrong shape for how a sentence
    /// actually gets made: it gets poked at — another tense, another information structure, another
    /// case — and every poke used to be a fresh process, a fresh dictionary open and the word
    /// <c>veta</c> typed again.
    /// <para>
    /// So the two stand beside each other rather than one replacing the other. <c>veta</c> stays exactly
    /// what it was, scriptable and answerable in advance; the session is for the poking.
    /// </para>
    /// </remarks>
    public sealed class SessionLoop
    {
        private readonly DraftBuilder _builder;
        private readonly DraftView _view;
        private readonly SentenceComposer _composer;
        private readonly TextReader _input;
        private readonly TextWriter _output;

        // Co drží přes celé sezení, a co jen přes jednu větu, rozhoduje ForgetWords: o přísudku se dá
        // mluvit i o větě, která ještě není, kdežto '3 pad=dativ' je o slovu, které na trojce stojí teď.
        private readonly DraftOverrides _overrides = new();

        private List<string> _lemmas = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionLoop"/> type.
        /// </summary>
        /// <param name="builder">The builder that turns lemmas into a draft.</param>
        /// <param name="view">The renderer for the review table.</param>
        /// <param name="composer">The composer that builds the sentence.</param>
        /// <param name="input">The stream to read lines from.</param>
        /// <param name="output">The stream to write to.</param>
        public SessionLoop(
            DraftBuilder builder,
            DraftView view,
            SentenceComposer composer,
            TextReader input,
            TextWriter output)
        {
            _builder = builder;
            _view = view;
            _composer = composer;
            _input = input;
            _output = output;
        }

        /// <summary>
        /// Runs until the user leaves.
        /// </summary>
        /// <returns>The process exit code.</returns>
        public int Run()
        {
            _output.WriteLine(Greeting);

            while (true)
            {
                _output.Write("> ");

                var line = _input.ReadLine();

                // Konec vstupu je totéž co ':konec' — jinak by se smyčka nad zavřeným stdin točila
                // donekonečna.
                if (line is null)
                {
                    _output.WriteLine();

                    return 0;
                }

                line = line.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith(':'))
                {
                    if (Command(line[1..].Trim()))
                    {
                        return 0;
                    }

                    continue;
                }

                if (line.StartsWith('?'))
                {
                    Explain(line[1..].Trim());

                    continue;
                }

                // Oprava se pozná podle rovnítka a je vždycky o poslední větě; holá slova jsou věta
                // nová. Nic jiného to rozlišit neumí a tohle je pravidlo, které jde vyslovit.
                if (line.Contains('='))
                {
                    Correct(line);
                }
                else
                {
                    Start(line);
                }

                Show();
            }
        }

        private void Start(string line)
        {
            _lemmas = [.. line.Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries)];

            // Nová věta má nová slova, takže co bylo řečeno o slovech, přestává platit. Co bylo řečeno
            // o přísudku, platí dál — na to se sezení ptalo jednou a ptát se na to u každé věty znovu
            // je přesně ta práce, kterou má sezení ušetřit.
            _overrides.ForgetWords();
        }

        private void Correct(string line)
        {
            if (_lemmas.Count == 0)
            {
                _output.WriteLine("Zatím není co opravovat — napiš nejdřív lemmata věty.");

                return;
            }

            try
            {
                OverrideParser.Apply(line, _lemmas, _overrides, LastPredicatePositions());
            }
            catch (CliException exception)
            {
                _output.WriteLine(exception.Message);
            }
        }

        private IReadOnlyList<int> LastPredicatePositions()
        {
            try
            {
                return _builder.Build(_lemmas, _overrides).PredicatePositions;
            }
            catch (CliException)
            {
                // Návrh, který neprojde, o svých přísudcích nic neřekne. Oprava se stejně bude zkoušet
                // proti němu a spadne se srozumitelnější hláškou než tahle.
                return [];
            }
        }

        private void Show()
        {
            if (_lemmas.Count == 0)
            {
                return;
            }

            SentenceDraft draft;

            try
            {
                draft = _builder.Build(_lemmas, _overrides);
            }
            catch (CliException exception)
            {
                _output.WriteLine(exception.Message);

                return;
            }

            _output.WriteLine();
            _output.WriteLine(_view.Render(draft));
            _output.WriteLine();

            var gaps = draft.Gaps();

            if (gaps.Count > 0)
            {
                foreach (var gap in gaps)
                {
                    _output.WriteLine("  " + gap);
                }

                _output.WriteLine();

                return;
            }

            _output.WriteLine("  " + _composer.Compose(draft, out var failure));

            if (failure is not null)
            {
                _output.WriteLine("  Chyba: " + failure);
            }

            _output.WriteLine();
        }

        private void Explain(string topic)
        {
            _output.WriteLine(topic.Length == 0
                ? $"Témata: {HelpTopics.Names}. Napiš třeba '? role'."
                : HelpTopics.Find(topic) ?? $"O '{topic}' nápovědu nemám. Témata: {HelpTopics.Names}.");
        }

        // Vrací true, když má sezení skončit.
        private bool Command(string command)
        {
            switch (Terms.Plain(command))
            {
                case "konec":
                case "q":
                    return true;

                case "":
                case "?":
                case "napoveda":
                    _output.WriteLine(Help);

                    return false;

                case "stav":
                    Describe();

                    return false;

                case "zapomen":
                    _overrides.ForgetAll();
                    _output.WriteLine("Zapomenuto. Věta se teď staví bez čehokoli trvalého.");

                    return false;

                case "znovu":
                    Show();

                    return false;

                default:
                    _output.WriteLine($"Příkaz ':{command}' neznám. ':?' je nápověda.");

                    return false;
            }
        }

        private void Describe()
        {
            var lines = _overrides.Describe();

            if (lines.Count == 0)
            {
                _output.WriteLine("Trvale nic nastaveného není — každá věta se staví od nuly.");

                return;
            }

            _output.WriteLine("Trvale platí, dokud to nezrušíš ':zapomen':");

            foreach (var line in lines)
            {
                _output.WriteLine("  " + line);
            }
        }

        private const string Greeting = """

            gramatika — sezení. Piš lemmata a stiskni Enter.
            ':?' nápověda, '? role' vysvětlení pojmů, ':konec' konec.
            """;

        private const string Help = """

            Řádek beze rovnítka je nová věta — lemmata oddělená mezerami, v základním tvaru:

              učitel psát dopis student

            Řádek s rovnítkem je oprava té poslední. Píše se '<cíl> <co>=<jak>' a dá se jich dát víc:

              3 role=ADDR            role podle FGD: ACT, PAT, ADDR, LOC, TWHEN, …
              kniha pad=dativ        pád natvrdo; přebije ten, který by řídil rámec
              2 cleneni=dane         dané (téma) nebo nové (réma) — z toho plyne slovosled
              2 rod=ženský           rod, číslo, vzor, životné — když slovník slovo nezná
              2 predlozka=v          předložka, kterou se člen otevírá
              2 privlastek=mladý     shodný přívlastek před jméno
              p cas=minulý           čas, způsob, rod, vid, osoba, číslo — přísudku
              p zapor=ano            zápor přísudku
              p podmet=vypustit      vypustit podmětové zájmeno: 'čtu' místo 'já čtu'
              p ramec=transfer       který význam slovesa se má vzít
              k 3=1                  na kterou klauzi se třetí věší

            Co řekneš o přísudku a o větě, platí do konce sezení a použije se i na další věty —
            ':stav' to ukáže, ':zapomen' to zruší. Co řekneš o slovu, platí jen pro tu jednu větu:
            další věta má na trojce jiné slovo, takže přenést to na ni by znamenalo mluvit o něčem
            jiném.

            Příkazy sezení:

              :stav       co je trvale nastavené
              :zapomen    zrušit to
              :znovu      vypsat poslední větu znovu
              :konec      konec (nebo Ctrl+D)

            Vysvětlení pojmů: ? role, ? cleneni, ? pad, ? ramec, ? odhad

            """;
    }
}

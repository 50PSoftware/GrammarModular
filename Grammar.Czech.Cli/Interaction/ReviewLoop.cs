using Grammar.Czech.Cli.Rendering;
using Grammar.Czech.Cli.Sentence;

namespace Grammar.Czech.Cli.Interaction
{
    /// <summary>
    /// Shows the draft, takes corrections, and hands back the reading the user agreed to.
    /// </summary>
    /// <remarks>
    /// Every correction is written into <see cref="DraftOverrides"/> and the draft is rebuilt from the
    /// lemmas, rather than the draft being patched in place. That keeps one code path for both modes —
    /// what the dialog produces is exactly what the same switches would have produced — and it lets a
    /// later correction undo the consequences of an earlier one instead of layering on top of them.
    /// </remarks>
    public sealed class ReviewLoop
    {
        private readonly DraftBuilder _builder;
        private readonly DraftView _view;
        private readonly SentenceComposer _composer;
        private readonly TextReader _input;
        private readonly TextWriter _output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReviewLoop"/> type.
        /// </summary>
        /// <param name="builder">The builder that turns lemmas into a draft.</param>
        /// <param name="view">The renderer for the review table.</param>
        /// <param name="composer">The composer that builds the sentence.</param>
        /// <param name="input">The stream to read answers from.</param>
        /// <param name="output">The stream to write the dialog to.</param>
        public ReviewLoop(
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
        /// Runs the review until the user accepts the draft or abandons it.
        /// </summary>
        /// <param name="lemmas">The lemmas, in the order they were entered.</param>
        /// <param name="overrides">What the user stated on the command line, extended as the dialog goes.</param>
        /// <returns>The accepted draft, or <see langword="null"/> when the user gave up.</returns>
        public SentenceDraft? Run(IReadOnlyList<string> lemmas, DraftOverrides overrides)
        {
            while (true)
            {
                var draft = _builder.Build(lemmas, overrides);

                _output.WriteLine();
                _output.WriteLine(_view.Render(draft));
                _output.WriteLine();

                var gaps = draft.Gaps();

                if (gaps.Count == 0)
                {
                    _output.WriteLine("Věta: " + _composer.Compose(draft, out var failure));

                    if (failure is not null)
                    {
                        _output.WriteLine("Chyba: " + failure);
                    }
                }

                // Číslo v nápovědě je číslo skutečného členu: pořadí odpovídá zadání, takže dvojka
                // často připadne slovesu a příklad by ukazoval na přísudek.
                var example = draft.Clauses.SelectMany(clause => clause.Constituents).FirstOrDefault()?.Position ?? 1;

                _output.WriteLine();
                _output.WriteLine($"[Enter] potvrdit · {example} role=ADDR · p cas=minuly · ? nápověda · q konec");
                _output.Write("> ");

                var answer = _input.ReadLine();

                // Konec vstupu je totéž co 'q'. Bez toho by se smyčka nad zavřeným stdin točila donekonečna.
                if (answer is null)
                {
                    return null;
                }

                answer = answer.Trim();

                if (answer.Length == 0)
                {
                    if (gaps.Count > 0)
                    {
                        _output.WriteLine("Ještě to není hotové — viz řádky „Chybí“ výš.");

                        continue;
                    }

                    return draft;
                }

                if (answer is "q" or "Q" or "konec")
                {
                    return null;
                }

                if (answer is "?" or "h" or "napoveda" or "nápověda")
                {
                    _output.WriteLine(Help);

                    continue;
                }

                try
                {
                    OverrideParser.Apply(answer, lemmas, overrides, draft.PredicatePositions);
                }
                catch (CliException exception)
                {
                    _output.WriteLine(exception.Message);
                }
            }
        }

        private const string Help = """

            Oprava se píše jako: <cíl> <co>=<jak>, a dá se jich na řádek dát víc.
            Cíl je pořadové číslo slova, jeho lemma, nebo 'p' pro přísudek.

              2 role=ADDR            role podle FGD: ACT, PAT, ADDR, LOC, TWHEN, …
              2 cleneni=dane         dané (téma) nebo nové (réma) — z toho plyne slovosled
              kniha pad=dativ        pád natvrdo; přebije ten, který by řídil rámec
              2 rod=ženský           rod, číslo, vzor, životné — když slovník slovo nezná
              2 predlozka=v          předložka, kterou se člen otevírá
              2 privlastek=mladý     shodný přívlastek před jméno

              p cas=minulý           čas, způsob, rod (činný/trpný), vid, osoba, číslo — všem klauzím
              p2 cas=přítomný        totéž jen druhé klauzi; číslo za p je klauze, ne slovo
              p zapor=ano            zápor přísudku
              p podmet=vypustit      vypustit podmětové zájmeno: 'čtu' místo 'já čtu'
              p zvratne=se           zvratné se/si u slovesa, které slovník nevede
              p ramec=transfer       který význam slovesa se má vzít
              p typ=tázací           druh věty; 'p konec=?' mění koncové znaménko

              k 3=1                  na kterou klauzi se třetí věší; bez toho visí na předchozí

            """;
    }
}

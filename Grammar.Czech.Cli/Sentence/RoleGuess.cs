using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Hands out roles from the order the words were written in, for a verb the dictionary has no frame
    /// for.
    /// </summary>
    /// <remarks>
    /// A frame is what says which arguments a verb takes and in which case; without one the role
    /// resolver has no slots to give out, every constituent stays roleless and no sentence comes of it.
    /// The dictionary holds frames for sixty verbs, so that is the ordinary outcome rather than the rare
    /// one, and refusing every other verb makes the tool useful only for the words it already knows.
    /// <para>
    /// This belongs in the tool and not in the library for the same reason <see cref="LemmaGuess"/>
    /// does: it is a proposal made visible and always overridable, and a library that invented valency
    /// silently would be lying to whoever built on it.
    /// </para>
    /// <para>
    /// What it knows is the unmarked Czech clause — the actor first, then the patient, and an addressee
    /// after them if it is something that can be addressed. It does not know that <em>zahrada</em> in
    /// <em>pes běhá zahrada</em> is a place and not a patient; neither word order nor animacy says so,
    /// and only the verb would. That case comes out as <em>Pes běhá zahradu</em>, marked as guessed,
    /// and is corrected with <c>--role zahrada=LOC</c>.
    /// </para>
    /// </remarks>
    public sealed class RoleGuess
    {
        private readonly IValencyProvider<CzechLexicalEntry> _lexicon;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleGuess"/> type.
        /// </summary>
        /// <param name="lexicon">The dictionary, asked whether the verb has a frame at all.</param>
        public RoleGuess(IValencyProvider<CzechLexicalEntry> lexicon)
        {
            _lexicon = lexicon;
        }

        /// <summary>
        /// Determines whether the predicate is one this has anything to say about.
        /// </summary>
        /// <param name="predicateLemma">The lemma of the predicate.</param>
        /// <returns>
        /// <see langword="true"/> when the dictionary holds no frame for it; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool IsNeeded(string predicateLemma) => !_lexicon.GetFrames(predicateLemma).Any();

        /// <summary>
        /// Writes a role onto every constituent that has none, in the order they were written.
        /// </summary>
        /// <param name="constituents">The constituents of the clause.</param>
        /// <param name="voice">
        /// The voice the clause was asked for, since it decides the case a guessed role stands in — a
        /// frame would remap it the same way through its <c>PassivePeriphrastic</c> diathesis, and there
        /// is no frame here to do that for us.
        /// </param>
        /// <returns>The constituents a role was invented for, in order.</returns>
        /// <exception cref="CliException">
        /// Thrown when the clause asks for the passive but no argument could be guessed as the patient —
        /// there would be nothing to promote to the subject and no way to know what the sentence means.
        /// </exception>
        /// <remarks>
        /// Anything already carrying a role keeps it, whether it came from a switch or from the review,
        /// and a constituent opened by a preposition is skipped: a preposition makes it an adverbial and
        /// the role resolver reads those off the preposition itself, which is knowledge rather than a
        /// guess.
        /// </remarks>
        public IReadOnlyList<ConstituentDraft> Assign(IEnumerable<ConstituentDraft> constituents, Voice voice)
        {
            var all = constituents.ToList();

            var open = all
                .Where(constituent => constituent.Functor is null && constituent.Preposition is null)
                .ToList();

            var taken = new List<ConstituentDraft>();

            foreach (var constituent in open)
            {
                var functor = Next(taken.Count, constituent);

                if (functor is null)
                {
                    // Čtvrtý a další holý člen: pro něj už není čtení, které by šlo obhájit pořadím.
                    // Zůstane bez role, ohlásí se jako chybějící a uživatel řekne, co s ním.
                    break;
                }

                constituent.Functor = functor;
                constituent.FunctorIsGuessed = true;

                // Role bez pádu nikam nevede: pád jinak dává rámec, a ten tu žádný není. Píše se rovnou
                // na request, tedy tam, kam ho píše i --pad — odhadnutá role a odhadnutý pád jsou jedno
                // rozhodnutí a rozdělit je by znamenalo tvrdit, že jedno z nich je jistější.
                //
                // Přes proměnnou, protože request je struct: 'constituent.Word.Case = …' by nastavil pád
                // kopii, kterou getter právě vrátil, a zahodil ho.
                var word = constituent.Word;

                // Trpný rod bez rámce se nezná v aktivní podobě a pak jen přeznačí pád — knihovna to tak
                // dělá skrz PassivePeriphrastic diatezi (PAT → nominativ, ACT → instrumentál) a bez
                // rámce je tohle jediné místo, které tu diatezi umí napodobit.
                word.Case ??= (functor, voice) switch
                {
                    (FgdFunctor.ACT, Voice.Passive) => Case.Instrumental,
                    (FgdFunctor.PAT, Voice.Passive) => Case.Nominative,
                    (FgdFunctor.ACT, _) => Case.Nominative,
                    (FgdFunctor.PAT, _) => Case.Accusative,
                    _ => Case.Dative,
                };

                constituent.Word = word;

                taken.Add(constituent);
            }

            if (voice == Voice.Passive && all.All(constituent => constituent.Functor != FgdFunctor.PAT))
            {
                throw new CliException(
                    "Trpný rod chce trpitele (PAT), na kterého se dá povýšit podmět — sloveso není ve "
                    + "slovníku, takže role se hádaly z pořadí slov, a mezi nimi žádný nevyšel jako PAT. "
                    + "Buď dej trpiteli roli výslovně (--role slovo=PAT), nebo zůstaň v činném rodě.");
            }

            return taken;
        }

        private static FgdFunctor? Next(int taken, ConstituentDraft constituent) => taken switch
        {
            0 => FgdFunctor.ACT,
            1 => FgdFunctor.PAT,

            // Adresát jen tam, kde je komu adresovat. Neživotné třetí jméno je spíš okolnost než
            // adresát a nechat ho otevřené je poctivější než dát mu dativ.
            2 when constituent.Word.IsAnimate == true => FgdFunctor.ADDR,
            _ => null,
        };
    }
}

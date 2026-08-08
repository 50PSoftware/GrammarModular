using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Services;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Reads a list of lemmas as a clause: which word is the predicate, what role each of the others
    /// fills, and what the lexicon already knows about them.
    /// </summary>
    /// <remarks>
    /// Everything here is a proposal. The tool derives what it can and leaves the rest open rather than
    /// picking for the user — an undecided functor stays <see langword="null"/> and comes back as a gap,
    /// because a wrong role produces a well-formed sentence that means something else, which is worse
    /// than a question.
    /// </remarks>
    public sealed class DraftBuilder
    {
        private readonly IValencyProvider<CzechLexicalEntry> _lexicon;
        private readonly CzechLexiconEnricher _enricher;
        private readonly ICzechPrepositionService _prepositions;
        private readonly LemmaGuess _guess;

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftBuilder"/> type.
        /// </summary>
        /// <param name="lexicon">The dictionary to read frames and entries from.</param>
        /// <param name="enricher">The service that fills a request from a dictionary entry.</param>
        /// <param name="prepositions">The preposition service, for recognizing and governing them.</param>
        /// <param name="guess">The fallback for lemmas the dictionary does not hold.</param>
        public DraftBuilder(
            IValencyProvider<CzechLexicalEntry> lexicon,
            CzechLexiconEnricher enricher,
            ICzechPrepositionService prepositions,
            LemmaGuess guess)
        {
            _lexicon = lexicon;
            _enricher = enricher;
            _prepositions = prepositions;
            _guess = guess;
        }

        /// <summary>
        /// Reads the lemmas as a clause and returns what the tool made of them.
        /// </summary>
        /// <param name="lemmas">The lemmas, in the order they were entered.</param>
        /// <param name="overrides">What the user has stated so far.</param>
        /// <returns>The draft, complete or with its open questions recorded.</returns>
        /// <exception cref="CliException">Thrown when the lemmas cannot form a clause at all.</exception>
        public ClauseDraft Build(IReadOnlyList<string> lemmas, DraftOverrides overrides)
        {
            if (lemmas.Count == 0)
            {
                throw new CliException("Nezadal jsi žádné lemma. Zkus třeba: gramatika veta student cist kniha");
            }

            foreach (var key in overrides.UnmatchedKeys(lemmas))
            {
                throw new CliException(
                    $"Přepínač se odkazuje na '{key}', ale takové slovo ve větě není. Použij lemma, "
                    + $"jak jsi ho zadal, nebo jeho pořadí (1 až {lemmas.Count}).");
            }

            var words = lemmas
                .Select((lemma, index) => Resolve(lemma, index + 1, overrides))
                .ToList();

            var draft = new ClauseDraft();

            AttachPredicate(draft, words, overrides);
            AttachConstituents(draft, words, overrides);

            // Slovesný rod musí být rozhodnutý dřív, než se hledá rámec: trpná diateze je jiný rámec,
            // ne ten činný přepočítaný.
            ApplyPredicateCategories(draft, overrides);

            ResolveFrame(draft, overrides);
            AssignFunctors(draft);
            ApplyGovernment(draft);
            ApplyStatus(draft, overrides);
            AgreeWithSubject(draft, overrides);
            Report(draft);

            return draft;
        }

        private ResolvedWord Resolve(string lemma, int position, DraftOverrides overrides)
        {
            var stated = overrides.Find(lemma, position);
            var word = new CzechWordRequest { Lemma = lemma };

            // Nejdřív to, co řekl uživatel — enricher i odhad píšou jen do prázdného, takže tímhle
            // pořadím zadané vždycky vyhraje nad slovníkem.
            if (stated is not null)
            {
                word.Gender = stated.Gender;
                word.Number = stated.Number;
                word.Case = stated.Case;
                word.Pattern = stated.Pattern;
                word.IsAnimate = stated.IsAnimate;
            }

            var known = _lexicon.HasEntry(lemma);
            var enriched = _enricher.Enrich(word);

            // Předložku slovník nevede — je v pravidlech, ne v hesláři — takže se pozná podle toho,
            // že pro ni existuje rekce.
            if (enriched.WordCategory is null && _prepositions.GetAllowedCases(lemma).Any())
            {
                enriched.WordCategory = WordCategory.Preposition;
            }

            var origin = stated?.StatesMorphology == true
                ? MetadataOrigin.User
                : known ? MetadataOrigin.Lexicon : MetadataOrigin.Guess;

            var completed = _guess.Complete(enriched);

            // Číslo neplyne ani ze slovníku, ani ze zakončení — jméno bez čísla je nedořečený request,
            // ne jednotné číslo. Pomnožné slovo si plurál nese v hesle.
            if (completed.WordCategory is not WordCategory.Verb && completed.Number is null)
            {
                completed.Number = completed.IsPluralOnly == true ? Number.Plural : Number.Singular;
            }

            return new ResolvedWord(position, lemma, completed, origin, stated);
        }

        private static void AttachPredicate(ClauseDraft draft, List<ResolvedWord> words, DraftOverrides overrides)
        {
            var verbs = words
                .Where(word => word.Request.WordCategory == WordCategory.Verb)
                .ToList();

            var predicate = overrides.PredicateLemma is { } named
                ? verbs.FirstOrDefault(word => string.Equals(word.Lemma, named, StringComparison.OrdinalIgnoreCase))
                    ?? throw new CliException($"Slovo '{named}' mezi zadanými slovesy není.")
                : verbs.Count switch
                {
                    1 => verbs[0],
                    0 => throw new CliException(
                        "Ve větě není sloveso, takže není z čeho udělat přísudek. Přidej ho v infinitivu."),
                    _ => throw new CliException(
                        $"Sloves je víc ({string.Join(", ", verbs.Select(verb => verb.Lemma))}). "
                        + "Řekni přepínačem --sloveso, které z nich je přísudek; souvětí zatím neskládám."),
                };

            // Přísudek není větný člen v tomhle smyslu, takže role ani členění na něm nedávají smysl.
            // Bez téhle hlášky by 'sloveso cleneni=nove' beze slova zmizelo.
            if (predicate.Stated is { } stated
                && (stated.Functor is not null || stated.Status is not null || stated.Preposition is not null))
            {
                throw new CliException(
                    $"'{predicate.Lemma}' je přísudek — role, členění ani předložka se na něj nevztahují. "
                    + "Čas, způsob a osobu mu nastav přes 'p' (v dialogu) nebo přepínači --cas, --zpusob, --osoba.");
            }

            words.Remove(predicate);

            draft.PredicateLemma = predicate.Lemma;
            draft.PredicatePosition = predicate.Position;
            draft.Predicate = predicate.Request;
            draft.PredicateOrigin = predicate.Origin;
        }

        private void AttachConstituents(ClauseDraft draft, List<ResolvedWord> words, DraftOverrides overrides)
        {
            string? pendingPreposition = null;
            var pendingModifiers = new List<CzechWordRequest>();

            foreach (var word in words)
            {
                if (word.Request.WordCategory == WordCategory.Preposition)
                {
                    if (pendingPreposition is not null)
                    {
                        throw new CliException(
                            $"Předložky '{pendingPreposition}' a '{word.Lemma}' stojí za sebou a nemají "
                            + "u sebe jméno.");
                    }

                    pendingPreposition = word.Lemma;

                    continue;
                }

                // Shodný přívlastek stojí před svým jménem, takže se drží stranou, dokud nepřijde hlava,
                // ke které patří. Rod, číslo a pád si doplní builder shodou.
                if (word.Request.WordCategory is WordCategory.Adjective or WordCategory.Numerale)
                {
                    pendingModifiers.Add(word.Request);

                    continue;
                }

                var constituent = new ConstituentDraft(word.Position, word.Lemma, word.Request, word.Origin)
                {
                    Preposition = word.Stated?.Preposition ?? pendingPreposition,
                    Functor = word.Stated?.Functor,
                };

                constituent.Modifiers.AddRange(pendingModifiers);
                constituent.Modifiers.AddRange(
                    (word.Stated?.Modifiers ?? []).Select(BuildModifier));

                draft.Constituents.Add(constituent);

                pendingPreposition = null;
                pendingModifiers.Clear();
            }

            if (pendingPreposition is not null)
            {
                throw new CliException(
                    $"Předložka '{pendingPreposition}' nemá u sebe jméno — za předložkou má následovat to, "
                    + "co řídí.");
            }

            if (pendingModifiers.Count > 0)
            {
                throw new CliException(
                    $"Přívlastek '{pendingModifiers[^1].Lemma}' nemá co rozvíjet — shodný přívlastek stojí "
                    + "před svým jménem.");
            }
        }

        private CzechWordRequest BuildModifier(string lemma) => _guess.Complete(
            _enricher.Enrich(new CzechWordRequest { Lemma = lemma, WordCategory = WordCategory.Adjective }));

        private void ResolveFrame(ClauseDraft draft, DraftOverrides overrides)
        {
            // Diatezi vybírá slovesný rod: trpný rámec přemapuje všechny slotry najednou, takže se do
            // něj nedá nahlédnout přes ten činný.
            var diathesis = draft.Predicate.Voice == Voice.Passive
                ? Diathesis.PassivePeriphrastic
                : Diathesis.Active;

            var frames = _lexicon.GetFrames(draft.PredicateLemma)
                .Where(frame => frame.Diathesis == diathesis)
                .ToList();

            draft.FrameChoices = frames;

            if (frames.Count == 0)
            {
                return;
            }

            if (overrides.FrameLabel is { } label)
            {
                draft.Frame = frames.FirstOrDefault(frame =>
                        string.Equals(frame.FrameLabel, label, StringComparison.OrdinalIgnoreCase))
                    ?? throw new CliException(
                        $"Sloveso '{draft.PredicateLemma}' rámec '{label}' nemá. Na výběr je: "
                        + $"{string.Join(", ", frames.Select(frame => frame.FrameLabel ?? "bez popisku"))}.");

                return;
            }

            // Když je rámec jeden, není co vybírat; když je jeden označený jako výchozí, slovník tu
            // volbu udělal za nás. Jinak zůstane nerozhodnutá a zeptáme se — vybrat význam slovesa
            // za uživatele je přesně to, co si tenhle projekt zakazuje.
            draft.Frame = frames.Count == 1
                ? frames[0]
                : frames.FirstOrDefault(frame => frame.IsDefault);
        }

        private void AssignFunctors(ClauseDraft draft)
        {
            var open = draft.Constituents.Where(constituent => constituent.Functor is null).ToList();

            if (draft.Frame is null)
            {
                foreach (var constituent in open)
                {
                    ResolveFree(constituent, draft);
                }

                return;
            }

            var slots = draft.Frame.Slots
                .Where(slot => slot.Realizations.Any(realization => realization.Case is not null))
                .Where(slot => draft.Constituents.All(constituent => constituent.Functor != slot.Functor))
                .OrderBy(slot => slot.CanonicalOrder)
                .ToList();

            // 1. Předložka je nejsilnější vodítko: mluvit má ADDR jako 's' + instrumentál a PAT jako
            //    'o' + lokál, takže zadaná předložka slot určí sama.
            foreach (var constituent in open.Where(item => item.Preposition is not null).ToList())
            {
                var match = slots.FirstOrDefault(slot => slot.Realizations.Any(realization =>
                    string.Equals(realization.Preposition, constituent.Preposition, StringComparison.OrdinalIgnoreCase)));

                if (match is null)
                {
                    continue;
                }

                constituent.Functor = match.Functor;
                slots.Remove(match);
                open.Remove(constituent);
            }

            // 2. Konatel a adresát berou životné jméno, je-li ve zbytku právě jedno — dávat ženě knihu
            //    a ne knize ženu. Proto se obsazují dřív než ostatní sloty, jinak by jim ten jediný
            //    životný kandidát utekl podle kanonického pořadí rámce. Zbytek se páruje tak, jak byl
            //    zadaný; pes vidí kočku má životná obě a rozhodne až pořadí — a proto se to potvrzuje.
            foreach (var slot in slots.OrderBy(Priority).ThenBy(slot => slot.CanonicalOrder).ToList())
            {
                if (open.Count == 0)
                {
                    break;
                }

                var animate = open.Where(item => item.Word.IsAnimate == true).ToList();

                var chosen = Priority(slot) < 2 && animate.Count == 1
                    ? animate[0]
                    : open[0];

                chosen.Functor = slot.Functor;
                slots.Remove(slot);
                open.Remove(chosen);
            }

            // 3. Co rámec nepojmenoval, je volné určení — to k slovesu patří jakékoli, takže tady už
            //    rozhoduje jen předložka, a bez ní nic.
            foreach (var constituent in open)
            {
                ResolveFree(constituent, draft);
            }
        }

        private static int Priority(ValencySlot slot) => slot.Functor switch
        {
            FgdFunctor.ACT => 0,
            FgdFunctor.ADDR => 1,
            _ => 2,
        };

        // Volné určení rámec neřídí — to k slovesu patří jakékoli — takže pád i role musí přijít
        // odjinud a jediné vodítko je předložka: 've škole' je místo, 'do školy' směr. Bez předložky
        // se neodhaduje nic; mezi 'večer' jako časem a 'večer' jako patiensem rozhoduje význam.
        private void ResolveFree(ConstituentDraft constituent, ClauseDraft draft)
        {
            if (constituent.Preposition is not { } preposition)
            {
                return;
            }

            var allowed = _prepositions.GetAllowedCases(preposition).ToList();

            if (constituent.Word.Case is null && allowed.Count == 1)
            {
                var word = constituent.Word;
                word.Case = allowed[0];
                constituent.Word = word;
            }

            if (constituent.Word.Case is not { } kase)
            {
                draft.Notes.Add(
                    $"Předložka '{preposition}' u slova '{constituent.Lemma}' se pojí s víc pády "
                    + $"({string.Join(", ", allowed.Select(Terms.Name))}) — vyber jeden přepínačem --pad.");

                return;
            }

            constituent.Functor ??= _prepositions.GetSemanticGroup(preposition, kase) switch
            {
                PrepositionSemanticGroup.Location => FgdFunctor.LOC,
                PrepositionSemanticGroup.Direction => FgdFunctor.DIR3,
                PrepositionSemanticGroup.Time => FgdFunctor.TWHEN,
                PrepositionSemanticGroup.Cause => FgdFunctor.CAUS,
                PrepositionSemanticGroup.Purpose => FgdFunctor.AIM,
                PrepositionSemanticGroup.Instrument => FgdFunctor.MEANS,
                PrepositionSemanticGroup.Comparison => FgdFunctor.CRIT,
                _ => null,
            };
        }

        // Pád, který doplní builder z rámce, si návrh nese jen pro výpis — request ho nemá a mít nemá,
        // protože smysl rámce je právě v tom, že ho volající neuvádí.
        private static void ApplyGovernment(ClauseDraft draft)
        {
            foreach (var constituent in draft.Constituents)
            {
                var slot = draft.Frame?.Slots.FirstOrDefault(item => item.Functor == constituent.Functor);
                var realization = slot?.Realizations
                    .Where(item => item.Case is not null)
                    .Where(item => constituent.Preposition is null
                        || string.Equals(item.Preposition, constituent.Preposition, StringComparison.OrdinalIgnoreCase))
                    .MinBy(item => item.Preference);

                constituent.FrameCase = realization?.Case;
                constituent.FramePreposition = realization?.Preposition;

                // Bezpředložkový člen, kterému pád neurčuje ani rámec, by skončil v nominativu — to je
                // skoro jistě špatně, tak ať se to řekne nahlas.
                if (constituent.EffectiveCase is null && constituent.Functor is not null)
                {
                    draft.Notes.Add(
                        $"U slova '{constituent.Lemma}' neurčuje pád ani rámec, ani předložka — doplň ho "
                        + "přepínačem --pad.");
                }
            }
        }

        private static void ApplyStatus(ClauseDraft draft, DraftOverrides overrides)
        {
            for (var index = 0; index < draft.Constituents.Count; index++)
            {
                var constituent = draft.Constituents[index];
                var stated = overrides.Find(constituent.Lemma, constituent.Position)?.Status;

                // Výchozí členění je to nepříznakové: první konstituent je téma, zbytek réma. Slovosled
                // z toho builder odvodí sám, takže tohle je jediné místo, kde se o pořadí rozhoduje.
                constituent.Status = stated
                    ?? (index == 0 ? InformationStatus.Given : InformationStatus.New);
            }

            draft.SentenceType = overrides.SentenceType ?? draft.SentenceType;
            draft.Terminator = overrides.Terminator
                ?? (draft.SentenceType == SentenceType.Interrogative ? "?" : ".");
        }

        private static void ApplyPredicateCategories(ClauseDraft draft, DraftOverrides overrides)
        {
            var predicate = draft.Predicate;

            predicate.Tense = overrides.Tense ?? predicate.Tense ?? Tense.Present;
            predicate.Modus = overrides.Mood ?? predicate.Modus ?? Modus.Indicative;
            predicate.Voice = overrides.Voice ?? predicate.Voice ?? Voice.Active;
            predicate.Aspect = overrides.Aspect ?? predicate.Aspect;
            predicate.IsNegative = overrides.IsNegative ?? predicate.IsNegative;

            if (overrides.ReflexiveType is { } reflexive)
            {
                predicate.ReflexiveType = reflexive;
            }

            draft.Predicate = predicate;
        }

        private static void AgreeWithSubject(ClauseDraft draft, DraftOverrides overrides)
        {
            var predicate = draft.Predicate;
            var subject = draft.Constituents.FirstOrDefault(constituent => constituent.Functor == FgdFunctor.ACT);

            // Shodu s podmětem dělá builder; tohle je výchozí kategorie pro větu, kde podmět není —
            // a zároveň to, co se ukáže v přehledu, aby bylo vidět, v čem se to bude časovat.
            predicate.Person = overrides.Person ?? Person.Third;
            predicate.Number = overrides.Number ?? subject?.Word.Number ?? Number.Singular;
            predicate.Gender = overrides.Gender ?? subject?.Word.Gender ?? Gender.Masculine;

            draft.Predicate = predicate;
        }

        private void Report(ClauseDraft draft)
        {
            var guessed = draft.Constituents
                .Where(constituent => constituent.Origin == MetadataOrigin.Guess)
                .Select(constituent => constituent.Lemma)
                .ToList();

            if (draft.PredicateOrigin == MetadataOrigin.Guess)
            {
                guessed.Insert(0, draft.PredicateLemma);
            }

            if (guessed.Count > 0)
            {
                draft.Notes.Add(
                    $"Slovník nezná: {string.Join(", ", guessed)}. Vzor a rod jsou odhadnuté ze zakončení.");
            }

            if (draft.Frame is null && draft.FrameChoices.Count > 1)
            {
                draft.Notes.Add(
                    $"Sloveso '{draft.PredicateLemma}' má víc významů a žádný z nich není výchozí.");
            }
        }

        private sealed record ResolvedWord(
            int Position,
            string Lemma,
            CzechWordRequest Request,
            MetadataOrigin Origin,
            WordOverride? Stated);
    }
}

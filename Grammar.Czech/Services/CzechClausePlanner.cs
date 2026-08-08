using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Decides how a slot filled by a proposition comes out: as an infinitive inside the clause, or as
    /// a dependent clause behind a conjunction.
    /// </summary>
    /// <remarks>
    /// The stage above <see cref="CzechMicroplanner"/>, and it has to be a stage of its own because the
    /// choice changes the shape of the sentence rather than the shape of a word. <em>Chce jít</em> is
    /// one clause and <em>ví, že jde</em> is two, so nothing downstream can be written until it is
    /// settled — a linearizer cannot be handed a constituent that might turn out to be a clause.
    /// <para>
    /// What decides is the valency frame, not the caller: the slot records whether it takes an
    /// infinitive and which conjunction introduces it otherwise. Both were already in the dictionary
    /// and read into <see cref="SlotRealization"/>; until this class existed nothing ever looked at
    /// them, and a slot realized as anything but a case came out as a bare nominative.
    /// </para>
    /// </remarks>
    public class CzechClausePlanner
    {
        private readonly ICzechValencyService valencyService;
        private readonly ICzechConjunctionService conjunctionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechClausePlanner"/> type.
        /// </summary>
        /// <param name="valencyService">The valency service, for the frame that says how a slot surfaces.</param>
        /// <param name="conjunctionService">The conjunction service, for checking the subordinator.</param>
        public CzechClausePlanner(
            ICzechValencyService valencyService,
            ICzechConjunctionService conjunctionService)
        {
            this.valencyService = valencyService;
            this.conjunctionService = conjunctionService;
        }

        /// <summary>
        /// Resolves every propositional slot of the clause and hands back the sentence it became.
        /// </summary>
        /// <param name="clause">The clause to plan.</param>
        /// <returns>
        /// The clause itself when nothing had to move, or a <see cref="Subordination"/> when a slot
        /// turned out to be a dependent clause.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the frame gives the slot no propositional realization at all, or when an
        /// infinitive is asked for where the subjects do not corefer.
        /// </exception>
        public SentenceNode Plan(CzechClause clause)
        {
            if (clause.Elements.All(element => element.Content is null))
            {
                return new SimpleSentence(clause);
            }

            var frame = clause.Predicate.WordCategory == WordCategory.Verb
                ? valencyService.GetFrame(clause.Predicate.Lemma, clause.FrameLabel)
                : null;

            var elements = new List<ClauseElement>();
            var dependent = new List<(string Conjunction, CzechClause Clause)>();
            var climbed = ReflexiveType.None;

            foreach (var element in clause.Elements)
            {
                if (element.Content is not { } content)
                {
                    elements.Add(element);

                    continue;
                }

                var realization = Realization(frame, element, clause.Predicate.Lemma);

                if (realization.TakesInfinitive)
                {
                    var (infinitive, reflexive) = ToInfinitive(content, clause, element, frame);

                    elements.Add(element with { Content = infinitive });
                    climbed = Climb(climbed, reflexive, clause.Predicate.Lemma);

                    continue;
                }

                dependent.Add((Subordinator(realization, clause.Predicate.Lemma, element.Functor), content));
            }

            var main = clause with { Elements = elements };

            if (climbed != ReflexiveType.None)
            {
                var predicate = main.Predicate;

                if (predicate.ReflexiveType != ReflexiveType.None)
                {
                    throw new InvalidOperationException(
                        $"Zvratné je řídící sloveso '{predicate.Lemma}' i infinitiv pod ním. Klastr je "
                        + "jeden a dvě se/si do něj nepatří.");
                }

                predicate.ReflexiveType = climbed;
                main = main with { Predicate = predicate };
            }

            if (dependent.Count == 0)
            {
                return new SimpleSentence(main);
            }

            if (dependent.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Klauze slovesa '{clause.Predicate.Lemma}' má víc slotů obsazených vedlejší větou "
                    + "a stavbu s několika vedlejšími větami zatím neumím poskládat.");
            }

            // Vedlejší věta se v souvětí zavěsí za hlavní; první pozici si obsazuje spojka, což řeší
            // Subordination i s čárkou a s pohlcením kondicionálu u aby.
            return new Subordination(new SimpleSentence(main), dependent[0].Conjunction, new SimpleSentence(dependent[0].Clause));
        }

        // A propositional slot has to be licensed by the frame like any other: a verb whose patient is
        // only ever a case cannot take a clause, and saying so is more use than generating one.
        private SlotRealization Realization(ValencyFrame? frame, ClauseElement element, string verbLemma)
        {
            if (frame is null)
            {
                throw new InvalidOperationException(
                    $"Sloveso '{verbLemma}' nemá ve slovníku rámec, takže není z čeho poznat, jestli se "
                    + $"{element.Functor} vyjadřuje infinitivem, nebo vedlejší větou. Doplň heslo, nebo "
                    + "obsaď slot slovem.");
            }

            var slot = valencyService.GetSlot(frame, element.Functor)
                ?? throw new InvalidOperationException(
                    $"Sloveso '{verbLemma}' nemá slot pro funktor {element.Functor}. Rámec "
                    + $"'{frame.FrameLabel}' obsahuje: {string.Join(", ", frame.Slots.Select(s => s.Functor))}.");

            // Propozice se dá vyjádřit jen tou realizací, která ji umí nést; pádová se tu neuplatní,
            // i kdyby byla preferovaná.
            var realization = slot.Realizations
                .Where(item => item.TakesInfinitive || item.ClauseType is not null)
                .MinBy(item => item.Preference);

            return realization ?? throw new InvalidOperationException(
                $"Slot {element.Functor} slovesa '{verbLemma}' se podle slovníku vyjadřuje jen pádem, "
                + "ne vedlejší větou ani infinitivem. Obsaď ho slovem.");
        }

        private string Subordinator(SlotRealization realization, string verbLemma, FgdFunctor functor)
        {
            var conjunction = realization.ClauseType!;

            // Slovník uvádí spojku samotnou — že, aby, zda — protože ta o vazbě říká víc než druh věty:
            // 'ví, že přijde' a 'ví, zda přijde' jsou obě obsahové a znamenají každá něco jiného.
            // Tak to zapisuje i VALLEX, jehož schéma je tady předlohou.
            try
            {
                conjunctionService.GetReadings(conjunction);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Slot {functor} slovesa '{verbLemma}' má ve slovníku uvedeno '{conjunction}', což "
                    + "není spojka. Do clause_type patří spojka, kterou se ta věta uvozuje (že, aby, zda).",
                    exception);
            }

            return conjunction;
        }

        // The infinitive has no subject of its own — it is structurally excluded — so the frame records
        // which participant of the matrix clause its understood subject corefers with. Whoever wants is
        // whoever goes: "chce jít". Where the two are different people the infinitive is impossible in
        // Czech and the construction is an aby-clause instead, which is what the refusal says.
        private (CzechClause Clause, ReflexiveType Climbed) ToInfinitive(
            CzechClause content, CzechClause matrix, ClauseElement element, ValencyFrame? frame)
        {
            var controller = frame is null ? null : valencyService.GetSlot(frame, element.Functor)?.ControlTarget;

            var subject = content.Elements.FirstOrDefault(item =>
                item.Functor == FgdFunctor.ACT && (item.PhraseCase ?? item.Word.Case) is null or Case.Nominative);

            if (subject is not null)
            {
                if (controller is null)
                {
                    throw new InvalidOperationException(
                        $"Infinitiv u slovesa '{matrix.Predicate.Lemma}' nemá ve slovníku zapsanou kontrolu, "
                        + "takže není řečeno, s čím se jeho nevyjádřený podmět ztotožňuje. Podmět "
                        + "z infinitivní vazby vypusť.");
                }

                var controllerElement = matrix.Elements.FirstOrDefault(item => item.Functor == controller);

                if (!Corefers(subject, controllerElement))
                {
                    throw new InvalidOperationException(
                        $"Podmět infinitivu ('{subject.Word.Lemma}') není tentýž jako {controller} slovesa "
                        + $"'{matrix.Predicate.Lemma}', a takovou větu čeština infinitivem nevyjádří. "
                        + "Použij vedlejší větu s aby.");
                }
            }

            var predicate = content.Predicate;

            predicate.Modus = Modus.Infinitive;

            // Osoba a číslo na infinitivu nejsou tvar, ale nedorozumění: neurčitý tvar je nenese.
            predicate.Person = null;
            predicate.Number = null;
            predicate.Gender = null;
            predicate.Tense = null;

            // Klitikum infinitivu se šplhá do věty řídící: 'chce se mýt', ne 'chce mýt se'. Klastr je
            // v klauzi jeden a drží ho to sloveso, které stojí na druhé pozici.
            var climbed = predicate.ReflexiveType;
            predicate.ReflexiveType = ReflexiveType.None;

            // Podmět je koreferenční s řídící větou, takže se nevyjadřuje. Porovnává se referencí:
            // dva stejně vypadající členy jsou pro záznam totéž a hodnotová rovnost by zahodila oba.
            return (content with
            {
                Predicate = predicate,
                Elements = content.Elements.Where(item => !ReferenceEquals(item, subject)).ToList(),
            }, climbed);
        }

        private static ReflexiveType Climb(ReflexiveType carried, ReflexiveType arriving, string verbLemma)
        {
            if (arriving == ReflexiveType.None || carried == arriving)
            {
                return carried == ReflexiveType.None ? arriving : carried;
            }

            if (carried != ReflexiveType.None)
            {
                throw new InvalidOperationException(
                    $"Do klastru slovesa '{verbLemma}' se šplhá se i si z různých infinitivů, a to už "
                    + "není jeden klastr.");
            }

            return arriving;
        }

        // Coreference is decided on the lemma, which is what a generator has to go on: it is handed two
        // requests, not two entities. Same word, same person.
        private static bool Corefers(ClauseElement subject, ClauseElement? controller) =>
            controller is not null
            && string.Equals(subject.Word.Lemma, controller.Word.Lemma, StringComparison.OrdinalIgnoreCase);
    }
}

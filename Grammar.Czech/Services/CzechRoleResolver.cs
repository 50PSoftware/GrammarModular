using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Works out which participant fills which role, for a caller that knows what it wants to say but
    /// not how the Functional Generative Description names the parts of it.
    /// </summary>
    /// <remarks>
    /// A layer above <see cref="CzechSentencePlanner"/>, and deliberately separate from it: the planner
    /// takes roles as given, so everything guessed lives here where it can be inspected, tested and
    /// overruled on its own. A participant it cannot place keeps a <see langword="null"/> functor rather
    /// than a plausible one — a wrong role produces a well-formed sentence that means something else,
    /// which is worse than an unanswered question.
    /// <para>
    /// What it goes on, in this order: a preposition the caller wrote, because the frame records which
    /// slot takes which preposition and that is the strongest signal there is; then animacy for the
    /// actor and the addressee, since a recipient is typically a person and that is what keeps the two
    /// objects of a transfer verb apart; then the frame's canonical order against the order the
    /// participants were given in.
    /// </para>
    /// </remarks>
    public class CzechRoleResolver
    {
        private readonly CzechFrameSelector frameSelector;
        private readonly ICzechPrepositionService prepositionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechRoleResolver"/> type.
        /// </summary>
        /// <param name="frameSelector">The selector for the sense of the verb.</param>
        /// <param name="prepositionService">The preposition service, for government and semantic groups.</param>
        public CzechRoleResolver(
            CzechFrameSelector frameSelector,
            ICzechPrepositionService prepositionService)
        {
            this.frameSelector = frameSelector;
            this.prepositionService = prepositionService;
        }

        /// <summary>
        /// Fills in the functor of every participant that left one unstated.
        /// </summary>
        /// <param name="plan">The plan to complete.</param>
        /// <returns>The plan with the roles it could work out filled in.</returns>
        /// <remarks>
        /// A stated functor is never touched. Where the frame settles nothing the participant comes back
        /// unresolved, and <see cref="Unresolved"/> is how a caller asks which ones those are.
        /// </remarks>
        public SentencePlan Resolve(SentencePlan plan)
        {
            if (plan.Participants.All(participant => participant.Functor is not null))
            {
                return plan;
            }

            var diathesis = plan.Perspective is FgdFunctor.PAT
                ? Diathesis.PassivePeriphrastic
                : Diathesis.Active;

            var frame = frameSelector.Select(plan.Predicate.Lemma, plan.FrameLabel, diathesis).Frame;
            var resolved = plan.Participants.ToList();

            // Sloty, které rámec nabízí a nikdo si je ještě nevzal. Beze jmenného vyjádření se nepočítají:
            // infinitivní a větné realizace řeší až plánovač klauzí a role na nich je dána slotem.
            var slots = frame is null
                ? []
                : frame.Slots
                    .Where(slot => slot.Realizations.Any(realization => realization.Case is not null))
                    .Where(slot => resolved.All(participant => participant.Functor != slot.Functor))
                    .OrderBy(Priority)
                    .ThenBy(slot => slot.CanonicalOrder)
                    .ToList();

            var open = resolved
                .Select((participant, index) => (participant, index))
                .Where(item => item.participant.Functor is null && item.participant.Content is null)
                .ToList();

            ClaimByPreposition(resolved, open, slots);
            ClaimByOrder(resolved, open, slots);
            ClaimFreeModifications(resolved, open);

            return plan with { Participants = resolved };
        }

        /// <summary>
        /// Lists the participants no role could be worked out for.
        /// </summary>
        /// <param name="plan">The plan to inspect.</param>
        /// <returns>The participants still without a functor.</returns>
        public static IReadOnlyList<PlannedParticipant> Unresolved(SentencePlan plan) =>
            [.. plan.Participants.Where(participant => participant.Functor is null)];

        // A preposition the caller wrote names the slot on its own where the frame distinguishes its
        // arguments that way: mluvit takes its addressee as s + instrumental and its patient as o +
        // locative, so there is nothing left to work out.
        private static void ClaimByPreposition(
            List<PlannedParticipant> resolved,
            List<(PlannedParticipant Participant, int Index)> open,
            List<ValencySlot> slots)
        {
            foreach (var item in open.Where(item => item.Participant.Preposition is not null).ToList())
            {
                var match = slots.FirstOrDefault(slot => slot.Realizations.Any(realization =>
                    string.Equals(realization.Preposition, item.Participant.Preposition, StringComparison.OrdinalIgnoreCase)));

                if (match is null)
                {
                    continue;
                }

                resolved[item.Index] = item.Participant with { Functor = match.Functor };
                slots.Remove(match);
                open.Remove(item);
            }
        }

        // The actor goes first out of the frame's canonical order, because it prefers an animate noun and
        // the canonical order would let the only animate candidate go to a slot that does not care.
        //
        // The addressee prefers one too, but only takes it when there is one to take: a recipient is
        // typically a person, so an animate candidate settles dávat ženě knihu against dávat knize ženu
        // — and where none is left the slot sinks below the patient instead, since a three-place verb
        // used with two arguments is far likelier to be naming what than to whom. Otherwise "žák píše
        // dopis" comes out as a letter being written to.
        private static void ClaimByOrder(
            List<PlannedParticipant> resolved,
            List<(PlannedParticipant Participant, int Index)> open,
            List<ValencySlot> slots)
        {
            Claim(FgdFunctor.ACT, requiresAnimate: false);
            Claim(FgdFunctor.ADDR, requiresAnimate: true);

            // Zbytek v kanonickém pořadí rámce, jen adresát až za ostatními — na ten už zbyl leda
            // neživotný kandidát, a to je slabší čtení než patiens.
            foreach (var slot in slots
                .OrderBy(item => item.Functor == FgdFunctor.ADDR ? 1 : 0)
                .ThenBy(item => item.CanonicalOrder)
                .ToList())
            {
                if (open.Count == 0)
                {
                    break;
                }

                Take(slot, open[0]);
            }

            void Claim(FgdFunctor functor, bool requiresAnimate)
            {
                if (open.Count == 0 || slots.FirstOrDefault(slot => slot.Functor == functor) is not { } slot)
                {
                    return;
                }

                var animate = open.Where(item => item.Participant.Word.IsAnimate == true).ToList();

                // Jeden životný kandidát rozhoduje; dva už ne — pes vidí kočku má životná obě a tam
                // nezbývá než pořadí, ve kterém to volající zadal.
                if (animate.Count == 1)
                {
                    Take(slot, animate[0]);

                    return;
                }

                if (!requiresAnimate)
                {
                    Take(slot, open[0]);
                }
            }

            void Take(ValencySlot slot, (PlannedParticipant Participant, int Index) chosen)
            {
                resolved[chosen.Index] = chosen.Participant with { Functor = slot.Functor };
                slots.Remove(slot);
                open.Remove(chosen);
            }
        }

        // Whatever the frame did not account for is a free modification, which attaches to any verb at
        // all — so the frame cannot help and the only thing left to read is the preposition. Without one
        // nothing is inferred: between 'večer' as a time and 'večer' as a patient the meaning decides,
        // and the meaning is not here.
        private void ClaimFreeModifications(
            List<PlannedParticipant> resolved,
            List<(PlannedParticipant Participant, int Index)> open)
        {
            foreach (var (participant, index) in open)
            {
                if (participant.Preposition is not { } preposition)
                {
                    continue;
                }

                var word = participant.Word;
                var allowed = prepositionService.GetAllowedCases(preposition).ToList();

                // Předložka s jedinou rekcí určuje pád sama — 'do' je vždycky s genitivem. Víc možností
                // je otázka na volajícího, ne odhad.
                if (word.Case is null && allowed.Count == 1)
                {
                    word.Case = allowed[0];
                }

                if (word.Case is not { } kase)
                {
                    continue;
                }

                resolved[index] = participant with
                {
                    Word = word,
                    Functor = FromSemanticGroup(prepositionService.GetSemanticGroup(preposition, kase)),
                };
            }
        }

        private static FgdFunctor? FromSemanticGroup(PrepositionSemanticGroup? group) => group switch
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

        private static int Priority(ValencySlot slot) => slot.Functor switch
        {
            FgdFunctor.ACT => 0,
            FgdFunctor.ADDR => 1,
            _ => 2,
        };
    }
}

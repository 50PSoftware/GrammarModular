using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Turns what is to be said into a Czech clause: which sense of the verb, which participant is the
    /// subject, whether it is expressed at all, and what counts as old information.
    /// </summary>
    /// <remarks>
    /// The top of the pipeline. It answers the questions a speaker settles before any Czech is chosen,
    /// and hands the result to <see cref="CzechClausePlanner"/>, which decides what shape each slot
    /// takes, and so on down to the surface.
    /// <para>
    /// Roles are its input rather than its output. A plan whose participants have no functors is
    /// refused, and <see cref="CzechRoleResolver"/> is the separate stage that fills them, so that
    /// everything worked out by guesswork stays where it can be checked before it turns into a
    /// sentence.
    /// </para>
    /// </remarks>
    public class CzechSentencePlanner
    {
        private readonly CzechFrameSelector frameSelector;
        private readonly ICzechValencyService valencyService;
        private readonly ICzechPronounService pronounService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechSentencePlanner"/> type.
        /// </summary>
        /// <param name="frameSelector">The selector for the sense of the verb.</param>
        /// <param name="valencyService">The valency service, for what a frame licenses.</param>
        /// <param name="pronounService">The pronoun service, for recognizing a droppable subject.</param>
        public CzechSentencePlanner(
            CzechFrameSelector frameSelector,
            ICzechValencyService valencyService,
            ICzechPronounService pronounService)
        {
            this.frameSelector = frameSelector;
            this.valencyService = valencyService;
            this.pronounService = pronounService;
        }

        /// <summary>
        /// Plans the sentence into the clause the rest of the pipeline builds from.
        /// </summary>
        /// <param name="plan">What is to be said.</param>
        /// <returns>The clause.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a participant has no functor, when the verb's sense is not settled, when a
        /// functor the verb has no slot for is used, or when the requested perspective is one the verb
        /// cannot take.
        /// </exception>
        /// <summary>
        /// Fills in what the plan left unsaid, without building anything from it.
        /// </summary>
        /// <param name="plan">The plan to complete.</param>
        /// <returns>The plan with the unmarked values filled in.</returns>
        /// <remarks>
        /// Separate from <see cref="Plan"/> so a caller can show what it is about to build and have that
        /// be the same thing. A tool that offered its reading for confirmation and then restated the
        /// defaults itself would have two copies of the rule and no way to notice them drifting.
        /// <para>
        /// It only writes where the plan holds <see langword="null"/>, so running it twice changes
        /// nothing and <see cref="Plan"/> can call it whether or not the caller already did.
        /// </para>
        /// </remarks>
        public SentencePlan Complete(SentencePlan plan)
        {
            var voice = ResolveVoice(plan);
            var participants = ApplyDefaultPerspective(plan);

            var subject = participants.FirstOrDefault(participant =>
                participant.Functor == (voice == Voice.Passive ? FgdFunctor.PAT : FgdFunctor.ACT)
                && participant.Content is null);

            return plan with
            {
                Participants = participants,
                Predicate = WithDefaults(plan.Predicate, voice, subject),
            };
        }

        /// <summary>
        /// Plans the sentence into the clause the rest of the pipeline builds from.
        /// </summary>
        /// <param name="plan">What is to be said.</param>
        /// <returns>The clause.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a participant has no functor, when the verb's sense is not settled, when a
        /// functor the verb has no slot for is used, or when the requested perspective is one the verb
        /// cannot take.
        /// </exception>
        public CzechClause Plan(SentencePlan plan)
        {
            plan = Complete(plan);

            var voice = ResolveVoice(plan);
            var selection = frameSelector.Select(
                plan.Predicate.Lemma,
                plan.FrameLabel,
                voice == Voice.Passive ? Diathesis.PassivePeriphrastic : Diathesis.Active);

            if (selection.IsAmbiguous)
            {
                throw new InvalidOperationException(
                    $"Sloveso '{plan.Predicate.Lemma}' má víc významů a slovník žádný neoznačuje za "
                    + $"výchozí: {selection.DescribeChoices()}. Vyber jeden přes {nameof(SentencePlan.FrameLabel)}.");
            }

            ValidateFunctors(plan, selection);

            var participants = plan.Participants.ToList();
            var predicate = DropSubject(plan, participants, voice);

            return new CzechClause
            {
                Predicate = predicate,
                Elements = [.. participants.Select(participant => participant.ToElement())],
                FrameLabel = selection.Frame?.FrameLabel,
                SentenceType = plan.SentenceType,
                Terminator = plan.Terminator,
                Particle = plan.Particle,
                Interjection = plan.Interjection,
            };
        }

        // The perspective is a communicative choice — what the sentence is about — and the voice follows
        // from it. Asking for the patient to be the subject is asking for the passive, which in Czech is
        // a frame of its own rather than the active one turned round.
        private Voice ResolveVoice(SentencePlan plan)
        {
            if (plan.Predicate.Voice is { } stated)
            {
                return stated;
            }

            if (plan.Perspective is not { } perspective || perspective == FgdFunctor.ACT)
            {
                return Voice.Active;
            }

            if (perspective != FgdFunctor.PAT)
            {
                throw new InvalidOperationException(
                    $"Podmětem se dá udělat konatel nebo patiens, ne {perspective}. Čeština pro ostatní "
                    + "funktory diatézi nemá.");
            }

            var active = frameSelector.Select(plan.Predicate.Lemma, plan.FrameLabel).Frame;

            // Rámec bez pasivní diateze ještě neznamená, že sloveso pasivum nemá — u hesla, které
            // slovník nevede, se nedá říct nic. Odmítá se jen to, o čem rámec mluví.
            if (active is not null && !valencyService.LicensesPeriphrasticPassive(active))
            {
                throw new InvalidOperationException(
                    $"Sloveso '{plan.Predicate.Lemma}' se v tomhle významu do trpného rodu nepřevede, "
                    + "takže patiens jeho podmětem být nemůže.");
            }

            return Voice.Passive;
        }

        // An inner participant belongs to the verb, so one the frame has no slot for cannot be said with
        // that verb at all. Caught here rather than three stages later, where the message would be about
        // a clause the caller never wrote.
        private void ValidateFunctors(SentencePlan plan, FrameSelection selection)
        {
            foreach (var participant in plan.Participants)
            {
                if (participant.Functor is not { } functor)
                {
                    throw new InvalidOperationException(
                        $"Participant '{Describe(participant)}' nemá roli. Doplň ji, nebo nech plán projít "
                        + $"{nameof(CzechRoleResolver)}em, který ji odvodí z rámce.");
                }

                if (selection.Frame is { } frame
                    && valencyService.IsInnerParticipant(functor)
                    && valencyService.GetSlot(frame, functor) is null)
                {
                    throw new InvalidOperationException(
                        $"Sloveso '{plan.Predicate.Lemma}' nemá slot pro funktor {functor}. Rámec "
                        + $"'{frame.FrameLabel}' obsahuje: {string.Join(", ", frame.Slots.Select(slot => slot.Functor))}.");
                }
            }
        }

        // The unmarked reading: the sentence starts from one participant and the rest is what it says
        // about it. Which one that is follows the perspective where there is one — asking for the
        // patient to be the subject is asking for the sentence to be about the patient, and a passive
        // that left the agent in front of it would have gained nothing over the active. Otherwise it is
        // the participant that was given first.
        //
        // Only participants that stated nothing are touched: saying Given of everything is a claim about
        // the discourse, and not saying it is not.
        private static List<PlannedParticipant> ApplyDefaultPerspective(SentencePlan plan)
        {
            var participants = plan.Participants.ToList();

            var theme = plan.Perspective is { } perspective
                ? participants.FindIndex(participant => participant.Functor == perspective)
                : 0;

            for (var index = 0; index < participants.Count; index++)
            {
                if (participants[index].Status is null)
                {
                    participants[index] = participants[index] with
                    {
                        Status = index == theme ? InformationStatus.Given : InformationStatus.New,
                    };
                }
            }

            return participants;
        }

        // Czech leaves the subject pronoun out unless it is doing work: the ending already carries the
        // person, so "čtu" is the neutral sentence and "já čtu" is emphasis. Dropping it means the
        // predicate has to take over the agreement the pronoun was carrying, which is why the categories
        // move across rather than the element simply being deleted.
        private CzechWordRequest DropSubject(
            SentencePlan plan, List<PlannedParticipant> participants, Voice voice)
        {
            var subjectFunctor = voice == Voice.Passive ? FgdFunctor.PAT : FgdFunctor.ACT;

            var subject = participants.FirstOrDefault(participant =>
                participant.Functor == subjectFunctor
                && participant.Content is null
                && participant.Word.Case is null or Case.Nominative);

            if (subject is null || !plan.AllowSubjectDrop || !IsDroppable(subject))
            {
                return plan.Predicate;
            }

            var predicate = plan.Predicate;

            // Kategorie se přenesou dřív, než podmět zmizí: on je nesl a po jeho vypuštění by se
            // přísudek neměl s čím shodovat. Complete je vyplnil výchozími, tak se tady přepíšou.
            predicate.Person = PersonOf(subject.Word.Lemma);
            predicate.Number = subject.Word.Number ?? predicate.Number;
            predicate.Gender = subject.Word.Gender ?? predicate.Gender;

            participants.Remove(subject);

            return predicate;
        }

        // Only a personal pronoun drops, and only when it is not being contrasted: "on čte, ona píše"
        // needs both of them said. A noun never drops — nothing else in the sentence would name it.
        private bool IsDroppable(PlannedParticipant subject) =>
            subject.Word.WordCategory == WordCategory.Pronoun
            && subject.Status is not InformationStatus.Contrastive and not InformationStatus.Interrogative
            && subject.Modifiers.Count == 0
            && pronounService.GetPronounType(subject.Word.Lemma) == PronounType.Personal;

        private static Person PersonOf(string lemma) => lemma switch
        {
            "já" or "my" => Person.First,
            "ty" or "vy" => Person.Second,
            _ => Person.Third,
        };

        // Whatever the plan did not state about the predicate takes the unmarked value. Person, number
        // and gender are the fallback for a clause with no subject to agree with — the microplanner
        // overwrites them from the subject wherever there is one.
        private static CzechWordRequest WithDefaults(
            CzechWordRequest predicate, Voice voice, PlannedParticipant? subject)
        {
            predicate.Voice ??= voice;
            predicate.Modus ??= Modus.Indicative;
            predicate.Tense ??= Tense.Present;
            predicate.Person ??= Person.Third;
            predicate.Number ??= subject?.Word.Number ?? Number.Singular;
            predicate.Gender ??= subject?.Word.Gender ?? Gender.Masculine;

            return predicate;
        }

        private static string Describe(PlannedParticipant participant) =>
            participant.Content is not null ? participant.Content.Predicate.Lemma : participant.Word.Lemma;
    }
}

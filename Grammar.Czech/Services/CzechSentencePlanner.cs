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
        private readonly ICzechConjunctionService conjunctionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechSentencePlanner"/> type.
        /// </summary>
        /// <param name="frameSelector">The selector for the sense of the verb.</param>
        /// <param name="valencyService">The valency service, for what a frame licenses.</param>
        /// <param name="pronounService">The pronoun service, for recognizing a droppable subject.</param>
        /// <param name="conjunctionService">The conjunction service, for how a joined clause attaches.</param>
        public CzechSentencePlanner(
            CzechFrameSelector frameSelector,
            ICzechValencyService valencyService,
            ICzechPronounService pronounService,
            ICzechConjunctionService conjunctionService)
        {
            this.frameSelector = frameSelector;
            this.valencyService = valencyService;
            this.pronounService = pronounService;
            this.conjunctionService = conjunctionService;
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
        public SentencePlan Complete(SentencePlan plan) => Complete(plan, themeTaken: false);

        // A relative clause opens with its pronoun, and that pronoun is what the clause is about — the
        // theme is spoken for before any participant is reached. So nothing inside becomes the theme by
        // default, and "který píše dopis" comes out in that order rather than as "který dopis píše",
        // which is a marked reading nobody asked for.
        private SentencePlan Complete(SentencePlan plan, bool themeTaken)
        {
            // Dřív než cokoli jiného: plán, který si odporuje sám se sebou, má o tom slyšet, a ne dostat
            // hlášku o slovese, které za nic nemůže.
            _ = CzechRoleResolver.DiathesisOf(plan);

            var voice = ResolveVoice(plan);
            var participants = ApplyDefaultPerspective(plan, themeTaken);

            var subject = participants.FirstOrDefault(participant =>
                participant.Functor == (voice == Voice.Passive ? FgdFunctor.PAT : FgdFunctor.ACT)
                && participant.Content is null);

            return plan with
            {
                // Vztažná věta se doplňuje taky — je to plán jako každý jiný a přehled, který ji chce
                // ukázat, ji potřebuje hotovou stejně jako klauzi, na které visí.
                Participants =
                [
                    .. participants.Select(participant => participant.Relative is { } relative
                        ? participant with { Relative = relative with { Clause = Complete(relative.Clause, themeTaken: true) } }
                        : participant),
                ],
                Predicate = WithDefaults(plan.Predicate, voice, subject, IsImpersonal(plan, voice)),
                // Způsob, který řídí spojka, se doplní dřív, než se doplní ten výchozí — jinak by
                // oznamovací způsob obsadil mezeru, kterou má vyplnit kondicionál z 'aby'.
                //
                // Obsazené téma se dědí do souřadné klauze ze stejného důvodu, z jakého se do ní dědí
                // rezervovaný slot v CzechRoleResolveru: jedno vztažné zájmeno je podmětem všeho, co
                // s ním souřadí, takže i tam je téma vyslovené dřív, než se dojde k participantům.
                // Bez toho vycházelo 'který čte knihu a dopis píše' — druhý konjunkt si za téma vzal
                // svůj vlastní první člen. Podřadicí spojka otevírá klauzi s vlastním podmětem
                // a dědění na ní končí.
                Joined =
                [
                    .. plan.Joined.Select(link => link with
                    {
                        Clause = Complete(
                            WithGovernedMood(link.Conjunction, link.Clause),
                            themeTaken
                                && conjunctionService.GetType(link.Conjunction) == ConjunctionType.Coordinating),
                    }),
                ],
            };
        }

        /// <summary>
        /// Plans the sentence into the tree of clauses the rest of the pipeline builds from.
        /// </summary>
        /// <param name="plan">What is to be said.</param>
        /// <returns>
        /// A single clause when nothing is joined to it, and a coordination or a subordination when
        /// something is.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a participant has no functor, when the verb's sense is not settled, when a
        /// functor the verb has no slot for is used, or when the requested perspective is one the verb
        /// cannot take.
        /// </exception>
        public SentenceNode Plan(SentencePlan plan)
        {
            // Doplněno na začátku, protože smyčka níž prochází Joined a potřebuje ty klauze už s tím,
            // co jim spojka nad nimi řídí.
            plan = Complete(plan);

            SentenceNode node = new SimpleSentence(PlanClause(plan));

            foreach (var link in plan.Joined)
            {
                node = Join(node, link);
            }

            return node;
        }

        // The conjunction decides how the two are joined, because that is what a conjunction is. Reading
        // it off the data rather than asking also means a caller cannot write "subordinate them with a"
        // — a combination the grammar has no reading for.
        private SentenceNode Join(SentenceNode node, ClauseLink link)
        {
            var joined = Plan(link.Clause);

            if (conjunctionService.GetType(link.Conjunction) == ConjunctionType.Subordinating)
            {
                return new Subordination(node, link.Conjunction, joined);
            }

            // Three clauses on one conjunction are one coordination, not two nested ones: "přišel,
            // viděl a zvítězil" has a single relation running through it, and nesting would punctuate
            // the inner one as though it were a member of the outer.
            if (node is Coordination running
                && string.Equals(running.Conjunction, link.Conjunction, StringComparison.Ordinal)
                && !running.Paired
                && !link.Paired)
            {
                return running with { Conjuncts = [.. running.Conjuncts, joined] };
            }

            return new Coordination(
                link.Conjunction, [node, joined], link.RequiresComma, link.Paired, link.AllowVerbEllipsis);
        }

        /// <summary>
        /// Applies the mood the conjunction governs to the clause it introduces.
        /// </summary>
        /// <param name="conjunction">The conjunction attaching the clause, or null when none does.</param>
        /// <param name="clause">The clause being attached.</param>
        /// <returns>The clause with the governed mood applied.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the clause states a mood the conjunction cannot govern.
        /// </exception>
        /// <remarks>
        /// aby and kdyby have the conditional auxiliary welded into them — abych, abys, aby — so the verb
        /// under them is an l-participle and the clause is in the conditional whether the caller said so
        /// or not. The conjunction data already knows which ones do this, because the renderer has to
        /// suppress the particle they carry; asking the same question here is what stops "aby zpívá".
        /// <para>
        /// Public because it has to run before <see cref="Complete"/> and a caller that completes its
        /// clauses one at a time — as a tool showing them separately does — has nowhere else to ask.
        /// Filling the gap with the indicative first would turn it into a contradiction.
        /// </para>
        /// </remarks>
        public SentencePlan WithGovernedMood(string? conjunction, SentencePlan clause) =>
            conjunction is not null && conjunctionService.FusesWithConditional(conjunction)
                ? WithConditional(conjunction, clause)
                : clause;

        // Coordination joins equals, so a clause coordinated with a conditional one is conditional too:
        // "aby žák psal dopis a lékař zpíval píseň" has one aby carrying the auxiliary for both. A
        // subordinator inside opens a domain of its own and stops it — "aby psal, když zpívá" is not a
        // wish about the singing.
        private SentencePlan WithConditional(string conjunction, SentencePlan clause)
        {
            if (clause.Predicate.Modus is { } stated && stated != Modus.Conditional)
            {
                throw new InvalidOperationException(
                    $"Spojka '{conjunction}' řídí podmiňovací způsob, ale věta pod ní má {stated}. "
                    + "Kondicionál je v té spojce už obsažený, takže jiný způsob za ní stát nemůže.");
            }

            var predicate = clause.Predicate;
            predicate.Modus = Modus.Conditional;

            return clause with
            {
                Predicate = predicate,
                Joined =
                [
                    .. clause.Joined.Select(link =>
                        conjunctionService.GetType(link.Conjunction) == ConjunctionType.Coordinating
                            ? link with { Clause = WithConditional(conjunction, link.Clause) }
                            : link),
                ],
            };
        }

        private bool IsImpersonal(SentencePlan plan, Voice voice) =>
            frameSelector.Select(
                plan.Predicate.Lemma,
                plan.FrameLabel,
                CzechRoleResolver.Companions(plan),
                DiathesisFor(plan, voice))
                .Frame?.Kind == ValencyKind.Impersonal;

        // Hlas říká jen to, jestli je to opisné pasivum. Deagentiv ani dispoziční diateze hlas nemění —
        // sloveso zůstává v činném tvaru a nese zvratné se — takže je z něj nepoznáš a musí je říct plán.
        private static Diathesis DiathesisFor(SentencePlan plan, Voice voice) =>
            plan.Diathesis is { } stated && stated != Diathesis.Active
                ? stated
                : voice == Voice.Passive ? Diathesis.PassivePeriphrastic : Diathesis.Active;

        // A relative clause is planned like the sentence it is: its own roles, its own sense of its own
        // verb, its own subject drop. Which is why it goes through Plan and not through some smaller
        // path — the only thing it does not have is a life outside the participant it hangs off.
        private RelativeAttachment? PlanRelative(PlannedParticipant participant) =>
            participant.Relative is not { } relative
                ? null
                : new RelativeAttachment
                {
                    Relativizer = relative.Relativizer,
                    Case = relative.Case,
                    Possessed = relative.Possessed,
                    Clause = Plan(WithPossessive(relative)),
                };

        // Přivlastňovací vztažné zájmeno není argument své věty, ale shodný přívlastek jednoho z nich, tak
        // se jím i stane: 'jejíž' se ke svému jménu chová jako 'mladý' a shodu s ním obstará táž cesta,
        // která ji obstarává každému jinému přívlastku. Zbývá jen pořadí — celý ten člen otevírá vztažnou
        // větu, protože ji otevírá zájmeno v něm — a to se řekne tématem a přesunutím na začátek.
        private SentencePlan WithPossessive(PlannedRelative relative)
        {
            if (!pronounService.IsPossessiveRelative(relative.Relativizer))
            {
                return relative.Clause;
            }

            // Které jméno přivlastňuje, z toho slova nevyplývá a uhodnout to nejde: 'žena, jejíž dům
            // student koupil' i '…, jejíhož studenta dům viděl' jsou obě věty. Musí to říct volající.
            if (relative.Possessed is not { } possessed)
            {
                throw new InvalidOperationException(
                    $"Přivlastňovací vztažné zájmeno '{relative.Relativizer}' neříká, který participant "
                    + $"přivlastňuje. Doplň {nameof(PlannedRelative.Possessed)}.");
            }

            var participants = relative.Clause.Participants.ToList();
            var index = participants.FindIndex(participant => participant.Functor == possessed);

            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"Vztažné zájmeno '{relative.Relativizer}' přivlastňuje {possessed}, ale vztažná věta "
                    + $"takový participant nemá.");
            }

            var owner = participants[index];

            participants.RemoveAt(index);
            participants.Insert(0, owner with
            {
                Modifiers =
                [
                    new CzechWordRequest
                    {
                        Lemma = relative.Relativizer,
                        WordCategory = WordCategory.Pronoun,
                    },
                    .. owner.Modifiers,
                ],
                Status = InformationStatus.Given,
            });

            return relative.Clause with { Participants = participants };
        }

        private CzechClause PlanClause(SentencePlan plan)
        {
            plan = Complete(plan);

            var voice = ResolveVoice(plan);
            var selection = frameSelector.Select(
                plan.Predicate.Lemma,
                plan.FrameLabel,
                CzechRoleResolver.Companions(plan),
                DiathesisFor(plan, voice));

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
                Elements = [.. participants.Select(participant => participant.ToElement(PlanRelative(participant)))],
                FrameLabel = selection.Frame?.FrameLabel,
                Diathesis = DiathesisFor(plan, voice),
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
                    // Bezpodměťové sloveso nemá kam participanta zařadit, a to je o něm to podstatné —
                    // říct místo toho jen 'nemá roli' by vypadalo, že se něco zapomnělo doplnit.
                    var alternative = selection.Choices
                        .FirstOrDefault(frame => frame.Kind != ValencyKind.Impersonal)?.FrameLabel;

                    throw new InvalidOperationException(selection.Frame?.Kind == ValencyKind.Impersonal
                        ? $"Sloveso '{plan.Predicate.Lemma}' je bezpodměťové a žádný účastník k němu "
                            + $"nepatří, takže '{Describe(participant)}' nemá kam."
                            + (alternative is null
                                ? string.Empty
                                : $" Význam '{alternative}' účastníka bere — vyber ho přes {nameof(SentencePlan.FrameLabel)}.")
                        : $"Participant '{Describe(participant)}' nemá roli. Doplň ji, nebo nech plán projít "
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
        private static List<PlannedParticipant> ApplyDefaultPerspective(SentencePlan plan, bool themeTaken)
        {
            var participants = plan.Participants.ToList();

            var theme = themeTaken
                ? -1
                : plan.Perspective is { } perspective
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
            CzechWordRequest predicate, Voice voice, PlannedParticipant? subject, bool impersonal)
        {
            predicate.Voice ??= voice;
            predicate.Modus ??= Modus.Indicative;
            predicate.Tense ??= Tense.Present;
            predicate.Person ??= Person.Third;
            predicate.Number ??= subject?.Word.Number ?? Number.Singular;

            // A verb with no participants has nothing to agree with, and Czech puts the participle in
            // the neuter singular for it: pršelo, sněžilo, svítalo. The masculine default is agreement
            // with a subject, and here there is none to be had.
            predicate.Gender ??= impersonal
                ? Gender.Neuter
                : subject?.Word.Gender ?? Gender.Masculine;

            return predicate;
        }

        private static string Describe(PlannedParticipant participant) =>
            participant.Content is not null ? participant.Content.Predicate.Lemma : participant.Word.Lemma;
    }
}

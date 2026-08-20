using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Settles every grammatical category of a clause: what the valency frame governs, what a numeral
    /// rewrites, what the predicate agrees with, and which reflexive particle the verb carries.
    /// </summary>
    /// <remarks>
    /// The stage between planning a clause and linearizing it. It answers what the words are; nothing
    /// here decides what order they come in, and <see cref="CzechWordOrderResolver"/> decides nothing
    /// but that.
    /// <para>
    /// The three passes run in this order because each feeds the next: the frame decides the case the
    /// phrase stands in, a cardinal numeral rewrites the head's case off the back of that, and subject
    /// agreement has to see the result — "pět studentů přišlo" agrees with a phrase whose head is
    /// genitive.
    /// </para>
    /// </remarks>
    public class CzechMicroplanner
    {
        private readonly ICzechValencyService valencyService;
        private readonly ICzechNumeralService numeralService;
        private readonly ICzechParticleService particleService;
        private readonly ICzechInterjectionService interjectionService;
        private readonly CzechLexiconEnricher lexiconEnricher;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechMicroplanner"/> type.
        /// </summary>
        /// <param name="valencyService">The valency service, for the frame that governs the arguments.</param>
        /// <param name="numeralService">The numeral service, for cardinal government.</param>
        /// <param name="particleService">The particle service, for the checks a particle can be held to.</param>
        /// <param name="interjectionService">The interjection service, for the comma rule.</param>
        /// <param name="lexiconEnricher">The enricher, for reflexivity stated on the dictionary entry.</param>
        public CzechMicroplanner(
            ICzechValencyService valencyService,
            ICzechNumeralService numeralService,
            ICzechParticleService particleService,
            ICzechInterjectionService interjectionService,
            CzechLexiconEnricher lexiconEnricher)
        {
            this.valencyService = valencyService;
            this.numeralService = numeralService;
            this.particleService = particleService;
            this.interjectionService = interjectionService;
            this.lexiconEnricher = lexiconEnricher;
        }

        /// <summary>
        /// Applies government and agreement to the clause and hands back both the result and the
        /// predicate that agreement produced.
        /// </summary>
        /// <param name="clause">The clause to plan.</param>
        /// <param name="firstPositionTaken">
        /// Whether something outside the clause already fills its first position, which is checked
        /// against a fronted interrogative.
        /// </param>
        /// <returns>The planned clause.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the clause states something the grammar refuses: a functor the verb has no slot
        /// for, a passive its frame does not license, a subjectless clause with no person on the
        /// predicate, or a particle used where its data says it cannot stand.
        /// </exception>
        public PlannedClause Plan(CzechClause clause, bool firstPositionTaken)
        {
            clause = ApplyValencyFrame(clause);

            // Has to sit between the two: the frame decides what case the phrase stands in, and the numeral
            // rewrites the head's case off the back of it — which subject agreement then has to see.
            clause = ApplyCardinalGovernment(clause);

            var predicate = ApplySubjectAgreement(clause);

            ValidateSentenceType(clause, firstPositionTaken);
            ValidateParticles(clause);

            return new PlannedClause(clause, predicate);
        }

        // The two checks the particle data supports and the clause can actually be measured against.
        private void ValidateParticles(CzechClause clause)
        {
            if (clause.Particle is { } particle)
            {
                if (!particleService.IsParticle(particle) || !particleService.IsClauseInitial(particle))
                {
                    throw new InvalidOperationException(
                        $"'{particle}' není větná částice, která uvozuje klauzi. Sem patří ať, kéž, nechť "
                        + "nebo nuže; částici s dosahem na jeden člen dej na ClauseElement.Particle.");
                }

                // The mood is deliberately unchecked: "Ať přijde" is a plain third-person present, and
                // NESČ states no mood government for the optative group at all.
            }

            if (clause.Interjection is { } interjection && interjectionService.IsInterjection(interjection)
                && !interjectionService.RequiresComma(interjection, asPredicate: false))
            {
                throw new InvalidOperationException(
                    $"Citoslovce '{interjection}' se tu neodděluje čárkou, což tenhle slot neumí vyjádřit.");
            }

            // A modifying particle carries no stress of its own, so it cannot be the rheme — NESČ states
            // it of the whole group, and Status makes it checkable rather than merely documented.
            foreach (var element in clause.Elements)
            {
                if (element.Particle is not { } scoped || !particleService.IsParticle(scoped))
                {
                    continue;
                }

                if (element.Status == InformationStatus.New && !particleService.CanStandInRheme(scoped))
                {
                    throw new InvalidOperationException(
                        $"Modifikační částice '{scoped}' nemůže stát v rématu. Buď dej konstituentu jiný "
                        + "InformationStatus, nebo použij vytýkací částici (jen, právě, dokonce).");
                }
            }
        }

        // The frame says how each argument is realized, so the caller states the functor and the word and
        // the case follows. An explicit case wins — the frame fills gaps, it does not overrule.
        private CzechClause ApplyValencyFrame(CzechClause clause)
        {
            if (clause.Predicate.WordCategory != WordCategory.Verb)
            {
                return clause;
            }

            var frame = valencyService.GetFrame(clause.Predicate.Lemma, clause.FrameLabel, clause.Diathesis);

            // A diathesis remaps every slot at once, so the passive is a frame of its own rather than the
            // active one recomputed: ACT drops to an instrumental adjunct and PAT rises to the subject.
            var passive = clause.Predicate.Voice == Voice.Passive
                ? valencyService.GetFrame(clause.Predicate.Lemma, clause.FrameLabel, Diathesis.PassivePeriphrastic)
                : null;

            // Having a passive frame is the licence, and it is the better answer of the two because it also
            // says how the arguments come out. The check speaks only for a sense that has none yet.
            if (clause.Predicate.Voice == Voice.Passive && passive is null)
            {
                ValidatePassive(clause.Predicate, frame);
            }

            var governing = passive ?? frame;

            var predicate = WithReflexive(clause.Predicate, governing);

            // Mít/dostat + příčestí keeps the active voice — the auxiliary changes, not the frame's
            // mapping of arguments — so this cannot be read off Voice the way the periphrastic passive
            // is; the composer needs it carried on the predicate itself.
            if (clause.Diathesis is Diathesis.Resultative or Diathesis.RecipientDeobjective)
            {
                predicate.Diathesis = clause.Diathesis;
            }

            return clause with
            {
                Predicate = predicate,
                Elements = clause.Elements.Select(element => ApplySlot(element, governing, clause.Predicate.Lemma)).ToList()
            };
        }

        // Which verbs can stand in the passive is a valency question, and the frame is where valency is
        // written down — so it is asked here rather than answered a second time somewhere else.
        //
        // A verb the lexicon does not know has no frame and is left alone. Silence is not a refusal, the
        // same rule the reflexive particle follows: a caller who works from a vzor and never opens the
        // dictionary has to keep working.
        private void ValidatePassive(CzechWordRequest predicate, ValencyFrame? frame)
        {
            if (predicate.Voice != Voice.Passive
                || frame is null
                || valencyService.LicensesPeriphrasticPassive(frame))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Sloveso '{predicate.Lemma}' se v tomhle významu do trpného rodu nedá převést. Trpný "
                + "rod chce konatele a aspoň jedno pravé doplnění, a rámec "
                + $"'{frame.FrameLabel ?? "bez popisku"}' má jen konatele — směr ani místo se nepočítají, "
                + "ty bere každé sloveso. Tvar příčestí existovat může; věta z něj ne.");
        }

        // Precedence: caller, then frame, then entry. None doubles as "not stated" at every step, the
        // same rule CzechLexiconEnricher follows — the frame speaks for one sense (dát si kávu, but
        // dát knihu Pavlovi), the entry for the whole lemma (starat se, which has no other form).
        //
        // The entry is read here rather than left to the enricher, because that one runs inside
        // MorphologyEngine on a copy of the request that never comes back, and the clitic cluster is
        // assembled before it. Anything set there would reach the verb form and nothing else.
        private CzechWordRequest WithReflexive(CzechWordRequest predicate, ValencyFrame? frame)
        {
            if (predicate.ReflexiveType == ReflexiveType.None && frame is not null)
            {
                predicate.ReflexiveType = frame.ReflexiveType;
            }

            if (predicate.ReflexiveType == ReflexiveType.None)
            {
                predicate.ReflexiveType = lexiconEnricher.Enrich(predicate).ReflexiveType;
            }

            return predicate;
        }

        private ClauseElement ApplySlot(ClauseElement element, ValencyFrame? frame, string verbLemma)
        {
            // A vocative is address, not an argument: no verb governs it and no frame licenses it.
            // Checking it against one rejected every imperative naming its addressee.
            if (element.Word.Case == Case.Vocative)
            {
                return element;
            }

            var slot = frame is null ? null : valencyService.GetSlot(frame, element.Functor);

            // An inner participant belongs to the verb, so a verb with no slot for it cannot take it at all.
            // Free modifications attach to any verb and are never licensed by a frame.
            if (slot is null && frame is not null && valencyService.IsInnerParticipant(element.Functor))
            {
                throw new InvalidOperationException(
                    $"Sloveso '{verbLemma}' nemá slot pro funktor {element.Functor}. Rámec '{frame.FrameLabel}' obsahuje: "
                    + string.Join(", ", frame.Slots.Select(s => s.Functor)) + ".");
            }

            if (slot is null || element.Word.Case is not null)
            {
                return element;
            }

            var realization = slot.PreferredRealization;

            // A slot may be realized as a clause or an infinitive instead of a case — říct to against
            // říct, že přijde — and building those is the clause planner's, which has resolved them into
            // ordinary constituents before this runs.
            if (realization?.Case is not { } governedCase)
            {
                return element;
            }

            var word = element.Word;
            word.Case = governedCase;

            return element with
            {
                Word = word,
                Preposition = element.Preposition ?? realization.Preposition
            };
        }

        // The one place Czech runs agreement backwards: a cardinal from five up forces the noun it counts
        // into the genitive plural and the predicate into the neuter singular — pět žáků bylo.
        private CzechClause ApplyCardinalGovernment(CzechClause clause) =>
            clause with { Elements = clause.Elements.Select(GovernByCardinal).ToList() };

        private ClauseElement GovernByCardinal(ClauseElement element)
        {
            var index = element.Modifiers
                .Select((modifier, position) => (modifier, position))
                .Where(candidate => candidate.modifier.WordCategory == WordCategory.Numerale)
                .Select(candidate => (int?)candidate.position)
                .FirstOrDefault();

            if (index is null)
            {
                return element;
            }

            var numeral = element.Modifiers[index.Value];
            var agreement = numeralService.GetAgreement(numeral.Lemma);

            // An ordinal is an ordinary agreeing attribute and wants the normal head-to-modifier path.
            if (agreement == CardinalAgreement.None)
            {
                return element;
            }

            var head = element.Word;
            var phraseCase = head.Case ?? Case.Nominative;

            // The numeral carries the case of the whole phrase. Setting it here also keeps AgreeWithHead off
            // it later, since that only fills categories still unset.
            numeral.Case = phraseCase;
            numeral.Gender ??= head.Gender;
            numeral.IsAnimate ??= head.IsAnimate;
            numeral.Number ??= head.Number;

            var isCountable = head.IsCountable ?? true;

            (head.Case, head.Number) = numeralService.ResolveCountedForm(agreement, phraseCase, isCountable);

            var modifiers = element.Modifiers.ToList();
            modifiers[index.Value] = numeral;

            // An uncountable noun under mnoho ends up in the genitive singular, which is a different
            // agreement from the one the lemma carries; the predicate has to be told the one that applied.
            var effective = agreement == CardinalAgreement.GenitivePluralInDirectCases && !isCountable
                ? CardinalAgreement.GenitiveSingular
                : agreement;

            return element with
            {
                Word = head,
                Modifiers = modifiers,
                PhraseCase = phraseCase,
                Agreement = effective
            };
        }

        // A wh-question fronts exactly one element, and the caller says which. Getting this wrong produces a
        // grammatical sentence with the wrong force rather than a visible failure, so it is checked.
        private static void ValidateSentenceType(CzechClause clause, bool firstPositionTaken)
        {
            var interrogativeCount = clause.Elements.Count(element => element.Status == InformationStatus.Interrogative);

            if (interrogativeCount > 1)
            {
                throw new NotSupportedException(
                    "Víc tázacích elementů v jedné klauzi podporováno není (Kdo komu co dal?). "
                    + "Ponech tázací status na jednom z nich.");
            }

            // Two claims on one first position: an indirect question is introduced by the wh-word itself,
            // not by a conjunction with one behind it. Refused rather than linearized into nonsense.
            if (interrogativeCount == 1 && firstPositionTaken)
            {
                throw new NotSupportedException(
                    "Tázací element ve vedlejší větě uvozené spojkou podporován není. Nepřímou otázku "
                    + "uvozuje samo tázací slovo, ne spojka — tuhle vazbu zatím model neumí vyjádřit.");
            }

            // The reverse — an interrogative clause with no fronted element — is a yes/no question,
            // which Czech marks by intonation and punctuation alone. That is valid and needs nothing here.
            if (interrogativeCount == 1 && clause.SentenceType != SentenceType.Interrogative)
            {
                throw new InvalidOperationException(
                    "Element má InformationStatus.Interrogative, ale klauze má SentenceType.Declarative. "
                    + "Nastav SentenceType.Interrogative, nebo tázací status odeber.");
            }
        }

        // Person, number and gender of the predicate follow the nominative actor. Without an actor the clause
        // is subjectless or pro-drop and whatever the caller set on the predicate stands.
        private static CzechWordRequest ApplySubjectAgreement(CzechClause clause)
        {
            var predicate = clause.Predicate;

            // Infinitiv je neurčitý tvar: nemá s čím se shodovat a osobu ani číslo nenese. Jeho podmět
            // je koreferenční s větou řídící a na povrchu chybí, což je to, co z něj infinitiv dělá.
            if (predicate.Modus == Modus.Infinitive)
            {
                return predicate;
            }

            // The passive is what the promotion is for: the patient becomes the subject and the verb agrees
            // with it, so "Kniha byla dána" is feminine off kniha and not off whatever the caller stated.
            // The agent is still ACT, but it stands in the instrumental and governs nothing.
            //
            // The recipient deobjective promotes ADDR the same way — dostat agrees with the recipient,
            // not with the actor it demotes to "od" + genitive.
            var subjectFunctor = clause.Diathesis == Diathesis.RecipientDeobjective ? FgdFunctor.ADDR
                : predicate.Voice == Voice.Passive ? FgdFunctor.PAT
                : FgdFunctor.ACT;

            // A counted subject stands in the nominative as a phrase while its head noun is genitive, so the
            // phrase case is what identifies it — "pět studentů" is the subject of "pět studentů přišlo".
            var subject = clause.Elements
                .Where(element => element.Functor == subjectFunctor
                    && (element.PhraseCase ?? element.Word.Case) == Case.Nominative)
                .Select(element => (ClauseElement?)element)
                .FirstOrDefault();

            if (subject is null)
            {
                // Subjectless or pro-drop: nothing to agree with, so the predicate has to carry the
                // categories itself. Say so here rather than let a null person reach the conjugator.
                if (predicate.WordCategory == WordCategory.Verb && (predicate.Person is null || predicate.Number is null))
                {
                    throw new InvalidOperationException(
                        $"Klauze bez podmětu v nominativu (funktor ACT): predikát '{predicate.Lemma}' musí mít vyplněnou osobu a číslo.");
                }

                return predicate;
            }

            predicate.Person = ResolvePerson(subject.Word);

            // A subject counted from five up stops behaving like a plural: the predicate goes neuter singular
            // regardless of the noun's own gender — "pět žáků bylo", against "tři žáci byli".
            //
            // A subject that states neither leaves what the predicate already carries: agreeing with a gap
            // is not agreement, and copying it across turned a past tense into "Unsupported gender".
            (predicate.Number, predicate.Gender) = subject.Agreement switch
            {
                CardinalAgreement.GenitivePluralInDirectCases
                    or CardinalAgreement.AlwaysGenitivePlural
                    or CardinalAgreement.GenitiveSingular => (Number.Singular, Gender.Neuter),
                _ => (subject.Word.Number ?? predicate.Number, subject.Word.Gender ?? predicate.Gender)
            };

            return predicate;
        }

        private static Person ResolvePerson(CzechWordRequest subject)
        {
            if (subject.WordCategory != WordCategory.Pronoun)
            {
                return Person.Third;
            }

            return subject.Lemma switch
            {
                "já" or "my" => Person.First,
                "ty" or "vy" => Person.Second,
                _ => Person.Third
            };
        }
    }
}

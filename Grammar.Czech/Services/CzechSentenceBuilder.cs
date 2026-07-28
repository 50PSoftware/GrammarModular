using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using System.Text.RegularExpressions;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Assembles a Czech clause into a surface sentence: agreement, functional sentence perspective,
    /// and Wackernagel placement of the clitic cluster.
    /// </summary>
    public class CzechSentenceBuilder
    {
        private readonly CzechWordFormComposer composer;
        private readonly ICzechCliticService cliticService;
        private readonly ICzechPronounService pronounService;
        private readonly ICzechNumeralService numeralService;
        private readonly ICzechPrepositionService prepositionService;
        private readonly ICzechConjunctionService conjunctionService;
        private readonly ICzechValencyService valencyService;
        private readonly ICzechAdverbService adverbService;
        private readonly ICzechParticleService particleService;
        private readonly ICzechInterjectionService interjectionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechSentenceBuilder"/> type.
        /// </summary>
        public CzechSentenceBuilder(
            CzechWordFormComposer composer,
            ICzechCliticService cliticService,
            ICzechPronounService pronounService,
            ICzechNumeralService numeralService,
            ICzechPrepositionService prepositionService,
            ICzechConjunctionService conjunctionService,
            ICzechValencyService valencyService,
            ICzechAdverbService adverbService,
            ICzechParticleService particleService,
            ICzechInterjectionService interjectionService)
        {
            this.adverbService = adverbService;
            this.composer = composer;
            this.cliticService = cliticService;
            this.pronounService = pronounService;
            this.numeralService = numeralService;
            this.prepositionService = prepositionService;
            this.conjunctionService = conjunctionService;
            this.valencyService = valencyService;
            this.particleService = particleService;
            this.interjectionService = interjectionService;
        }

        /// <summary>
        /// Builds the surface sentence for the supplied clause.
        /// </summary>
        /// <param name="clause">The clause to linearize.</param>
        /// <returns>The assembled sentence, capitalized and terminated.</returns>
        public string Build(CzechClause clause) => Build(new SimpleSentence(clause));

        /// <summary>
        /// Builds the surface sentence for the supplied sentence tree.
        /// </summary>
        /// <param name="sentence">The sentence to linearize.</param>
        /// <returns>The assembled sentence, capitalized and terminated.</returns>
        public string Build(SentenceNode sentence)
        {
            // Capitalization and the final mark belong to the sentence, not to any one clause, so they are
            // applied once here — a dependent clause must get neither.
            return Capitalize(NormalizePunctuation(Render(sentence, firstPositionTaken: false))) + FindTerminator(sentence);
        }

        // A relative clause always emits the comma that closes it, because whether one is needed depends on
        // what follows and the constituent cannot see that. Two commas therefore meet whenever the clause it
        // closes is also followed by a comma of its own, and a comma is left dangling whenever the relative
        // clause happens to end the sentence. Both are settled here, once, rather than guessed at per site.
        private static string NormalizePunctuation(string sentence) =>
            Regex.Replace(sentence, @",(\s*,)+", ",").TrimEnd().TrimEnd(',');

        private string Render(
            SentenceNode sentence,
            bool firstPositionTaken,
            string? secondPositionConjunction = null,
            bool suppressConditional = false)
            => RenderNode(sentence, firstPositionTaken, secondPositionConjunction, suppressConditional).Text;

        // Hands back the predicate of the leading clause alongside the text, because a caller above may need
        // the person it agreed on — aby does. CzechWordRequest is a struct, so the categories subject
        // agreement fills in cannot be read back off the clause the caller still holds: that copy never saw
        // them. Returning the resolved one is the only way to see it without resolving it a second time.
        private (string Text, CzechWordRequest? Predicate) RenderNode(
            SentenceNode sentence,
            bool firstPositionTaken,
            string? secondPositionConjunction = null,
            bool suppressConditional = false) => sentence switch
        {
            SimpleSentence simple =>
                RenderClause(simple.Clause, firstPositionTaken, secondPositionConjunction, suppressConditional),
            Coordination coordination => RenderCoordination(coordination, firstPositionTaken, suppressConditional),
            Subordination subordination => RenderSubordination(subordination),
            _ => throw new NotSupportedException($"Neznámý typ větného uzlu: {sentence.GetType().Name}.")
        };

        // The conjunction stands between the conjuncts and outside the clause that follows it, so that
        // clause keeps its own first position: "Petr přišel a umyl se".
        // An inherited first position — a subordinator above this coordination — reaches the first conjunct
        // only. Every later conjunct is a clause of its own and opens its own second position.
        private (string Text, CzechWordRequest? Predicate) RenderCoordination(
            Coordination coordination, bool firstPositionTaken, bool suppressConditional)
        {
            if (coordination.Conjuncts.Count == 0)
            {
                throw new InvalidOperationException("Souřadné souvětí musí mít alespoň jednu klauzi.");
            }

            var requiresComma = coordination.RequiresComma ?? conjunctionService.RequiresComma(coordination.Conjunction);

            // však does not open its clause, so it is handed to the conjunct to place after the first
            // constituent instead of standing between the two: "Petr přišel, Pavel však zůstal".
            var secondPosition = conjunctionService.OccupiesSecondPosition(coordination.Conjunction);

            var separator = secondPosition
                ? ", "
                : requiresComma
                    ? $", {coordination.Conjunction} "
                    : $" {coordination.Conjunction} ";

            // aby above a coordination carries the auxiliary for every conjunct, not only the first:
            // "aby přišel a pomohl" has one by between them.
            var rendered = coordination.Conjuncts
                .Select((conjunct, index) => RenderNode(
                    conjunct,
                    firstPositionTaken && index == 0,
                    secondPosition && index > 0 ? coordination.Conjunction : null,
                    suppressConditional))
                .ToList();

            // The leading conjunct is what anything above this coordination agrees with.
            return (string.Join(separator, rendered.Select(item => item.Text)), rendered[0].Predicate);
        }

        // The conjunction belongs to the dependent clause and fills its first position, which is why the
        // cluster follows the conjunction and not the verb: "Petr přišel, protože se bál".
        private (string Text, CzechWordRequest? Predicate) RenderSubordination(Subordination subordination)
        {
            var (main, mainPredicate) = RenderNode(subordination.Main, firstPositionTaken: false);
            var conjunction = subordination.Conjunction;
            var fuses = conjunctionService.FusesWithConditional(conjunction);

            // aby and kdyby carry the conditional auxiliary themselves, so the clause is built without one:
            // the particle moved into the conjunction, it was not duplicated. Rendering both would give
            // "abych se bych umyl".
            var (subordinate, subordinatePredicate) = RenderNode(
                subordination.Subordinate,
                conjunctionService.OccupiesFirstPosition(conjunction),
                suppressConditional: fuses);

            // The person comes back out of the render rather than being worked out ahead of it. Subject
            // agreement is what resolves it — stated outright, left to pro-drop, or taken off a nominative
            // subject — and doing that twice would mean keeping a second copy of the rule in step with it.
            var conjunctionForm = conjunctionService.GetForm(
                conjunction, subordinatePredicate?.Number, subordinatePredicate?.Person);

            var separator = conjunctionService.RequiresComma(conjunction) ? ", " : " ";

            // What stands above this sentence agrees with its main clause, not with the dependent one.
            return ($"{main}{separator}{conjunctionForm} {subordinate}", mainPredicate);
        }

        // The mark closing the sentence comes from the clause that opens it.
        private static string FindTerminator(SentenceNode sentence) => sentence switch
        {
            SimpleSentence simple => simple.Clause.Terminator,
            Coordination coordination => FindTerminator(coordination.Conjuncts[0]),
            Subordination subordination => FindTerminator(subordination.Main),
            _ => "."
        };

        private (string Text, CzechWordRequest? Predicate) RenderClause(
            CzechClause clause,
            bool firstPositionTaken,
            string? secondPositionConjunction = null,
            bool suppressConditional = false)
        {
            clause = ApplyValencyFrame(clause);

            // Has to sit between the two: the frame decides what case the phrase stands in, and the numeral
            // rewrites the head's case off the back of it — which subject agreement then has to see.
            clause = ApplyCardinalGovernment(clause);

            var predicate = ApplySubjectAgreement(clause);

            // Short pronouns leave the constituent order entirely and join the cluster, so they have to be
            // taken out before the remaining elements are linearized.
            var pronounClitics = clause.Elements.Where(IsCliticPronoun).ToList();
            var constituents = clause.Elements.Except(pronounClitics).ToList();

            ValidateSentenceType(clause, constituents, firstPositionTaken);
            ValidateParticles(clause, constituents);

            // A clause-initial particle fills first position exactly as a subordinating conjunction does, so
            // the cluster follows it: "Ať se umyje". An interjection does not — it stands outside the clause
            // behind its own comma and leaves first position to whatever comes next.
            firstPositionTaken |= clause.Particle is not null;

            // FSP: the interrogative focus opens the clause, contrastive material is fronted behind it,
            // given material forms the theme before the verb, new material forms the rheme after it.
            // Order inside one status is the caller's.
            var preVerbal = constituents
                .Where(element => element.Status == InformationStatus.Interrogative)
                .Concat(constituents.Where(element => element.Status == InformationStatus.Contrastive))
                .Concat(constituents.Where(element => element.Status == InformationStatus.Given))
                .Select(Realize)
                .ToList();

            var postVerbal = constituents
                .Where(element => element.Status == InformationStatus.New)
                .Select(Realize)
                .ToList();

            // The buckets select by status rather than sorting, so an unhandled status would drop its
            // element from the output instead of failing. Cheaper to notice here than in the surface string.
            if (preVerbal.Count + postVerbal.Count != constituents.Count)
            {
                throw new NotSupportedException(
                    "Některý konstituent nemá v linearizaci místo — jeho InformationStatus není zařazen "
                    + "ani před sloveso, ani za něj.");
            }

            var (verbRest, clitics) = SplitOffClitics(predicate, suppressConditional);
            clitics.AddRange(BuildPronounClitics(pronounClitics));

            var words = BuildLinearOrder(
                preVerbal, verbRest, cliticService.ContractCluster(clitics), postVerbal,
                firstPositionTaken, secondPositionConjunction);

            if (clause.Particle is not null)
            {
                words.Insert(0, clause.Particle);
            }

            var text = string.Join(' ', words);

            // The comma is the ÚJČ rule for an interjection that does not stand in for a clause member, which
            // is the only use this slot expresses.
            return (clause.Interjection is null ? text : $"{clause.Interjection}, {text}", predicate);
        }

        // The two checks the particle data supports and the clause can actually be measured against.
        private void ValidateParticles(CzechClause clause, IEnumerable<ClauseElement> constituents)
        {
            if (clause.Particle is { } particle)
            {
                if (!particleService.IsParticle(particle) || !particleService.IsClauseInitial(particle))
                {
                    throw new InvalidOperationException(
                        $"'{particle}' není větná částice, která uvozuje klauzi. Sem patří ať, kéž, nechť "
                        + "nebo nuže; částici s dosahem na jeden člen dej na ClauseElement.Particle.");
                }

                // The mood is deliberately not checked. "Ať přijde" is a plain third-person present — Czech
                // has no third-person imperative — and NESČ states no mood government for the optative group
                // at all, so any check here would be enforcing a rule of mine rather than one of the
                // language's.
            }

            if (clause.Interjection is { } interjection && interjectionService.IsInterjection(interjection)
                && !interjectionService.RequiresComma(interjection, asPredicate: false))
            {
                throw new InvalidOperationException(
                    $"Citoslovce '{interjection}' se tu neodděluje čárkou, což tenhle slot neumí vyjádřit.");
            }

            // A modifying particle carries no stress of its own, so it cannot be part of what the utterance
            // is about. NESČ states it of the whole group, and Status is what says which constituent is the
            // rheme, so this is checkable rather than merely documented.
            foreach (var element in constituents)
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

        // The frame says how each argument of this verb is realized, so the caller states the functor and the
        // word and the case follows from the verb. A case set explicitly wins — the frame fills gaps, it does
        // not overrule a deliberate choice.
        private CzechClause ApplyValencyFrame(CzechClause clause)
        {
            if (clause.Predicate.WordCategory != WordCategory.Verb)
            {
                return clause;
            }

            var frame = valencyService.GetFrame(clause.Predicate.Lemma, clause.FrameLabel);

            return clause with { Elements = clause.Elements.Select(element => ApplySlot(element, frame, clause.Predicate.Lemma)).ToList() };
        }

        private ClauseElement ApplySlot(ClauseElement element, ValencyFrame? frame, string verbLemma)
        {
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

            var word = element.Word;
            word.Case = slot.Realization.Case;

            return element with
            {
                Word = word,
                Preposition = element.Preposition ?? slot.Realization.Preposition
            };
        }

        // The one place Czech runs agreement backwards. Everywhere else the head hands its categories down to
        // its attributes; a cardinal from five up instead forces the noun it counts into the genitive plural
        // and, through the element, the predicate into the neuter singular — pět žáků bylo.
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
        private static void ValidateSentenceType(
            CzechClause clause, IReadOnlyList<ClauseElement> constituents, bool firstPositionTaken)
        {
            var interrogativeCount = constituents.Count(element => element.Status == InformationStatus.Interrogative);

            if (interrogativeCount > 1)
            {
                throw new NotSupportedException(
                    "Víc tázacích elementů v jedné klauzi podporováno není (Kdo komu co dal?). "
                    + "Ponech tázací status na jednom z nich.");
            }

            // Two claims on one first position. In Czech an indirect question is introduced by the wh-word
            // itself — "Zeptal se, koho jsem viděl" — not by a conjunction with a wh-word behind it, so the
            // combination does not describe a real sentence. Refused rather than linearized into
            // "protože jsi koho viděl".
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

        // One constituent, however many words. The whole phrase counts as a single unit for second position,
        // so the string returned here is what the cluster attaches after.
        private string Realize(ClauseElement element)
        {
            var head = element.Word;
            var afterPreposition = element.Preposition is not null;

            if (afterPreposition)
            {
                head.IsAfterPreposition = true;
            }

            var words = element.Modifiers
                .Select(modifier => AgreeWithHead(modifier, element.Word, afterPreposition))
                .Select(modifier => composer.GetFullForm(modifier).Form)
                .Append(composer.GetFullForm(head).Form)
                .ToList();

            if (afterPreposition)
            {
                ValidateGovernment(element);

                // Vocalization looks at whatever actually comes next, which is the first modifier when there is one.
                words.Insert(0, prepositionService.Vocalize(element.Preposition!, words[0]));
            }

            // Outside the preposition, not inside it: the particle scopes over the whole constituent —
            // "jen pro Petra", not "pro jen Petra".
            if (element.Particle is not null)
            {
                words.Insert(0, element.Particle);
            }

            var text = string.Join(' ', words);

            return element.Relative is null ? text : $"{text}, {RenderRelative(element)}";
        }

        // The pronoun agrees with the antecedent in gender, number and animacy, and takes its case from the
        // role it plays inside the relative clause. It also fills the first position of that clause, so the
        // cluster follows it: "muž, kterého jsem viděl".
        // The closing comma is emitted here and removed again if the sentence happens to end on it.
        private string RenderRelative(ClauseElement element)
        {
            var relative = element.Relative!;
            var antecedent = element.Word;

            // A relative adverb is uninflected and is not an argument of its clause, so nothing agrees with
            // the antecedent through it: "dům, kde bydlím" keeps the clause's own person and number.
            if (adverbService.IsRelative(relative.Relativizer))
            {
                return $"{relative.Relativizer} {RenderClause(relative.Clause, firstPositionTaken: true).Text},";
            }

            if (pronounService.GetPronounType(relative.Relativizer) != PronounType.Relative)
            {
                throw new InvalidOperationException(
                    $"'{relative.Relativizer}' není vztažné zájmeno ani vztažné příslovce.");
            }

            var pronoun = pronounService.TryGetForm(
                relative.Relativizer, relative.Case, antecedent.Gender, antecedent.Number, antecedent.IsAnimate, null)
                ?? throw new InvalidOperationException(
                    $"Vztažné zájmeno '{relative.Relativizer}' nemá tvar pro pád {relative.Case}.");

            var clause = relative.Clause;

            // A nominative pronoun is the subject of its clause, so the predicate agrees with the antecedent
            // through it — "muž, který se učil" against "žena, která se učila".
            if (relative.Case == Case.Nominative)
            {
                var predicate = clause.Predicate;
                predicate.Person = Person.Third;
                predicate.Number = antecedent.Number;
                predicate.Gender = antecedent.Gender;
                clause = clause with { Predicate = predicate };
            }

            return $"{pronoun} {RenderClause(clause, firstPositionTaken: true).Text},";
        }

        // The preposition governs the constituent, not its head noun, and the two part company under a
        // cardinal: in "pro pět studentů" the noun is genitive because the numeral put it there, while the
        // phrase pro governs is accusative. PhraseCase is what the constituent actually stands in, so that
        // is what gets checked; it is only filled when a numeral rewrote the head, hence the fallback.
        private void ValidateGovernment(ClauseElement element)
        {
            var preposition = element.Preposition!;
            var governed = element.PhraseCase ?? element.Word.Case;

            if (governed is null || !prepositionService.GetAllowedCases(preposition).Any())
            {
                return;
            }

            if (!prepositionService.IsAllowed(preposition, governed.Value))
            {
                throw new InvalidOperationException(
                    $"Předložka '{preposition}' neřídí pád {governed.Value}. Povolené pády: "
                    + string.Join(", ", prepositionService.GetAllowedCases(preposition)) + ".");
            }
        }

        // The head governs the attribute. Only unset categories are filled in, so an attribute that carries
        // its own case — a genitive one, say — keeps it.
        private static CzechWordRequest AgreeWithHead(CzechWordRequest modifier, CzechWordRequest head, bool afterPreposition)
        {
            // An adverb has no categories to agree in. Handing it a case would be harmless today, because
            // the adverb service reads only the lemma and the degree, but it would be a lie in the request.
            if (modifier.WordCategory == WordCategory.Adverb)
            {
                return modifier;
            }

            modifier.Gender ??= head.Gender;
            modifier.Number ??= head.Number;
            modifier.Case ??= head.Case;
            modifier.IsAnimate ??= head.IsAnimate;
            modifier.IsAfterPreposition = afterPreposition;

            return modifier;
        }

        // Ranks 4 and 5 of the cluster: dative short pronouns, then accusative ones.
        // Dal jsem mu ho, never Dal jsem ho mu.
        private IReadOnlyList<string> BuildPronounClitics(IEnumerable<ClauseElement> elements) =>
            elements
                .OrderBy(element => element.Word.Case == Case.Dative ? 0 : 1)
                .Select(element => pronounService.TryGetForm(
                    element.Word.Lemma,
                    element.Word.Case!.Value,
                    element.Word.Gender,
                    element.Word.Number,
                    element.Word.IsAnimate,
                    new PronounFormOptions { PreferClitic = true }) ?? element.Word.Lemma)
                .ToList();

        // A personal pronoun in the dative or accusative is prosodically weak and belongs in the cluster.
        // Four things keep one out: a preposition, which forces the prepositional form inside its own phrase;
        // contrastive status, which needs the stressed long form left where it stands (Mně to dal, ne tobě);
        // a modifier, which makes the pronoun the head of a full phrase rather than a weak word; and any
        // other case, which is never clitic.
        private bool IsCliticPronoun(ClauseElement element) =>
            element.Word.WordCategory == WordCategory.Pronoun
            && element.Word.Case is Case.Dative or Case.Accusative
            && element.Status != InformationStatus.Contrastive
            // An interrogative focus has to open its clause, so it can never be pulled into the cluster.
            && element.Status != InformationStatus.Interrogative
            && element.Preposition is null
            && !element.Word.IsAfterPreposition
            && element.Modifiers.Count == 0
            && pronounService.GetPronounType(element.Word.Lemma) == PronounType.Personal;

        // The clitic cluster attaches to the first constituent of the clause, whatever that constituent is.
        // With no pre-verbal constituent the verb itself opens the clause and the cluster follows its first word
        // (Budu se dělat); otherwise it follows the first constituent only, not all of them — which is why
        // "Petr včera se myl" is wrong and "Petr se včera myl" is right.
        private static List<string> BuildLinearOrder(
            List<string> preVerbal, List<string> verbRest, IReadOnlyList<string> clitics, List<string> postVerbal,
            bool firstPositionTaken, string? secondPositionConjunction = null)
        {
            var words = new List<string>();

            // však lands in the same slot as the cluster, behind it. It takes no rank inside the obligatory
            // cluster — NESČ counts it among the nestálá klitika rather than the klitika tantum — so this is
            // a position the sources permit, not one they prescribe.
            var second = clitics.ToList();

            if (secondPositionConjunction is not null)
            {
                second.Add(secondPositionConjunction);
            }

            if (second.Count == 0)
            {
                words.AddRange(preVerbal);
                words.AddRange(verbRest);
                words.AddRange(postVerbal);
                return words;
            }

            // A subordinating conjunction already fills first position, so the cluster opens the clause
            // proper — ahead of the subject: "protože se Petr umyl", not "protože Petr se umyl".
            if (firstPositionTaken)
            {
                words.AddRange(second);
                words.AddRange(preVerbal);
                words.AddRange(verbRest);
                words.AddRange(postVerbal);
                return words;
            }

            if (preVerbal.Count > 0)
            {
                words.Add(preVerbal[0]);
                words.AddRange(second);
                words.AddRange(preVerbal.Skip(1));
                words.AddRange(verbRest);
            }
            else
            {
                words.Add(verbRest[0]);
                words.AddRange(second);
                words.AddRange(verbRest.Skip(1));
            }

            words.AddRange(postVerbal);
            return words;
        }

        // The builder owns the whole cluster, so it asks the composer for a phrase without the reflexive and
        // adds the particle itself. Letting the composer place it first and lifting it back out would break on
        // the contracted forms, where the auxiliary and the reflexive fuse into a single token (jsi se → ses).
        private (List<string> VerbRest, List<string> Clitics) SplitOffClitics(
            CzechWordRequest predicate, bool suppressConditional)
        {
            var reflexiveType = predicate.ReflexiveType;

            predicate.HasPrecedingConstituent = false;
            predicate.ReflexiveType = ReflexiveType.None;

            var verbRest = new List<string>();
            var clitics = new List<string>();

            foreach (var word in composer.GetFullForm(predicate).Form.Split(' '))
            {
                // Dropped, not moved: aby or kdyby above this clause is already carrying it.
                if (suppressConditional && cliticService.IsConditionalParticle(word))
                {
                    continue;
                }

                (cliticService.IsCliticAuxiliary(word) ? clitics : verbRest).Add(word);
            }

            // Rank 3: the reflexive follows any auxiliary already in the cluster.
            if (reflexiveType != ReflexiveType.None)
            {
                clitics.Add(cliticService.GetReflexive(reflexiveType));
            }

            return (verbRest, clitics);
        }

        // Person, number and gender of the predicate follow the nominative actor. Without an actor the clause
        // is subjectless or pro-drop and whatever the caller set on the predicate stands.
        private static CzechWordRequest ApplySubjectAgreement(CzechClause clause)
        {
            var predicate = clause.Predicate;

            // A counted subject stands in the nominative as a phrase while its head noun is genitive, so the
            // phrase case is what identifies it — "pět studentů" is the subject of "pět studentů přišlo".
            var subject = clause.Elements
                .Where(element => element.Functor == FgdFunctor.ACT
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
            (predicate.Number, predicate.Gender) = subject.Agreement switch
            {
                CardinalAgreement.GenitivePluralInDirectCases
                    or CardinalAgreement.AlwaysGenitivePlural
                    or CardinalAgreement.GenitiveSingular => (Number.Singular, Gender.Neuter),
                _ => (subject.Word.Number, subject.Word.Gender)
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

        private static string Capitalize(string sentence) =>
            string.IsNullOrEmpty(sentence)
                ? sentence
                : char.ToUpperInvariant(sentence[0]) + sentence[1..];
    }
}

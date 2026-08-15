using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Puts the words of a planned clause in order: functional sentence perspective decides where the
    /// constituents go, and Wackernagel's law decides where the clitic cluster lands.
    /// </summary>
    /// <remarks>
    /// The last stage before the surface string, and the only one allowed to care about order. It
    /// changes no grammatical category — everything it renders was already decided by
    /// <see cref="CzechMicroplanner"/> — which is what makes Czech word order expressible at all: it is
    /// pragmatic rather than syntactic, so it has to be free to vary without any of the forms moving
    /// with it.
    /// </remarks>
    public class CzechWordOrderResolver
    {
        private readonly CzechWordFormComposer composer;
        private readonly ICzechCliticService cliticService;
        private readonly ICzechPronounService pronounService;
        private readonly ICzechPrepositionService prepositionService;
        private readonly ICzechAdverbService adverbService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechWordOrderResolver"/> type.
        /// </summary>
        /// <param name="composer">The composer that produces each word's form.</param>
        /// <param name="cliticService">The clitic service, for the cluster and its contractions.</param>
        /// <param name="pronounService">The pronoun service, for short forms and relative pronouns.</param>
        /// <param name="prepositionService">The preposition service, for vocalization and government.</param>
        /// <param name="adverbService">The adverb service, for telling a relative adverb from a pronoun.</param>
        public CzechWordOrderResolver(
            CzechWordFormComposer composer,
            ICzechCliticService cliticService,
            ICzechPronounService pronounService,
            ICzechPrepositionService prepositionService,
            ICzechAdverbService adverbService)
        {
            this.composer = composer;
            this.cliticService = cliticService;
            this.pronounService = pronounService;
            this.prepositionService = prepositionService;
            this.adverbService = adverbService;
        }

        /// <summary>
        /// Renders the planned clause as a surface string, without capitalization or a final mark.
        /// </summary>
        /// <param name="planned">The clause with all its categories settled.</param>
        /// <param name="firstPositionTaken">
        /// Whether something outside the clause already fills its first position — a subordinating
        /// conjunction, say — in which case the cluster opens the clause proper.
        /// </param>
        /// <param name="renderEmbedded">
        /// How to render a sentence embedded in a constituent — a relative clause, which may itself be a
        /// complex sentence. Passed in rather than resolved, because the sentence that owns the recursion
        /// is above this stage.
        /// </param>
        /// <param name="secondPositionConjunction">The conjunction that shares the cluster's slot, or null.</param>
        /// <param name="suppressConditional">
        /// Whether the conditional auxiliary is being carried by a conjunction above this clause, in
        /// which case it is dropped here rather than rendered twice.
        /// </param>
        /// <returns>The clause as a string of words.</returns>
        public string Resolve(
            PlannedClause planned,
            bool firstPositionTaken,
            Func<SentenceNode, bool, string> renderEmbedded,
            string? secondPositionConjunction = null,
            bool suppressConditional = false,
            bool omitPredicate = false)
        {
            var clause = planned.Clause;

            // Short pronouns leave the constituent order entirely and join the cluster, so they have to be
            // taken out before the remaining elements are linearized.
            var pronounClitics = clause.Elements.Where(IsCliticPronoun).ToList();
            var constituents = clause.Elements.Except(pronounClitics).ToList();

            // A clause-initial particle fills first position as a subordinator does, so the cluster
            // follows it ("Ať se umyje"); an interjection stands outside the clause and does not.
            firstPositionTaken |= clause.Particle is not null;

            // FSP: interrogative focus opens the clause, contrastive material follows it, theme before
            // the verb and rheme after. Order within one status is the caller's.
            var preVerbal = constituents
                .Where(element => element.Status == InformationStatus.Interrogative)
                .Concat(constituents.Where(element => element.Status == InformationStatus.Contrastive))
                .Concat(constituents.Where(element => element.Status == InformationStatus.Given))
                .Select(element => Realize(element, renderEmbedded))
                .ToList();

            var postVerbal = constituents
                .Where(element => element.Status == InformationStatus.New)
                .Select(element => Realize(element, renderEmbedded))
                .ToList();

            // The buckets select by status rather than sorting, so an unhandled status would drop its
            // element from the output instead of failing. Cheaper to notice here than in the surface string.
            if (preVerbal.Count + postVerbal.Count != constituents.Count)
            {
                throw new NotSupportedException(
                    "Některý konstituent nemá v linearizaci místo — jeho InformationStatus není zařazen "
                    + "ani před sloveso, ani za něj.");
            }

            var (verbRest, clitics) = SplitOffClitics(planned.Predicate, suppressConditional);

            // Vypuštěné sloveso zmizí ze slovosledu, ne z plánu: pády zbytků řídí pořád ono, jen se
            // nevysloví. Klitika se nevypouštějí — kdyby jaká byla, mezera by neměla hostitele, a proto
            // se elipsa na takovou větu vůbec nepustí.
            if (omitPredicate)
            {
                verbRest.Clear();
            }

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
            return clause.Interjection is null ? text : $"{clause.Interjection}, {text}";
        }

        // One constituent, however many words. The whole phrase counts as a single unit for second position,
        // so the string returned here is what the cluster attaches after.
        private string Realize(ClauseElement element, Func<SentenceNode, bool, string> renderEmbedded)
        {
            // A slot filled by a proposition is one constituent however many words it runs to, so the
            // cluster of the clause above attaches after the whole of it: "chce jít do školy". Its first
            // position counts as taken, because the clause it belongs to owns the only cluster there is
            // and the infinitive's clitics have already climbed into it.
            if (element.Content is { } content)
            {
                return renderEmbedded(content, true);
            }

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

            return element.Relative is null ? text : $"{text}, {RenderRelative(element, renderEmbedded)}";
        }

        // The pronoun agrees with the antecedent but takes its case from its role inside the clause, and
        // fills that clause's first position: "muž, kterého jsem viděl".
        private string RenderRelative(ClauseElement element, Func<SentenceNode, bool, string> renderEmbedded)
        {
            var relative = element.Relative!;
            var antecedent = element.Word;

            // A relative adverb is uninflected and is not an argument of its clause, so nothing agrees with
            // the antecedent through it: "dům, kde bydlím" keeps the clause's own person and number.
            if (adverbService.IsRelative(relative.Relativizer))
            {
                return $"{relative.Relativizer} {renderEmbedded(relative.Clause, true)},";
            }

            // Přivlastňovací vztažné zájmeno je uvnitř věty jako přívlastek svého jména, kam ho postavil
            // plánovač, takže se tady nevypisuje podruhé. Z antecedentu si nebere tvar, ale samo slovo:
            // rod a číslo řídícího jména rozhodují, které ze tří to je, a to je jediné, co jde zkontrolovat.
            // První pozici drží ten člen, ne zájmeno samo, takže ji klauze obsazuje ze svého.
            if (pronounService.IsPossessiveRelative(relative.Relativizer))
            {
                ValidatePossessive(relative.Relativizer, antecedent);

                return $"{renderEmbedded(relative.Clause, false)},";
            }

            // Mezi čteními, ne to primární: 'co' a 'kdo' jsou v datech vedené jako tázací, protože se tak
            // čtou nejčastěji, ale 'člověk, co přišel' je táž slovní jednotka v jiné konstrukci. Kterou
            // z nich stavíme, ví tohle místo, ne heslář.
            if (!pronounService.GetReadings(relative.Relativizer)
                .Any(reading => reading.Type == PronounType.Relative))
            {
                throw new InvalidOperationException(
                    $"'{relative.Relativizer}' není vztažné zájmeno ani vztažné příslovce.");
            }

            var pronoun = pronounService.TryGetForm(
                relative.Relativizer, relative.Case, antecedent.Gender, antecedent.Number, antecedent.IsAnimate, null)
                ?? throw new InvalidOperationException(
                    $"Vztažné zájmeno '{relative.Relativizer}' nemá tvar pro pád {relative.Case}.");

            var clause = relative.Case == Case.Nominative
                ? AgreeWithAntecedent(relative.Clause, antecedent)
                : relative.Clause;

            return $"{pronoun} {renderEmbedded(clause, true)},";
        }

        // Které ze tří přivlastňovacích vztažných zájmen se použije, říká řídící jméno: mužský a střední
        // rod v jednotném čísle jehož, ženský jejíž, množné číslo jejichž. Není to volba stylu — 'žena,
        // jehož dům' je chyba, a chyba, kterou by nic dalšího nezachytilo, protože všechny tři jsou platná
        // slova a věta by se postavila.
        private static void ValidatePossessive(string relativizer, CzechWordRequest antecedent)
        {
            var expected = antecedent.Number == Number.Plural
                ? "jejichž"
                : antecedent.Gender == Gender.Feminine ? "jejíž" : "jehož";

            if (!string.Equals(relativizer, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Přivlastňovací vztažné zájmeno se k '{antecedent.Lemma}' neshoduje: čekalo se "
                    + $"'{expected}', zadané je '{relativizer}'. Rod a číslo bere z řídícího jména, "
                    + "pád z vlastněného.");
            }
        }

        // A nominative pronoun is the subject of its clause, so the predicate agrees with the antecedent
        // through it — "muž, který se učil" against "žena, která se učila". One pronoun is the subject of
        // every clause coordinated with that one, so the agreement reaches all of them: "žena, která
        // přišla a odešla". A subordinator opens a clause with a subject of its own and stops it.
        private static SentenceNode AgreeWithAntecedent(SentenceNode node, CzechWordRequest antecedent) => node switch
        {
            SimpleSentence simple => new SimpleSentence(Agree(simple.Clause, antecedent)),
            Coordination coordination => coordination with
            {
                Conjuncts = [.. coordination.Conjuncts.Select(conjunct => AgreeWithAntecedent(conjunct, antecedent))],
            },
            Subordination subordination => subordination with
            {
                Main = AgreeWithAntecedent(subordination.Main, antecedent),
            },
            _ => node,
        };

        private static CzechClause Agree(CzechClause clause, CzechWordRequest antecedent)
        {
            var predicate = clause.Predicate;

            predicate.Person = Person.Third;
            predicate.Number = antecedent.Number;
            predicate.Gender = antecedent.Gender;

            return clause with { Predicate = predicate };
        }

        // The preposition governs the constituent, not its head noun: in "pro pět studentů" the noun is
        // genitive but the phrase is accusative. PhraseCase holds the latter, hence the fallback.
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

        // A dative or accusative personal pronoun is prosodically weak and belongs in the cluster; a
        // preposition, contrastive status, a modifier or any other case keeps it out.
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

        // The cluster follows the first constituent, whatever it is — the verb itself when nothing
        // precedes it (Budu se dělat), and the first one only: "Petr se včera myl", not "Petr včera se".
        private static List<string> BuildLinearOrder(
            List<string> preVerbal, List<string> verbRest, IReadOnlyList<string> clitics, List<string> postVerbal,
            bool firstPositionTaken, string? secondPositionConjunction = null)
        {
            var words = new List<string>();

            // však lands in the cluster's slot, behind it. NESČ counts it among the nestálá klitika, so
            // this is a position the sources permit rather than prescribe.
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

        // The resolver owns the cluster, so it asks for a phrase without the reflexive and adds it itself:
        // lifting it back out afterwards breaks on the contracted forms (jsi se → ses).
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
    }
}

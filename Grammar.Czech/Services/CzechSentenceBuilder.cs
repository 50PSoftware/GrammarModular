using Grammar.Core.Enums;
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
    /// <remarks>
    /// The entry point for the whole generation pipeline, and the only part of it a consumer needs to
    /// know about. What it does itself is the sentence level — how clauses join, where the comma goes,
    /// which clause the final mark comes from. Everything inside one clause is delegated:
    /// <see cref="CzechMicroplanner"/> settles the grammatical categories and
    /// <see cref="CzechWordOrderResolver"/> puts the words in order.
    /// <para>
    /// The recursion stays here rather than in either of them, because a clause can contain a sentence —
    /// a relative clause hangs off a constituent — and neither stage should have to know that.
    /// </para>
    /// </remarks>
    public class CzechSentenceBuilder
    {
        private readonly CzechClausePlanner clausePlanner;
        private readonly CzechMicroplanner microplanner;
        private readonly CzechWordOrderResolver wordOrderResolver;
        private readonly ICzechConjunctionService conjunctionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechSentenceBuilder"/> type.
        /// </summary>
        /// <param name="microplanner">The stage that settles the grammatical categories of a clause.</param>
        /// <param name="wordOrderResolver">The stage that puts the words of a clause in order.</param>
        /// <param name="conjunctionService">The conjunction service, for commas and the conditional fusion.</param>
        public CzechSentenceBuilder(
            CzechClausePlanner clausePlanner,
            CzechMicroplanner microplanner,
            CzechWordOrderResolver wordOrderResolver,
            ICzechConjunctionService conjunctionService)
        {
            this.clausePlanner = clausePlanner;
            this.microplanner = microplanner;
            this.wordOrderResolver = wordOrderResolver;
            this.conjunctionService = conjunctionService;
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

        // A relative clause always emits its closing comma, not being able to see what follows, so the
        // doubled and the dangling one are both settled here rather than guessed at per site.
        private static string NormalizePunctuation(string sentence) =>
            Regex.Replace(sentence, @",(\s*,)+", ",").TrimEnd().TrimEnd(',');

        private string Render(
            SentenceNode sentence,
            bool firstPositionTaken,
            string? secondPositionConjunction = null,
            bool suppressConditional = false)
            => RenderNode(sentence, firstPositionTaken, secondPositionConjunction, suppressConditional).Text;

        // Hands back the predicate because a caller above may need the person it agreed on, and
        // CzechWordRequest is a struct — the caller's copy never saw what agreement filled in.
        private (string Text, CzechWordRequest? Predicate) RenderNode(
            SentenceNode sentence,
            bool firstPositionTaken,
            string? secondPositionConjunction = null,
            bool suppressConditional = false,
            bool omitPredicate = false) => sentence switch
        {
            // A clause whose slot turned out to be a dependent clause is no longer one clause, so
            // planning runs before anything else and its result goes back through this switch.
            SimpleSentence simple => clausePlanner.Plan(simple.Clause) switch
            {
                SimpleSentence planned =>
                    RenderClause(
                        planned.Clause, firstPositionTaken, secondPositionConjunction, suppressConditional,
                        omitPredicate),
                var grown => RenderNode(grown, firstPositionTaken, secondPositionConjunction, suppressConditional),
            },
            Coordination coordination => RenderCoordination(coordination, firstPositionTaken, suppressConditional),
            Subordination subordination =>
                RenderSubordination(subordination, firstPositionTaken, suppressConditional),
            _ => throw new NotSupportedException($"Neznámý typ větného uzlu: {sentence.GetType().Name}.")
        };

        // The conjunction stands outside the clause that follows it, so that clause keeps its own first
        // position ("Petr přišel a umyl se"). An inherited one reaches the first conjunct only.
        private (string Text, CzechWordRequest? Predicate) RenderCoordination(
            Coordination coordination, bool firstPositionTaken, bool suppressConditional)
        {
            if (coordination.Conjuncts.Count == 0)
            {
                throw new InvalidOperationException("Souřadné souvětí musí mít alespoň jednu klauzi.");
            }

            var requiresComma = coordination.RequiresComma
                ?? conjunctionService.RequiresComma(coordination.Conjunction, ConjunctionType.Coordinating);

            // však does not open its clause, so it is handed to the conjunct to place after the first
            // constituent instead of standing between the two: "Petr přišel, Pavel však zůstal".
            var secondPosition = conjunctionService.OccupiesSecondPosition(coordination.Conjunction, ConjunctionType.Coordinating);

            var correlate = coordination.Paired ? ResolveCorrelate(coordination.Conjunction) : null;

            // In the split construction the correlate joins the conjuncts and always takes a comma — ÚJČ
            // writes one before the second connective whatever the bare conjunction would do.
            var separator = correlate is not null
                ? $", {correlate} "
                : secondPosition
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
                    suppressConditional,
                    omitPredicate: coordination.AllowVerbEllipsis
                        && index > 0
                        && Repeats(coordination.Conjuncts[index - 1], conjunct)))
                .ToList();

            var text = string.Join(separator, rendered.Select(item => item.Text));

            // The opening member stands outside the first conjunct, exactly as an ordinary coordinating
            // conjunction stands outside the clause it precedes, so it leaves first position alone.
            if (correlate is not null)
            {
                text = $"{coordination.Conjunction} {text}";
            }

            // The leading conjunct is what anything above this coordination agrees with.
            return (text, rendered[0].Predicate);
        }

        // Vypuštěné sloveso se podle manuálu PDT (§12.1.1.1) obnovuje kopií z předchozí klauze, a to
        // tehdy, „když je jasné, které sloveso bylo vypuštěno". Obnovitelnost se tady testuje tvrději,
        // než jazyk vyžaduje: shodne-li se všechno, co by se do mezery doplňovalo, doplnit se dá právě
        // jedno sloveso. Osoba a číslo se lišit smí — ty nese podmět, který ve druhém konjunktu zůstal,
        // takže 'já piju kávu a ona čaj' projde.
        //
        // A jen tam, kde přísudek vychází jako jedno slovo. V 1. a 2. osobě minulého času, v kondicionálu
        // a v opisných tvarech by po vypuštění osiřelo pomocné sloveso nebo zvratná částice, a kam je
        // v takové větě položit, tenhle projekt doložené nemá. Nevypustit je vždycky gramatické.
        private static bool Repeats(SentenceNode previous, SentenceNode current) =>
            Clause(previous) is { } first
            && Clause(current) is { } second

            // Musí zbýt co vyslovit, a musí být čemu to postavit naproti. Manuálový příklad
            // '(Jirka navštívil Marii.) Honza Jiřinu' stojí na tom, že obě klauze mají zbytky a ty si
            // odpovídají — z té paralely se vypuštěné sloveso obnovuje. Klauze, kde nic jiného není, by
            // se vypuštěním slovesa vyprázdnila celá; a klauze, které nemá co odpovídat, mezeru doplnit
            // nepomůže.
            && first.Elements.Count > 0
            && second.Elements.Count > 0
            && first.Predicate is { } before
            && second.Predicate is { } after
            && string.Equals(before.Lemma, after.Lemma, StringComparison.Ordinal)
            && before.Tense == after.Tense
            && before.Modus == after.Modus
            && before.Voice == after.Voice
            && before.Aspect == after.Aspect
            && before.IsNegative == after.IsNegative
            && before.ReflexiveType == ReflexiveType.None
            && after.ReflexiveType == ReflexiveType.None;

        // Jen prostá klauze: u souřadnosti ani podřadnosti uvnitř konjunktu není jedno sloveso, které by
        // se dalo porovnat, a domýšlet které z nich by bylo hádání.
        private static CzechClause? Clause(SentenceNode node) =>
            node is SimpleSentence simple ? simple.Clause : null;

        private string ResolveCorrelate(string conjunction)
            => conjunctionService.GetCorrelate(conjunction, ConjunctionType.Coordinating)
                ?? throw new InvalidOperationException(
                    $"Spojka '{conjunction}' není párová, takže Paired nemá druhý člen, který by položila. "
                    + "Párové jsou buď, ani, nejen, jak, sice a jednak.");

        // The conjunction belongs to the dependent clause and fills its first position, which is why the
        // cluster follows the conjunction and not the verb: "Petr přišel, protože se bál".
        private (string Text, CzechWordRequest? Predicate) RenderSubordination(
            Subordination subordination, bool firstPositionTaken = false, bool suppressConditional = false)
        {
            // Both of these reach the main clause and stop there: what stands above this subordination
            // stands above the clause it opens with, while the dependent clause has its own conjunction
            // filling its own first position and its own answer about the auxiliary.
            //
            // The first position matters inside a relative clause, where the pronoun fills it: without
            // this, "muž, který se učil, protože se bál" put the cluster after the verb instead.
            var (main, mainPredicate) = RenderNode(
                subordination.Main, firstPositionTaken, suppressConditional: suppressConditional);
            var conjunction = subordination.Conjunction;
            var fuses = conjunctionService.FusesWithConditional(conjunction);

            // aby and kdyby carry the conditional auxiliary themselves — the particle moved into the
            // conjunction rather than being duplicated, and rendering both gives "abych se bych umyl".
            var (subordinate, subordinatePredicate) = RenderNode(
                subordination.Subordinate,
                conjunctionService.OccupiesFirstPosition(conjunction),
                suppressConditional: fuses);

            // Taken out of the render rather than worked out ahead of it, since subject agreement is what
            // resolves it and doing that twice means two copies of the rule to keep in step.
            var conjunctionForm = conjunctionService.GetForm(
                conjunction, subordinatePredicate?.Number, subordinatePredicate?.Person);

            var separator = conjunctionService.RequiresComma(conjunction, ConjunctionType.Subordinating) ? ", " : " ";

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
            bool suppressConditional = false,
            bool omitPredicate = false)
        {
            var planned = microplanner.Plan(clause, firstPositionTaken);

            var text = wordOrderResolver.Resolve(
                planned,
                firstPositionTaken,
                // A relative clause is a sentence hanging off a constituent — and a sentence in the full
                // sense, since it can coordinate — so rendering one starts again at the top. The
                // resolver is handed the way back up rather than the whole builder.
                (embedded, embeddedFirstPositionTaken) =>
                    RenderNode(embedded, embeddedFirstPositionTaken).Text,
                secondPositionConjunction,
                suppressConditional,
                omitPredicate);

            return (text, planned.Predicate);
        }

        private static string Capitalize(string sentence) =>
            string.IsNullOrEmpty(sentence)
                ? sentence
                : char.ToUpperInvariant(sentence[0]) + sentence[1..];
    }
}

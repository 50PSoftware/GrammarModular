using Grammar.Core.Enums;
using Grammar.Czech.Cli.Sentence;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using System.Text;

namespace Grammar.Czech.Cli.Rendering
{
    /// <summary>
    /// Writes the draft out as the table the user confirms or corrects.
    /// </summary>
    /// <remarks>
    /// The table is the whole point of the confirm step: it says what the tool read into the lemmas —
    /// which word became the predicate, what role each of the others got, where the case came from — so
    /// that a wrong reading is visible before it turns into a well-formed sentence about something else.
    /// </remarks>
    public sealed class DraftView
    {
        private readonly CzechWordFormComposer _composer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftView"/> type.
        /// </summary>
        /// <param name="composer">The composer used to show each constituent's own form.</param>
        public DraftView(CzechWordFormComposer composer)
        {
            _composer = composer;
        }

        /// <summary>
        /// Renders the draft: the predicate, the constituents, and whatever is still open.
        /// </summary>
        /// <param name="draft">The draft to render.</param>
        /// <returns>The text to print.</returns>
        public string Render(ClauseDraft draft)
        {
            var text = new StringBuilder();

            text.AppendLine($"Přísudek  {draft.PredicateLemma} — {Describe(draft.Predicate)}");

            if (draft.Frame is { } frame)
            {
                text.AppendLine($"Rámec     {frame.FrameLabel ?? "bez popisku"} "
                    + $"({string.Join(", ", frame.Slots.OrderBy(slot => slot.CanonicalOrder).Select(slot => slot.Functor))})");
            }
            else if (draft.FrameChoices.Count == 0)
            {
                text.AppendLine("Rámec     žádný — slovník tohle sloveso nevede, pády doplň sám");
            }

            text.AppendLine();
            text.AppendLine(Table(draft));

            foreach (var note in draft.Notes.Distinct())
            {
                text.AppendLine($"Pozn.: {note}");
            }

            foreach (var gap in draft.Gaps())
            {
                text.AppendLine($"Chybí: {gap}");
            }

            return text.ToString().TrimEnd();
        }

        /// <summary>
        /// Describes the predicate's categories in one line.
        /// </summary>
        /// <param name="predicate">The predicate to describe.</param>
        /// <returns>The description.</returns>
        public static string Describe(CzechWordRequest predicate)
        {
            var parts = new List<string>();

            if (predicate.Aspect is { } aspect)
            {
                parts.Add(Terms.Name(aspect));
            }

            if (predicate.Tense is { } tense)
            {
                parts.Add(Terms.Name(tense) + " čas");
            }

            if (predicate.Modus is { } mood)
            {
                parts.Add(Terms.Name(mood) + " způsob");
            }

            if (predicate.Voice is { } voice)
            {
                parts.Add(Terms.Name(voice) + " rod");
            }

            if (predicate.Person is { } person && predicate.Number is { } number)
            {
                parts.Add($"{Terms.Name(person)} os. {Terms.Name(number)} č.");
            }

            if (predicate.IsNegative)
            {
                parts.Add("zápor");
            }

            if (predicate.ReflexiveType != ReflexiveType.None)
            {
                parts.Add(predicate.ReflexiveType.ToString().EndsWith("Si", StringComparison.Ordinal) ? "si" : "se");
            }

            return string.Join(", ", parts);
        }

        private string Table(ClauseDraft draft)
        {
            var rows = draft.Constituents
                .Select(constituent => new[]
                {
                    constituent.Position.ToString(),
                    Head(constituent),
                    Role(constituent),
                    Terms.Name(constituent.Status),
                    CaseColumn(constituent),
                    Form(constituent),
                    Source(constituent.Origin),
                })
                .ToList();

            string[] header = ["#", "slovo", "role", "členění", "pád", "tvar", "zdroj"];
            var widths = header
                .Select((column, index) => rows.Select(row => row[index].Length).Append(column.Length).Max())
                .ToArray();

            var text = new StringBuilder();

            text.AppendLine("  " + Line(header, widths));

            foreach (var row in rows)
            {
                text.AppendLine("  " + Line(row, widths));
            }

            return text.ToString().TrimEnd();
        }

        private static string Line(IReadOnlyList<string> cells, IReadOnlyList<int> widths) =>
            string.Join("  ", cells.Select((cell, index) => cell.PadRight(widths[index]))).TrimEnd();

        private static string Head(ConstituentDraft constituent)
        {
            var words = constituent.Modifiers.Select(modifier => modifier.Lemma).Append(constituent.Lemma);

            return constituent.EffectivePreposition is { } preposition
                ? preposition + " " + string.Join(" ", words)
                : string.Join(" ", words);
        }

        private static string Role(ConstituentDraft constituent) => constituent.Functor is { } functor
            ? $"{functor} ({Terms.Gloss(functor)})"
            : "?";

        // Pád z rámce se značí, protože je to jediný sloupec, který v requestu nestojí — kdyby se
        // ukazoval stejně jako zadaný, nebylo by poznat, co se stane po jeho přepsání.
        private static string CaseColumn(ConstituentDraft constituent) => constituent.EffectiveCase switch
        {
            null => "—",
            { } kase when constituent.Word.Case is null => Terms.Name(kase) + " (rámec)",
            { } kase => Terms.Name(kase),
        };

        private string Form(ConstituentDraft constituent)
        {
            if (constituent.EffectiveCase is not { } kase)
            {
                return "—";
            }

            var word = constituent.Word;
            word.Case = kase;
            word.IsAfterPreposition = constituent.EffectivePreposition is not null;

            try
            {
                return _composer.GetFullForm(word).Form;
            }
            catch (Exception)
            {
                // Tvar je tu jen pro orientaci. Když se nepovede, řekne to o pár řádků níž stavba věty
                // se skutečnou chybou — dvakrát totéž hlásit nemá cenu.
                return "—";
            }
        }

        private static string Source(MetadataOrigin origin) => origin switch
        {
            MetadataOrigin.Lexicon => "slovník",
            MetadataOrigin.Guess => "odhad",
            _ => "zadáno",
        };
    }
}

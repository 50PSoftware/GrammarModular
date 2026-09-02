using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Semantics;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Globalization;

namespace Grammar.Czech.Cli.Commands
{
    /// <summary>
    /// Defines <c>vztahy</c>: the synonyms and antonyms recorded for one sense.
    /// </summary>
    /// <remarks>
    /// Takes lu_id directly rather than a lemma. The library has no general way to resolve a lemma to a
    /// lexical unit — that mapping exists today only per verb frame
    /// (<see cref="Grammar.Czech.Interfaces.ICzechValencyService.GetFrame"/>), not for every word class a
    /// relation can attach to, so building that resolution is out of scope here. Find the number on the
    /// word's page in the lexicon admin.
    /// </remarks>
    public static class RelationsCommand
    {
        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <param name="lexicon">The root's lexicon option, read when the services are built.</param>
        /// <returns>The command.</returns>
        public static Command Create(Option<FileInfo?> lexicon)
        {
            var luId = new Argument<long>("lu_id")
            {
                Description = "Identifikátor významu (lu_id), ne lemma — najdeš ho na stránce lexému v adminu.",
            };

            var synonymsOnly = new Option<bool>("--synonyma") { Description = "Vypsat jen synonyma." };
            var antonymsOnly = new Option<bool>("--antonyma") { Description = "Vypsat jen antonyma." };

            var command = new Command(
                "vztahy",
                "Sémantické vztahy — synonyma a antonyma — zaznamenané pro daný význam.")
            {
                luId, synonymsOnly, antonymsOnly,
            };

            command.SetAction(parse =>
            {
                var id = parse.GetValue(luId);
                var onlySynonyms = parse.GetValue(synonymsOnly);
                var onlyAntonyms = parse.GetValue(antonymsOnly);

                if (onlySynonyms && onlyAntonyms)
                {
                    throw new CliException(
                        "--synonyma a --antonyma najednou nejdou — bez žádného z nich se vypíšou obě.");
                }

                var provider = Services.Build(parse.GetValue(lexicon))
                    .GetRequiredService<ISemanticRelationProvider>();

                var relations = provider.GetRelations(id)
                    .Where(relation => !onlySynonyms || relation.RelationType == SemanticRelationType.Synonym)
                    .Where(relation => !onlyAntonyms || relation.RelationType == SemanticRelationType.Antonym)
                    .ToList();

                if (relations.Count == 0)
                {
                    Console.WriteLine($"Význam {id} nemá zaznamenaný žádný sémantický vztah.");

                    return 0;
                }

                foreach (var group in relations.GroupBy(relation => relation.RelationType))
                {
                    Console.WriteLine(group.Key == SemanticRelationType.Synonym ? "Synonyma:" : "Antonyma:");

                    foreach (var relation in group)
                    {
                        Console.WriteLine("  " + Describe(relation, id));
                    }
                }

                return 0;
            });

            return command;
        }

        private static string Describe(SemanticRelation relation, long anchor)
        {
            var other = $"lu_id {relation.OtherLuId(anchor)}";

            var subtype = relation.AntonymSubtype is { } value
                ? $" — {Terms.Name(value)}"
                : string.Empty;

            var strength = relation.Strength is { } strengthValue
                ? $" (síla {strengthValue.ToString("0.0", CultureInfo.InvariantCulture)})"
                : string.Empty;

            return other + subtype + strength;
        }
    }
}

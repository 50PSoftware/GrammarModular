using Grammar.Czech.Cli.Interaction;
using System.CommandLine;

namespace Grammar.Czech.Cli.Commands
{
    /// <summary>
    /// Defines <c>pojmy</c>: the explanatory help, outside a session.
    /// </summary>
    /// <remarks>
    /// The same topics <c>? role</c> gives in the review and in the session, reachable without entering
    /// either. <c>--help</c> lists the switches, which is the reference half and answers what can be
    /// written; this answers what the words in it mean, and someone who has not read the Functional
    /// Generative Description needs that before the switch list is of any use.
    /// </remarks>
    public static class TermsCommand
    {
        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command Create()
        {
            var topic = new Argument<string?>("téma")
            {
                Description = $"Které téma vysvětlit: {HelpTopics.Names}. Bez něj se vypíšou všechna.",
                Arity = ArgumentArity.ZeroOrOne,
            };

            var command = new Command("pojmy", "Vysvětlí termíny, kterými nástroj mluví.") { topic };

            command.SetAction(parse =>
            {
                var name = parse.GetValue(topic);

                if (name is null)
                {
                    foreach (var text in HelpTopics.All.Values)
                    {
                        Console.WriteLine(text);
                    }

                    return 0;
                }

                // Neznámé téma je chyba volajícího, ne prázdný výstup: bez návratového kódu by se ve
                // skriptu překlep tvářil jako téma, které nic neobsahuje.
                if (HelpTopics.Find(name) is not { } found)
                {
                    throw new CliException($"Téma '{name}' neznám. Vyber si z: {HelpTopics.Names}.");
                }

                Console.WriteLine(found);

                return 0;
            });

            return command;
        }
    }
}

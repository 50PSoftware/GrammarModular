using Grammar.Core.Enums;
using Grammar.Czech.Cli.Sentence;

namespace Grammar.Czech.Cli.Interaction
{
    /// <summary>
    /// Reads a correction — from a switch or from the review — into <see cref="DraftOverrides"/>.
    /// </summary>
    /// <remarks>
    /// One parser for both, so <c>--pad kniha=dativ</c> and <c>kniha pad=dativ</c> mean the same thing
    /// and accept the same values. That is what makes a session reproducible as a command: the dialog
    /// has no vocabulary of its own.
    /// </remarks>
    public static class OverrideParser
    {
        /// <summary>
        /// Applies a correction written the way a switch writes it: a word, then a value.
        /// </summary>
        /// <param name="overrides">The record to write into.</param>
        /// <param name="property">The property the switch stands for, unaccented.</param>
        /// <param name="assignment">The argument of the switch, as <c>slovo=hodnota</c>.</param>
        /// <exception cref="CliException">Thrown when the argument or the value cannot be read.</exception>
        public static void Assign(DraftOverrides overrides, string property, string assignment)
        {
            var (target, value) = Split(assignment,
                $"Přepínač --{property} se zadává jako slovo=hodnota, třeba --{property} kniha=…");

            Set(overrides, target, property, value);
        }

        /// <summary>
        /// Applies a correction written the way the review takes it: a target, then one or more pairs.
        /// </summary>
        /// <param name="line">The line the user typed.</param>
        /// <param name="lemmas">The lemmas of the clause, for resolving a target by name.</param>
        /// <param name="overrides">The record to write into.</param>
        /// <param name="predicatePosition">The position of the predicate, which takes corrections of its own.</param>
        /// <exception cref="CliException">Thrown when the line cannot be read.</exception>
        /// <remarks>
        /// Nothing is written until the whole line reads, so a refused correction leaves the state it
        /// found. The review rebuilds the draft after every accepted line and could not undo a half
        /// applied one.
        /// </remarks>
        public static void Apply(
            string line,
            IReadOnlyList<string> lemmas,
            DraftOverrides overrides,
            int predicatePosition = 0)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length < 2)
            {
                throw new CliException("Oprava se píše jako: <cíl> <co>=<jak>, třeba 2 role=ADDR. Nápověda je '?'.");
            }

            var target = Resolve(parts[0], lemmas);

            if (target != "p" && target == predicatePosition.ToString())
            {
                throw new CliException(
                    $"'{parts[0]}' je přísudek — ten se opravuje přes 'p', třeba p cas=minuly.");
            }

            // Nejdřív se přečte celý řádek, teprve pak se zapisuje: kdyby druhá dvojice neprošla,
            // s první už by se nedalo nic dělat — návrh se po každé přijaté opravě staví znovu.
            var writes = parts
                .Skip(1)
                .Select(pair =>
                {
                    var (property, value) = Split(pair, $"'{pair}' není dvojice <co>=<jak>.");

                    return Prepare(overrides, target, Terms.Plain(property), value);
                })
                .ToList();

            foreach (var write in writes)
            {
                write();
            }
        }

        // Lemma se překládá na pořadí, protože jen to je jednoznačné: stejné slovo může ve větě stát
        // dvakrát a pak se na ně jménem ukázat nedá.
        private static string Resolve(string target, IReadOnlyList<string> lemmas)
        {
            if (target is "p" or "P")
            {
                return "p";
            }

            if (int.TryParse(target, out var position))
            {
                return position >= 1 && position <= lemmas.Count
                    ? position.ToString()
                    : throw new CliException($"Slovo číslo {position} ve větě není; zadaných je {lemmas.Count}.");
            }

            var matches = lemmas
                .Select((lemma, index) => (lemma, position: index + 1))
                .Where(candidate => string.Equals(candidate.lemma, target, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return matches.Count switch
            {
                1 => matches[0].position.ToString(),
                0 => throw new CliException($"Slovo '{target}' ve větě není."),
                _ => throw new CliException(
                    $"Slovo '{target}' je ve větě víckrát ({string.Join(", ", matches.Select(match => match.position))}) "
                    + "— ukaž na ně pořadovým číslem."),
            };
        }

        private static void Set(DraftOverrides overrides, string target, string property, string value) =>
            Prepare(overrides, target, property, value)();

        // Hodnota se přečte hned a zapíše se až vrácenou akcí, aby šlo celý řádek nejdřív ověřit.
        private static Action Prepare(DraftOverrides overrides, string target, string property, string value)
        {
            if (target is "p")
            {
                return PreparePredicate(overrides, property, value);
            }

            var word = overrides.For(target);

            return property switch
            {
                "role" => Assigning(Terms.ParseFunctor(value), parsed => word.Functor = parsed),
                "cleneni" => Assigning(Terms.ParseStatus(value), parsed => word.Status = parsed),
                "pad" => Assigning(Terms.ParseCase(value), parsed => word.Case = parsed),
                "rod" => Assigning(Terms.ParseGender(value), parsed => word.Gender = parsed),
                "cislo" => Assigning(Terms.ParseNumber(value), parsed => word.Number = parsed),
                "vzor" => Assigning(value, parsed => word.Pattern = parsed),
                "zivotne" => Assigning(Terms.ParseYesNo(value), parsed => word.IsAnimate = parsed),
                "predlozka" => Assigning(value, parsed => word.Preposition = parsed),
                "privlastek" => Assigning(value, word.Modifiers.Add),
                _ => throw new CliException(
                    $"U slova neznám '{property}'. Jde: role, cleneni, pad, rod, cislo, vzor, "
                    + "zivotne, predlozka, privlastek."),
            };
        }

        private static Action PreparePredicate(DraftOverrides overrides, string property, string value) => property switch
        {
            "cas" => Assigning(Terms.ParseTense(value), parsed => overrides.Tense = parsed),
            "zpusob" => Assigning(Terms.ParseMood(value), parsed => overrides.Mood = parsed),
            "rod" => Assigning(Terms.ParseVoice(value), parsed => overrides.Voice = parsed),
            "vid" => Assigning(Terms.ParseAspect(value), parsed => overrides.Aspect = parsed),
            "osoba" => Assigning(Terms.ParsePerson(value), parsed => overrides.Person = parsed),
            "cislo" => Assigning(Terms.ParseNumber(value), parsed => overrides.Number = parsed),
            "zapor" => Assigning(Terms.ParseYesNo(value), parsed => overrides.IsNegative = parsed),
            "podmet" => Assigning(ParseSubjectDrop(value), parsed => overrides.DropSubject = parsed),
            "zvratne" => Assigning(ParseReflexive(value), parsed => overrides.ReflexiveType = parsed),
            "ramec" => Assigning(value, parsed => overrides.FrameLabel = parsed),
            "sloveso" => Assigning(value, parsed => overrides.PredicateLemma = parsed),
            "typ" => Assigning(Terms.ParseSentenceType(value), parsed => overrides.SentenceType = parsed),
            "konec" => Assigning(value, parsed => overrides.Terminator = parsed),
            _ => throw new CliException(
                $"U přísudku neznám '{property}'. Jde: cas, zpusob, rod, vid, osoba, cislo, "
                + "zapor, podmet, zvratne, ramec, sloveso, typ, konec."),
        };

        // Vypuštění podmětu je vlastnost věty, ne slova: 'čtu' proti 'já čtu' je táž věta jednou
        // nepříznaková a jednou důrazová.
        private static bool ParseSubjectDrop(string value) => Terms.Plain(value) switch
        {
            "vypustit" or "vypusteny" or "ano" => true,
            "ponechat" or "vyjadreny" or "ne" => false,
            _ => throw new CliException(
                $"Z '{value}' nepoznám, co s podmětem. Jde: vypustit, ponechat."),
        };

        private static Action Assigning<TValue>(TValue parsed, Action<TValue> write) => () => write(parsed);

        /// <summary>
        /// Reads the reflexive particle written as a Czech word.
        /// </summary>
        /// <param name="value">The text to read.</param>
        /// <returns>The reflexive type.</returns>
        /// <exception cref="CliException">Thrown when the text names no reflexive type.</exception>
        /// <remarks>
        /// Derived reflexivity is all a switch can state. Whether a verb is a reflexivum tantum or a
        /// deagentive does not follow from the particle, and that is what the dictionary entry is for.
        /// </remarks>
        public static ReflexiveType ParseReflexive(string value) => Terms.Plain(value) switch
        {
            "se" => ReflexiveType.DerivedReflexive_Se,
            "si" => ReflexiveType.DerivedBenefactive_Si,
            "ne" or "zadne" => ReflexiveType.None,
            _ => Enum.TryParse<ReflexiveType>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new CliException($"Z '{value}' nepoznám zvratnost. Jde: se, si, ne."),
        };

        private static (string Target, string Value) Split(string text, string complaint)
        {
            var separator = text.IndexOf('=');

            if (separator <= 0 || separator == text.Length - 1)
            {
                throw new CliException(complaint);
            }

            return (text[..separator].Trim(), text[(separator + 1)..].Trim());
        }
    }
}

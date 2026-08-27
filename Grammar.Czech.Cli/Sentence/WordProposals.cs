using Grammar.Core.Enums;
using Grammar.Czech.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Collects the words the dictionary did not know, so that they can be put into it later.
    /// </summary>
    /// <remarks>
    /// A word typed at this tool that the dictionary does not hold is the most useful thing it ever
    /// sees: somebody wanted it, and the tool had to make its gender and pattern up. Recording it costs
    /// nothing and turns a session into a list of words worth adding.
    /// <para>
    /// What it does not do is write to the dictionary, and it cannot. The SQLite file is a read-only
    /// replica of a MySQL copy edited through the PHP admin; identifiers are handed out by the server,
    /// the API only reads, and <c>lexikon pull</c> or <c>lexikon build</c> overwrites the local file
    /// whole. A row inserted here would survive until the next pull and then be gone — a feature that
    /// silently discards its own result.
    /// </para>
    /// <para>
    /// So this file is a proposal and nothing more. <c>lexikon navrhy</c> reads it and writes a seed
    /// draft, where identifiers come from the seeds on disk and a person decides what goes in. That
    /// keeps the server the only authority on identifiers and a human the only authority on what the
    /// dictionary contains — which is also why <c>source</c> is left empty here rather than filled with
    /// something plausible. Provenance is what the licensing of this project rests on, and a word
    /// invented in a session did not come from the Internetová jazyková příručka.
    /// </para>
    /// </remarks>
    public sealed class WordProposals
    {
        /// <summary>
        /// The environment variable naming the file, for anyone who wants it somewhere else.
        /// </summary>
        public const string PathVariable = "GRAMMAR_CZECH_NAVRHY";

        private static readonly JsonSerializerOptions Format = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            // Jménem, ne číslem. Soubor si má člověk přečíst a rozhodnout se nad ním, a 'Category: 4'
            // je odpověď jen pro toho, kdo má po ruce zdrojáky — navíc by se posunula, kdyby do enumu
            // někdo přidal člen doprostřed. Přesně to dělá i slovník ve svých sloupcích.
            Converters = { new JsonStringEnumConverter() },
        };

        private readonly string _path;

        /// <summary>
        /// Initializes a new instance of the <see cref="WordProposals"/> type.
        /// </summary>
        /// <param name="path">The file to keep the proposals in, or <see langword="null"/> for the default.</param>
        /// <remarks>
        /// Resolution order: an explicit <paramref name="path"/>, then the <c>navrhy</c> key in a
        /// <c>lexikon.json</c> a caller's own working directory sits under (see
        /// <see cref="LexiconSettings.ProposalsPath"/>), then the environment variable, then the fixed
        /// application-directory default — the same order <c>ToolSettings</c> in the lexicon tool
        /// already uses for its own settings, and the same reason: a value written down for the project
        /// is more deliberate than one left in a shell, so the file wins when both are set.
        /// </remarks>
        public WordProposals(string? path = null)
        {
            _path = path
                ?? LexiconSettings.ProposalsPath()
                ?? Environment.GetEnvironmentVariable(PathVariable)
                ?? DefaultPath();
        }

        /// <summary>
        /// Gets the file the proposals are kept in.
        /// </summary>
        public string Path => _path;

        /// <summary>
        /// Reads what has been collected so far.
        /// </summary>
        /// <returns>The proposals, oldest first, empty when there are none.</returns>
        public IReadOnlyList<WordProposal> Read()
        {
            if (!File.Exists(_path))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<WordProposal>>(File.ReadAllText(_path), Format) ?? [];
            }
            catch (JsonException exception)
            {
                throw new CliException(
                    $"Soubor návrhů {_path} nejde přečíst: {exception.Message}\n\n"
                    + "Buď ho oprav, nebo smaž — sbírá se do něj znovu.");
            }
        }

        /// <summary>
        /// Records a word, or leaves the record alone when it is already there.
        /// </summary>
        /// <param name="lemma">The lemma as the user wrote it.</param>
        /// <param name="word">What the tool made of it.</param>
        /// <returns><see langword="true"/> when the word was new to the file.</returns>
        /// <remarks>
        /// Deliberately not overwriting an existing record: the first sighting of a word may have been
        /// answered by hand in a session, and a later one that guessed from the ending would throw those
        /// answers away.
        /// </remarks>
        public bool Add(string lemma, CzechWordRequest word)
        {
            var proposals = Read().ToList();

            if (proposals.Any(proposal => Terms.LemmaComparer.Equals(proposal.Lemma, lemma)))
            {
                return false;
            }

            proposals.Add(new WordProposal
            {
                Lemma = lemma,
                Category = word.WordCategory,
                Gender = word.Gender,
                Pattern = word.Pattern,
                IsAnimate = word.IsAnimate,
                VerbClass = word.VerbClass,
                Aspect = word.Aspect,
                SeenAt = DateTimeOffset.Now,
            });

            // Sběr je vedlejší užitek, ne práce nástroje. Když soubor drží druhý běžící `gramatika`,
            // je správná odpověď to slovo nezapsat — ne odmítnout postavit větu, o kterou někdo žádal.
            try
            {
                Write(proposals);
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Replaces the whole collection.
        /// </summary>
        /// <param name="proposals">The proposals to keep.</param>
        public void Write(IReadOnlyList<WordProposal> proposals)
        {
            var directory = System.IO.Path.GetDirectoryName(_path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Přes dočasný soubor a přejmenování: zápis na místo znamená, že proces, který v tu chvíli
            // čte, uvidí půlku souboru. Přejmenování je atomické a čtenář uvidí buď starý, nebo nový.
            var temporary = _path + ".tmp";

            File.WriteAllText(temporary, JsonSerializer.Serialize(proposals, Format));
            File.Move(temporary, _path, overwrite: true);
        }

        /// <summary>
        /// Forgets everything collected.
        /// </summary>
        public void Clear()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        // Vedle konfigurace uživatele, ne do dočasných souborů: slovo, které stojí za doplnění do
        // slovníku, nemá zmizet se zavřením terminálu.
        private static string DefaultPath() => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "gramatika",
            "navrhy.json");
    }

    /// <summary>
    /// One word the dictionary did not hold, with whatever was worked out about it.
    /// </summary>
    public sealed class WordProposal
    {
        /// <summary>
        /// Gets or sets the lemma, as it was written.
        /// </summary>
        public string Lemma { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the word class.
        /// </summary>
        public WordCategory? Category { get; set; }

        /// <summary>
        /// Gets or sets the gender.
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Gets or sets the inflection pattern.
        /// </summary>
        public string? Pattern { get; set; }

        /// <summary>
        /// Gets or sets whether the noun is animate.
        /// </summary>
        public bool? IsAnimate { get; set; }

        /// <summary>
        /// Gets or sets the conjugation class of a verb.
        /// </summary>
        public VerbClass? VerbClass { get; set; }

        /// <summary>
        /// Gets or sets the aspect of a verb.
        /// </summary>
        public VerbAspect? Aspect { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a person has confirmed the values above.
        /// </summary>
        /// <remarks>
        /// Everything here starts as a guess from the ending. Confirmation is what a session records
        /// when it asks, and what tells a reviewed proposal from an unreviewed one downstream.
        /// </remarks>
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a person has looked at this proposal and decided it
        /// does not belong in the dictionary.
        /// </summary>
        /// <remarks>
        /// Deliberately separate from <see cref="IsConfirmed"/> being <see langword="false"/>, which
        /// also covers a proposal nobody has looked at yet — the two used to be the same bit, so a
        /// batch source's own track record could not be told from its unreviewed backlog. Rejecting one
        /// does not remove it: the record of what was tried and turned down is what a source's accuracy
        /// is measured against, not just a single proposal's own fate.
        /// </remarks>
        public bool IsRejected { get; set; }

        /// <summary>
        /// Gets or sets which seed file this proposal was already drafted into, or
        /// <see langword="null"/> when it has not been exported yet.
        /// </summary>
        /// <remarks>
        /// Set by <c>lexikon navrhy</c>, never by <c>gramatika</c> or <c>rozbor</c>. A proposal is not
        /// removed from the queue once it is exported — the same reason a rejected one is not removed
        /// either, the record of what was drafted where is worth more than the entry — so without this,
        /// a second <c>lexikon navrhy</c> run would draft the same lemma into a second seed just because
        /// it still sits in <c>navrhy.json</c>.
        /// </remarks>
        public string? ExportedTo { get; set; }

        /// <summary>
        /// Gets or sets a free note, for whatever the guess could not carry.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets when the word was first seen.
        /// </summary>
        public DateTimeOffset SeenAt { get; set; }

        /// <summary>
        /// Describes the proposal in one line, for a listing.
        /// </summary>
        /// <returns>The description.</returns>
        public string Describe()
        {
            List<string> parts = [Category is { } category ? Terms.Name(category) : "slovní druh neznámý"];

            if (Gender is { } gender)
            {
                parts.Add(Terms.Name(gender));
            }

            if (Pattern is { } pattern)
            {
                parts.Add("vzor " + pattern);
            }

            if (IsAnimate is { } animate)
            {
                parts.Add(animate ? "životné" : "neživotné");
            }

            if (Aspect is { } aspect)
            {
                parts.Add(Terms.Name(aspect));
            }

            var state = IsConfirmed ? "potvrzeno" : IsRejected ? "zamítnuto" : "odhad";

            if (ExportedTo is { } seed)
            {
                state += $", v {seed}";
            }

            return $"{Lemma} — {string.Join(", ", parts)} [{state}]";
        }
    }
}

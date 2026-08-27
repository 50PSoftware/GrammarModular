using System.IO.Compression;
using System.Xml.Linq;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Reads the running text out of a file, whether it is already plain text or a word processor's
    /// own zipped-XML format.
    /// </summary>
    /// <remarks>
    /// Nothing downstream needs layout, styles, tables-as-tables or embedded objects — the whole
    /// pipeline only ever wants one flat string to hand to <see cref="Tokenizer.CountTokens"/> — so
    /// this reads each format's own zip archive directly via the BCL's <c>System.IO.Compression</c>
    /// and <c>System.Xml.Linq</c> rather than pull in a full document-object-model package
    /// (<c>DocumentFormat.OpenXml</c> alone is a multi-megabyte dependency for .docx and still would
    /// not cover .odt) for what a few lines of XML digging already does.
    /// </remarks>
    public static class DocumentReader
    {
        /// <summary>
        /// Reads the text of the file at <paramref name="path"/>, dispatching on its extension.
        /// </summary>
        /// <param name="path">The file to read.</param>
        /// <returns>The file's running text, paragraphs joined by newlines.</returns>
        public static string ReadText(string path) => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".docx" => ReadDocx(path),
            ".odt" => ReadOdt(path),
            _ => File.ReadAllText(path),
        };

        // word/document.xml holds every paragraph as w:p, each run's text as w:t — concatenating a
        // paragraph's own w:t descendants and joining paragraphs with a newline keeps sentence and
        // paragraph boundaries roughly where FindLikelyProperNouns's crude sentence-start heuristic
        // expects them, without needing to understand runs, styles or anything else in the document.
        private static string ReadDocx(string path)
        {
            using var archive = ZipFile.OpenRead(path);
            var entry = archive.GetEntry("word/document.xml")
                ?? throw new InvalidOperationException($"'{path}' neobsahuje word/document.xml — není to platný .docx.");

            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            var paragraphs = document.Descendants(w + "p")
                .Select(paragraph => string.Concat(paragraph.Descendants(w + "t").Select(t => t.Value)));

            return string.Join("\n", paragraphs);
        }

        // content.xml stores a paragraph's text as direct content mixed with text:span/text:s/text:tab,
        // so XElement.Value — which already concatenates all descendant text — is enough on its own; no
        // need to walk a run structure the way .docx's w:t elements require.
        private static string ReadOdt(string path)
        {
            using var archive = ZipFile.OpenRead(path);
            var entry = archive.GetEntry("content.xml")
                ?? throw new InvalidOperationException($"'{path}' neobsahuje content.xml — není to platný .odt.");

            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            XNamespace text = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

            var paragraphs = document.Descendants()
                .Where(element => element.Name == text + "p" || element.Name == text + "h")
                .Select(element => element.Value);

            return string.Join("\n", paragraphs);
        }
    }
}

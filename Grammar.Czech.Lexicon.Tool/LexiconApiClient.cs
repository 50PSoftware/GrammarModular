using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Fetches the lexicon from the API, a page at a time.
    /// </summary>
    public sealed class LexiconApiClient : IDisposable
    {
        private const string UserAgent = "GrammarModular-LexiconTool/1.0";

        private readonly HttpClient _client;
        private readonly Uri _endpoint;
        private readonly string? _token;
        private readonly int _pageSize;
        private bool _useCurl;

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconApiClient"/> type.
        /// </summary>
        /// <param name="endpoint">The API endpoint serving the lexicon.</param>
        /// <param name="token">The bearer token, or <see langword="null"/> when the API is open.</param>
        /// <param name="pageSize">How many rows to ask for at a time.</param>
        /// <param name="handler">
        /// The transport to use, or <see langword="null"/> for the default. Supplied by the tests, which
        /// have to reach the paging loop and its guards without opening a socket. Supplying one also turns
        /// off the curl transport below, since a fake handler has nothing for curl to hit.
        /// </param>
        public LexiconApiClient(Uri endpoint, string? token, int pageSize, HttpMessageHandler? handler = null)
        {
            _client = handler is null ? new HttpClient() : new HttpClient(handler);
            _endpoint = endpoint;
            _token = token;
            _pageSize = pageSize;

            // A dictionary is large and the server has to read it out of MySQL, so the default hundred
            // seconds is short rather than generous here.
            _client.Timeout = TimeSpan.FromMinutes(10);
            _client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse(UserAgent));

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Windows only, and only for the real transport: a WAF in front of the API has been seen to
            // challenge .NET's own TLS handshake while letting curl's through unchallenged, on the very
            // same machine and account. curl ships with Windows since 10 1803, so this is not an extra
            // dependency — it is a fallback for when HttpClient's handshake is the one thing standing
            // between the caller and the dictionary. See .claude/memory/wedos-protection-altcha-slovnik.md.
            _useCurl = handler is null && OperatingSystem.IsWindows();
        }

        /// <summary>
        /// Fetches every table of the lexicon, in dependency order.
        /// </summary>
        /// <returns>The pages, as they arrive.</returns>
        public IEnumerable<LexiconPage> Fetch()
        {
            foreach (var table in LexiconSchema.Tables)
            {
                string? after = null;

                do
                {
                    var page = FetchPage(table, after);

                    // A page that names a different table than the one asked for means the request was
                    // routed somewhere unexpected, and importing it would file rows under the wrong table.
                    if (page.Table != table.Name)
                    {
                        throw new InvalidOperationException(
                            $"Server vrátil tabulku '{page.Table}', ptali jsme se na '{table.Name}'.");
                    }

                    // A server that keeps answering with the same next_after would otherwise pull
                    // forever, reinserting the same rows until the primary key rejected them.
                    if (page.NextAfter is not null && page.NextAfter == after)
                    {
                        throw new InvalidOperationException(
                            $"Tabulka '{table.Name}': server vrací pořád stejné next_after ('{after}'). "
                            + "Stránkování na serveru je rozbité.");
                    }

                    after = page.NextAfter;

                    yield return page;
                }
                while (after is not null);
            }
        }

        /// <summary>
        /// Releases the underlying client.
        /// </summary>
        public void Dispose() => _client.Dispose();

        private LexiconPage FetchPage(LexiconTable table, string? after)
        {
            // Appended rather than replaced, so an endpoint that already carries a query — a front
            // controller taking ?route=lexicon, say — keeps it.
            var separator = string.IsNullOrEmpty(_endpoint.Query) ? "?" : "&";
            var query = $"{separator}table={Uri.EscapeDataString(table.Name)}&limit={_pageSize}";

            if (after is not null)
            {
                query += $"&after={Uri.EscapeDataString(after)}";
            }

            var url = new Uri(_endpoint.AbsoluteUri + query);

            if (_useCurl)
            {
                try
                {
                    return ParseResponse(url, FetchViaCurl(url));
                }
                catch (Win32Exception)
                {
                    // No curl on PATH. Falls back for the rest of this client's lifetime rather than
                    // retrying every page — the outcome will not change page to page.
                    _useCurl = false;
                }
            }

            return ParseResponse(url, FetchViaHttpClient(url));
        }

        private (int Status, string Body) FetchViaHttpClient(Uri url)
        {
            using var response = _client.GetAsync(url).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            return ((int)response.StatusCode, body);
        }

        // Shells out rather than reimplementing curl's handshake: the point is to get exactly curl's own
        // TLS stack, not something that merely resembles it.
        private (int Status, string Body) FetchViaCurl(Uri url)
        {
            const string statusMarker = "___LEXICON_STATUS___";

            var config = new StringBuilder();
            config.AppendLine("silent");
            config.AppendLine("show-error");
            config.AppendLine($"header = \"User-Agent: {UserAgent}\"");

            if (!string.IsNullOrEmpty(_token))
            {
                config.AppendLine($"header = \"Authorization: Bearer {_token}\"");
            }

            config.AppendLine($"url = \"{url.AbsoluteUri}\"");
            config.AppendLine($"write-out = \"\\n{statusMarker}%{{http_code}}\"");

            // "-K -" reads the config from stdin instead of a file. The token never touches the command
            // line, where every account on the machine can read it back out of the process list, and
            // never touches disk either — it only ever lives in this process's memory and the pipe to curl.
            var startInfo = new ProcessStartInfo("curl", "-K -")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("curl se nepodařilo spustit.");

            // Reading has to start before the write, not after: the config is small enough here that it
            // would not deadlock either order, but a response that grew past the pipe buffer while nobody
            // was reading it yet is exactly the mistake that hung an earlier draft of this fix.
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            process.StandardInput.Write(config.ToString());
            process.StandardInput.Close();

            process.WaitForExit();

            var output = stdoutTask.GetAwaiter().GetResult();
            var error = stderrTask.GetAwaiter().GetResult();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"curl selhal (kód {process.ExitCode}): {error.Trim()}");
            }

            var markerIndex = output.LastIndexOf(statusMarker, StringComparison.Ordinal);

            if (markerIndex < 0)
            {
                throw new InvalidOperationException("curl nevrátil status kód, který mu byl zadán.");
            }

            var body = output[..markerIndex].TrimEnd('\r', '\n');
            var status = int.Parse(
                output[(markerIndex + statusMarker.Length)..].Trim(),
                CultureInfo.InvariantCulture);

            return (status, body);
        }

        private static LexiconPage ParseResponse(Uri url, (int Status, string Body) response)
        {
            if (response.Status is < 200 or >= 300)
            {
                throw new InvalidOperationException(
                    $"{url} odpovědělo {response.Status} {(HttpStatusCode)response.Status}. {response.Body}"
                        .Trim());
            }

            return JsonSerializer.Deserialize<LexiconPage>(response.Body, LexiconPage.SerializerOptions)
                ?? throw new InvalidOperationException($"{url} vrátilo prázdnou odpověď.");
        }
    }
}

<?php

declare(strict_types=1);

/**
 * Serves the central MySQL lexicon as the paged JSON that Grammar.Czech.Lexicon.Tool imports.
 *
 * One request returns one page of one table:
 *
 *   GET /api/?table=lemma_entry&limit=5000[&after=<key>]
 *   Authorization: Bearer <token>
 *
 *   {"table":"lemma_entry","columns":[...],"rows":[[...],[...]],"next_after":"5000"}
 *
 * Rows are arrays rather than objects and the column names are stated once. At the size a Czech
 * dictionary reaches, repeating twenty-four keys per row is most of the payload — and the single
 * header doubles as the contract: the importer refuses a page whose columns are not the ones its
 * schema expects, in that order, which is what stops a reordered column from being written into the
 * wrong place and validating cleanly.
 *
 * Paging is by key and not by offset. An offset re-counts the skipped rows on every request and shifts
 * when the dictionary is edited mid-pull, which drops or repeats rows without saying so.
 *
 * Requires PHP 8.1 or newer, for the never return type.
 *
 * Configuration is LEXICON_MYSQL_DSN, LEXICON_MYSQL_USER and LEXICON_MYSQL_PASSWORD, read by env.php
 * from the real environment or from Php/.env.php. See .env.php.example.
 *
 * The bearer token is not configuration: each caller presents a personal token minted in the admin
 * (see admin/schema/api_token.mysql.sql and the "Tokeny" page), and authorize() looks its hash up in
 * the api_token table rather than comparing against a single shared secret.
 *
 * Deployed as www/api/index.php, with the admin at www/index.php beside it. The shared includes and
 * the secrets sit one level up and are denied by .htaccess; see ../.htaccess.
 *
 * One deployment note, because it costs an afternoon when missed: Apache with CGI or FPM strips the
 * Authorization header before PHP sees it, so every request arrives unauthenticated. Either set
 * CGIPassAuth On for the directory, or restore it with:
 *
 *   RewriteEngine On
 *   RewriteCond %{HTTP:Authorization} .
 *   RewriteRule .* - [E=HTTP_AUTHORIZATION:%{HTTP:Authorization}]
 */

// Where env.php, schema-tables.php and .env live. They sit one level up by default, which keeps them
// above the document root when the vhost points at this api/ directory.
//
// On shared hosting the document root is usually fixed — Wedos serves www/ and will not be pointed
// deeper — so there the includes go beside www/ rather than above this file, and this is the single
// line to change. See the deployment section of the README.
$lexiconIncludes = __DIR__ . '/..';

require $lexiconIncludes . '/env.php';
require $lexiconIncludes . '/schema-tables.php';

const MAX_LIMIT = 20000;
const DEFAULT_LIMIT = 5000;

header('Content-Type: application/json; charset=utf-8');

try {
    respond(handle());
} catch (Throwable $exception) {
    // The message is deliberately not the exception's own: a PDO failure carries the DSN, the user and
    // often the statement, none of which belongs in a response to an unauthenticated caller.
    error_log('api: ' . $exception->getMessage());
    fail(500, 'Interní chyba serveru.');
}

/**
 * Reads the request, checks it, and returns the page to send.
 */
function handle(): array
{
    $pdo = connect();
    authorize($pdo);

    $table = (string) ($_GET['table'] ?? '');

    if (!array_key_exists($table, LEXICON_TABLES)) {
        fail(400, "Neznámá tabulka '$table'.");
    }

    $columns = LEXICON_TABLES[$table];
    $keyColumn = $columns[0];

    // Clamped rather than trusted. An unbounded limit lets one request ask the server to materialise the
    // whole dictionary in memory, which is a denial of service dressed as a legitimate parameter.
    $limit = (int) ($_GET['limit'] ?? DEFAULT_LIMIT);
    $limit = max(1, min($limit, MAX_LIMIT));

    $after = isset($_GET['after']) ? (string) $_GET['after'] : null;

    $quotedColumns = implode(', ', array_map(fn (string $column): string => "`$column`", $columns));

    // Keyset paging compares the primary key in its own type. The key travels as text because one
    // parameter has to cover both kinds, and is converted back here rather than cast in SQL: casting the
    // column — ORDER BY CAST(`id` AS CHAR) — would compare consistently but make the primary key index
    // unusable, turning every page of a hundred-thousand-row table into a full scan and a filesort.
    //
    // Filtering and ordering must agree on the comparison. Ordering numerically while filtering as text
    // is a mismatch small data hides: with keys one to twelve and a page of five, the second request
    // asks for keys after '5' and never returns '10', '11' or '12', because as text they sort below it.
    $sql = "SELECT $quotedColumns FROM `$table` ";
    $sql .= $after === null ? '' : "WHERE `$keyColumn` > ? ";
    $sql .= "ORDER BY `$keyColumn` LIMIT $limit";

    $statement = $pdo->prepare($sql);

    if ($after === null) {
        $statement->execute();
    } else {
        // Bound with the type the column holds. lexicon_meta is the only table keyed by text; binding a
        // numeric key as a string would still work through MySQL's coercion, but binding it as an
        // integer is what keeps the plan on the index.
        $isTextKey = in_array($table, LEXICON_TEXT_KEY_TABLES, true);
        $statement->bindValue(1, $isTextKey ? $after : (int) $after, $isTextKey ? PDO::PARAM_STR : PDO::PARAM_INT);
        $statement->execute();
    }

    $rows = $statement->fetchAll(PDO::FETCH_NUM);
    $count = count($rows);

    return [
        'table' => $table,
        'columns' => $columns,
        'rows' => $rows,

        // A short page is the last one. next_after stays null there, which is what ends the client's
        // loop; repeating the previous key instead would spin it forever, and the client checks for
        // exactly that.
        'next_after' => $count === $limit ? (string) $rows[$count - 1][0] : null,
    ];
}

function authorize(PDO $pdo): void
{
    $presented = preg_match('/^Bearer\s+(.+)$/i', trim(readAuthorizationHeader()), $matches) === 1
        ? $matches[1]
        : '';

    if ($presented === '') {
        fail(401, 'Neplatný token.');
    }

    // The token itself is never stored, only its hash — a leaked api_token table hands over nothing
    // usable, the same reasoning the admin's own sign-in applies to passwords.
    $hash = hash('sha256', $presented);

    $statement = $pdo->prepare('SELECT id FROM api_token WHERE token_hash = ?');
    $statement->execute([$hash]);
    $row = $statement->fetch();

    if ($row === false) {
        fail(401, 'Neplatný token.');
    }

    $pdo->prepare('UPDATE api_token SET last_used_at = NOW() WHERE id = ?')->execute([$row['id']]);
}

/**
 * Finds the Authorization header, wherever this particular server put it.
 *
 * Getting at it is the single most fragile thing here, because where it lands depends on the SAPI and
 * on how the vhost is configured. Each source is tried until one yields something non-empty rather
 * than merely set: the Apache rewrite that restores the header leaves HTTP_AUTHORIZATION defined and
 * empty on some setups while the real value sits under the REDIRECT_ prefix, and a plain ?? chain
 * would stop at the empty one and reject every request with a correct token.
 */
function readAuthorizationHeader(): string
{
    foreach (['HTTP_AUTHORIZATION', 'REDIRECT_HTTP_AUTHORIZATION'] as $key) {
        if (($_SERVER[$key] ?? '') !== '') {
            return (string) $_SERVER[$key];
        }
    }

    // Available under mod_php and, since PHP 7.3, under FPM too. It reads the header from the SAPI
    // directly, which is what works when the vhost has not been told to pass it through at all.
    if (function_exists('getallheaders')) {
        foreach (getallheaders() as $name => $value) {
            if (strcasecmp($name, 'Authorization') === 0 && $value !== '') {
                return (string) $value;
            }
        }
    }

    return '';
}

function connect(): PDO
{
    $dsn = lexicon_config('LEXICON_MYSQL_DSN');

    if ($dsn === '') {
        error_log('api: LEXICON_MYSQL_DSN není nastaven.');
        fail(500, 'Server není nastavený.');
    }

    $pdo = new PDO($dsn, lexicon_config('LEXICON_MYSQL_USER'), lexicon_config('LEXICON_MYSQL_PASSWORD'), [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,

        // Both matter for the wire format. Without them every value comes back as a string, and the
        // importer would write "1" where the column expects a number and, worse, the empty string where
        // the row holds a genuine NULL.
        PDO::ATTR_EMULATE_PREPARES => false,
        PDO::ATTR_STRINGIFY_FETCHES => false,
    ]);

    // Without this the connection can negotiate latin1 and every Czech diacritic arrives mangled —
    // silently, because mangled text is still text. The DSN should carry charset=utf8mb4 as well.
    $pdo->query('SET NAMES utf8mb4')?->closeCursor();

    return $pdo;
}

function respond(array $page): never
{
    // Unescaped unicode so the Czech stays readable and the payload stays small; the response declares
    // UTF-8 and the C# client reads it as such.
    echo json_encode($page, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_THROW_ON_ERROR);
    exit(0);
}

function fail(int $status, string $message): never
{
    http_response_code($status);
    echo json_encode(['error' => $message], JSON_UNESCAPED_UNICODE);
    exit(1);
}

<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Shared plumbing for the lexicon admin: configuration, database, authentication, form helpers.
 *
 * The admin writes the dictionary that the API then serves and the C# tool pulls, so it shares the
 * column map and the permitted values with both — see ../schema-tables.php. It deliberately does not
 * re-implement the checks in LexiconValidator: two hand-maintained copies of the same rules drift,
 * and the validator already runs as the gate on every pull, so anything this lets through is caught
 * before it can reach a local lexicon.
 *
 * What it does enforce is the handful of things that cannot be repaired afterwards — the lookup key,
 * the enumerated values, and the referential shape of a frame.
 */

require_once __DIR__ . '/../env.php';
require_once __DIR__ . '/../schema-tables.php';

const ADMIN_PAGE_SIZE = 40;

// ─────────────────────────────────────────────────────────────────────────────
// Configuration and database
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Opens the lexicon database.
 */
function admin_db(): PDO
{
    static $pdo = null;

    if ($pdo instanceof PDO) {
        return $pdo;
    }

    $dsn = lexicon_config('LEXICON_MYSQL_DSN');

    if ($dsn === '') {
        admin_fail('Chybí LEXICON_MYSQL_DSN. Doplň ho do .env.php.');
    }

    $pdo = new PDO($dsn, lexicon_config('LEXICON_MYSQL_USER'), lexicon_config('LEXICON_MYSQL_PASSWORD'), [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_EMULATE_PREPARES => false,
        PDO::ATTR_STRINGIFY_FETCHES => false,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
    ]);

    // Without this the connection can negotiate latin1 and every Czech diacritic is mangled on the way
    // in — silently, because mangled text is still text.
    $pdo->query('SET NAMES utf8mb4')?->closeCursor();

    return $pdo;
}

/**
 * Runs a query and returns every row.
 *
 * @param list<mixed> $parameters
 * @return list<array<string, mixed>>
 */
function admin_all(string $sql, array $parameters = []): array
{
    $statement = admin_db()->prepare($sql);
    $statement->execute($parameters);

    return $statement->fetchAll();
}

/**
 * Runs a query and returns the first row, or null.
 *
 * @param list<mixed> $parameters
 * @return array<string, mixed>|null
 */
function admin_one(string $sql, array $parameters = []): ?array
{
    return admin_all($sql, $parameters)[0] ?? null;
}

/**
 * Runs a statement that returns no rows.
 *
 * @param list<mixed> $parameters
 */
function admin_run(string $sql, array $parameters = []): void
{
    admin_db()->prepare($sql)->execute($parameters);
}

// ─────────────────────────────────────────────────────────────────────────────
// Authentication
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Starts the session and sends the cookie settings that matter.
 */
function admin_session_start(): void
{
    if (session_status() === PHP_SESSION_ACTIVE) {
        return;
    }

    session_set_cookie_params([
        'httponly' => true,

        // The admin writes the dictionary and the password crosses the wire, so the cookie is marked
        // secure whenever the request itself arrived over TLS. Serving this over plain HTTP is not a
        // supported arrangement.
        'secure' => !empty($_SERVER['HTTPS']),
        'samesite' => 'Lax',
    ]);

    session_start();
}

/**
 * Determines whether the current session is signed in.
 */
function admin_is_signed_in(): bool
{
    return ($_SESSION['lexicon_admin'] ?? false) === true;
}

/**
 * Checks a password against the stored hash.
 *
 * The configuration holds a hash, never the password itself, so a leaked .env.php does not hand over
 * a working login. Generate it with:
 *
 *   php -r "echo password_hash('heslo', PASSWORD_DEFAULT), PHP_EOL;"
 */
function admin_sign_in(string $password): bool
{
    $hash = lexicon_config('LEXICON_ADMIN_PASSWORD_HASH');

    if ($hash === '') {
        admin_fail('Chybí LEXICON_ADMIN_PASSWORD_HASH. Bez něj se do administrace nedá přihlásit.');
    }

    if (!password_verify($password, $hash)) {
        return false;
    }

    // A new identifier for the new privilege level, so a session id captured before the login cannot
    // be reused after it.
    session_regenerate_id(true);
    $_SESSION['lexicon_admin'] = true;

    return true;
}

/**
 * Ends the session.
 */
function admin_sign_out(): void
{
    $_SESSION = [];
    session_destroy();
}

// ─────────────────────────────────────────────────────────────────────────────
// Cross-site request forgery
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Gets the token for this session, creating it on first use.
 */
function admin_csrf_token(): string
{
    if (empty($_SESSION['csrf'])) {
        $_SESSION['csrf'] = bin2hex(random_bytes(32));
    }

    return $_SESSION['csrf'];
}

/**
 * Refuses a POST that did not come from a form this session rendered.
 */
function admin_check_csrf(): void
{
    if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
        return;
    }

    if (!hash_equals(admin_csrf_token(), (string) ($_POST['csrf'] ?? ''))) {
        http_response_code(400);
        exit('Neplatný formulářový token. Načti stránku znovu.');
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Input
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Reads a posted text field, turning an empty one into null.
 *
 * Empty and absent are the same thing in a form — a cleared text input posts "" — and the columns
 * distinguish null from the empty string. Storing "" would make a lemma with no pattern compare
 * unequal to one that never had a pattern.
 */
function admin_text(string $name): ?string
{
    $value = trim((string) ($_POST[$name] ?? ''));

    return $value === '' ? null : $value;
}

/**
 * Reads a posted integer field.
 */
function admin_int(string $name, ?int $default = null): ?int
{
    $value = admin_text($name);

    return $value === null ? $default : (int) $value;
}

/**
 * Reads a three-state flag: yes, no, or not recorded.
 *
 * Most of the morphological flags are nullable on purpose. "This noun does not have a mobile e" and
 * "nobody has checked" are different claims, and the resolvers treat them differently.
 */
function admin_flag(string $name): ?int
{
    $value = (string) ($_POST[$name] ?? '');

    return match ($value) {
        '1' => 1,
        '0' => 0,
        default => null,
    };
}

/**
 * Reads a posted value that has to be one of the permitted ones.
 *
 * Anything unrecognised becomes null rather than being written through, so a tampered form cannot put
 * a value into a column the C# side then fails to parse.
 */
function admin_enum(string $name, string $column): ?string
{
    $value = admin_text($name);

    return $value !== null && array_key_exists($value, LEXICON_ENUMS[$column]) ? $value : null;
}

/**
 * Reads the posted vzor and checks it against the patterns the category has.
 *
 * Unlike admin_enum, an unknown value is not quietly dropped to null. A vzor is the one field the
 * inflection services cannot work around: a null means "nothing declines this word", which is a
 * legitimate state, while a typo silently turned into null would look like a deliberate choice and
 * leave a word that never inflects with nobody knowing why. So this reports instead, and the caller
 * refuses the save.
 *
 * Case is folded because all three inflection services look the pattern up through Pattern.ToLower().
 * The value is stored as typed — the C# side folds again on the way in.
 *
 * @param-out string|null $error
 */
function admin_pattern(string $name, string $category, ?string &$error): ?string
{
    $error = null;
    $value = admin_text($name);

    if ($value === null) {
        return null;
    }

    $label = LEXICON_ENUMS['category'][$category] ?? $category;

    if (!array_key_exists($category, LEXICON_PATTERNS)) {
        $error = 'Vzor „' . $value . '“ nelze uložit: ' . $label
            . ' se podle vzoru neskloňuje. Nech pole prázdné.';

        return null;
    }

    $wanted = mb_strtolower($value, 'UTF-8');

    foreach (LEXICON_PATTERNS[$category] as $pattern) {
        if ($wanted === mb_strtolower($pattern, 'UTF-8')) {
            return $value;
        }
    }

    $error = 'Vzor „' . $value . '“ neexistuje. Slovní druh ' . $label . ' má tyhle: '
        . implode(', ', LEXICON_PATTERNS[$category]) . '.';

    return null;
}

/**
 * Computes the lookup key from a lemma.
 *
 * mb_strtolower and not strtolower: the plain one works byte by byte and leaves Á alone, which would
 * produce a key no lookup ever matches — the entry would save, and then simply never be found. The C#
 * validator checks this independently, folding with ToLowerInvariant.
 */
function admin_lemma_key(string $lemma): string
{
    return mb_strtolower(trim($lemma), 'UTF-8');
}

// ─────────────────────────────────────────────────────────────────────────────
// Output
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Escapes text for HTML.
 */
function h(?string $value): string
{
    return htmlspecialchars((string) $value, ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
}

/**
 * Builds a URL inside the admin.
 *
 * @param array<string, string|int|null> $parameters
 */
function admin_url(array $parameters): string
{
    return '?' . http_build_query(array_filter($parameters, fn ($value): bool => $value !== null));
}

/**
 * Remembers a message to show after a redirect.
 */
function admin_flash(string $message, string $kind = 'ok'): void
{
    $_SESSION['flash'][] = ['message' => $message, 'kind' => $kind];
}

/**
 * Takes the pending messages and forgets them.
 *
 * @return list<array{message: string, kind: string}>
 */
function admin_take_flashes(): array
{
    $flashes = $_SESSION['flash'] ?? [];
    unset($_SESSION['flash']);

    return $flashes;
}

/**
 * Redirects, ending the request.
 *
 * @param array<string, string|int|null> $parameters
 */
function admin_redirect(array $parameters): never
{
    // The front controller renders pages into an output buffer, and a page that redirects does so from
    // inside it. Discarding what has been buffered keeps the half-built page from being flushed out
    // after the Location header on the way to exit.
    while (ob_get_level() > 0) {
        ob_end_clean();
    }

    header('Location: ' . admin_url($parameters));
    exit(0);
}

/**
 * Stops with a configuration error.
 */
function admin_fail(string $message): never
{
    http_response_code(500);
    echo '<!doctype html><meta charset="utf-8"><p style="font:16px system-ui;padding:2rem">'
        . h($message) . '</p>';
    exit(1);
}

/**
 * Renders a select over the permitted values of a column.
 *
 * The id defaults to the name and can be given separately, for the pages that render the same field
 * once per row. Two elements sharing an id is not a cosmetic problem there: a label points at the
 * first one, so clicking the second row's label focuses the first row's select.
 */
function admin_select(
    string $name,
    string $column,
    ?string $selected,
    bool $allowEmpty = true,
    ?string $id = null
): string {
    $html = '<select name="' . h($name) . '" id="' . h($id ?? $name) . '">';

    if ($allowEmpty) {
        $html .= '<option value="">— neuvedeno —</option>';
    }

    foreach (LEXICON_ENUMS[$column] as $value => $label) {
        $isSelected = $value === $selected ? ' selected' : '';
        $html .= '<option value="' . h($value) . '"' . $isSelected . '>' . h($label) . '</option>';
    }

    return $html . '</select>';
}

/**
 * Counts how many of the named columns the row actually says something in.
 *
 * Drives which sections of the entry form open by themselves. A collapsed section that turns out to
 * hold data is worse than no collapsing at all — the value is invisible and looks absent — so a
 * section opens whenever it has something to show.
 *
 * A flag set to "no" counts: 0 is a claim, and only null is the gap.
 */
function admin_filled_count(?array $row, array $columns): int
{
    if ($row === null) {
        return 0;
    }

    $filled = 0;

    foreach ($columns as $column) {
        $value = $row[$column] ?? null;

        if ($value !== null && $value !== '') {
            $filled++;
        }
    }

    return $filled;
}

/**
 * Opens a foldable section of a form.
 *
 * A <details> rather than anything scripted, because the fields inside a collapsed one are still in
 * the form and still post. A section hidden with display:none would do the same, but one built out of
 * tabs or removed from the DOM would silently drop half the entry on save.
 */
function admin_fold_open(string $title, int $filled, int $total): string
{
    // Vyplněná sekce se otevře sama. Nevyplněná zůstane složená a řekne to číslem, aby nešlo splést
    // „nic tam není“ s „nekoukal jsem dovnitř“.
    $badge = $filled === 0
        ? '<span class="muted">prázdné</span>'
        : '<span class="badge">' . $filled . ' z ' . $total . '</span>';

    return '<details class="fold"' . ($filled > 0 ? ' open' : '') . '><summary>' . h($title) . $badge
        . '</summary>';
}

/**
 * Closes a foldable section.
 */
function admin_fold_close(): string
{
    return '</details>';
}

/**
 * Renders a three-state flag as a group of radio buttons.
 */
function admin_flag_field(string $name, ?int $value): string
{
    $options = [null => 'neuvedeno', 1 => 'ano', 0 => 'ne'];
    $html = '<span class="flag">';

    foreach ($options as $option => $label) {
        $checked = $option === $value ? ' checked' : '';
        $id = $name . '_' . ($option === null ? 'x' : $option);

        $html .= '<label for="' . h($id) . '"><input type="radio" id="' . h($id) . '" name="' . h($name)
            . '" value="' . ($option === null ? '' : $option) . '"' . $checked . '> ' . h($label) . '</label>';
    }

    return $html . '</span>';
}

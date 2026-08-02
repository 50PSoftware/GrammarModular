<?php

declare(strict_types=1);

/**
 * Reads configuration from the real environment, falling back to a file beside this one.
 *
 * A file is needed because getenv() under PHP-FPM sees only what the pool passes with env[NAME],
 * which is a setting people reasonably expect the shell to cover and it does not — and on shared
 * hosting the pool is not yours to configure at all.
 *
 * Two file formats, in this order:
 *
 *   .env.php   Returns an array. PREFER THIS wherever the file has to sit inside the document root,
 *              which on shared hosting is everywhere: requested over HTTP it is executed, prints
 *              nothing, and leaks nothing. That holds with no .htaccess, with AllowOverride off, and
 *              on nginx, because it does not depend on the server being configured to refuse it.
 *
 *   .env       Plain KEY=value. Only safe above the document root. A .env inside it is served as
 *              plain text by every server that has not been told otherwise, since nothing maps the
 *              extension to PHP — https://example.com/.env then hands over the database password and
 *              the API token with no error and no trace beyond an access log line.
 *
 * lexicon_refuse_if_web_accessible() below exists for the second case and refuses to start when it
 * can tell the .env is inside the document root. A deployment that has deliberately put it there and
 * protected it another way switches that call off; switching to .env.php makes the question moot.
 *
 * The real environment wins over both files, so one value can still be overridden with env[NAME]
 * without editing anything.
 */

/**
 * Gets a configuration value.
 *
 * @param string $name The variable name.
 * @return string The value, or an empty string when it is set nowhere.
 */
function lexicon_config(string $name): string
{
    $value = getenv($name);

    // An empty value is treated as absent on purpose. A variable set to nothing is a half-finished
    // deployment, and the callers all fail closed on an empty string.
    if ($value !== false && $value !== '') {
        return $value;
    }

    return lexicon_env_file()[$name] ?? '';
}

/**
 * Parses the .env once and remembers it.
 *
 * @return array<string, string>
 */
function lexicon_env_file(): array
{
    static $parsed = null;

    if ($parsed !== null) {
        return $parsed;
    }

    // Checked first, and the one to use when the file cannot be kept out of the document root: a PHP
    // file answers an HTTP request by running, not by handing over its contents.
    $phpPath = __DIR__ . DIRECTORY_SEPARATOR . '.env.php';

    if (is_file($phpPath)) {
        $values = require $phpPath;

        if (!is_array($values)) {
            error_log("lexicon: $phpPath nevrací pole.");

            return $parsed = [];
        }

        $parsed = [];

        foreach ($values as $key => $value) {
            $parsed[(string) $key] = (string) $value;
        }

        return $parsed;
    }

    $path = __DIR__ . DIRECTORY_SEPARATOR . '.env';

    if (!is_file($path)) {
        // Not an error. The variables may well come from the FPM pool, which is the better arrangement
        // where you control it.
        return $parsed = [];
    }

    //lexicon_refuse_if_web_accessible($path);

    $parsed = [];

    foreach (file($path, FILE_IGNORE_NEW_LINES) ?: [] as $line) {
        $line = trim($line);

        if ($line === '' || str_starts_with($line, '#')) {
            continue;
        }

        $separator = strpos($line, '=');

        if ($separator === false) {
            continue;
        }

        $key = trim(substr($line, 0, $separator));
        $value = trim(substr($line, $separator + 1));

        // Surrounding quotes are stripped, and nothing inside them is interpolated. A token is an
        // opaque string and $ or \ in one must survive verbatim.
        if (strlen($value) >= 2
            && ($value[0] === '"' || $value[0] === "'")
            && $value[strlen($value) - 1] === $value[0]) {
            $value = substr($value, 1, -1);
        }

        $parsed[$key] = $value;
    }

    return $parsed;
}

/**
 * Refuses to continue when the .env sits somewhere the web server would serve it.
 *
 * Fails closed, and loudly, because the alternative failure is silent: the file works perfectly while
 * also being downloadable. Moving it one directory up is the whole fix.
 */
function lexicon_refuse_if_web_accessible(string $path): void
{
    $documentRoot = $_SERVER['DOCUMENT_ROOT'] ?? '';

    // Empty under the CLI, and unreliable enough elsewhere that an unresolvable path is treated as
    // "cannot tell" rather than as "unsafe" — refusing to start on a guess would be its own outage.
    if ($documentRoot === '') {
        return;
    }

    $realRoot = realpath($documentRoot);
    $realPath = realpath($path);

    if ($realRoot === false || $realPath === false) {
        return;
    }

    $realRoot = rtrim($realRoot, DIRECTORY_SEPARATOR) . DIRECTORY_SEPARATOR;

    if (str_starts_with($realPath, $realRoot)) {
        error_log(
            "lexicon: .env leží v document rootu ($realPath). Web server ho vydá jako text — "
            . 'přesuň ho o adresář výš a nasměruj vhost na Php/api/.'
        );

        http_response_code(500);
        echo json_encode(['error' => 'Server není nastavený.'], JSON_UNESCAPED_UNICODE);
        exit(1);
    }
}

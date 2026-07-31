<?php

declare(strict_types=1);

/**
 * Reads configuration from the real environment, falling back to a .env file beside this one.
 *
 * The .env exists because getenv() under PHP-FPM sees only what the pool passes with env[NAME], which
 * is a setting people reasonably expect the shell to cover and it does not. A file removes that whole
 * class of confusion.
 *
 * It also introduces a worse hazard than the one it removes, and the guard below is the point of this
 * file as much as the parsing is. A .env inside the document root is served as plain text by every web
 * server that has not been told otherwise, because nothing maps the extension to PHP — so
 * https://example.com/.env hands the database password and the API token to anyone who asks, with no
 * error anywhere and no trace beyond an access log line. The file therefore belongs one level above
 * whatever the vhost serves, and this refuses to run if it is not.
 *
 * Layout this assumes:
 *
 *   Php/
 *     .env              <- secrets, never committed
 *     .env.example      <- the template, committed
 *     .htaccess         <- denies .env, in case the vhost is pointed here anyway
 *     env.php
 *     schema-tables.php
 *     api/              <- point the vhost HERE, not at Php/
 *       lexicon.php
 *
 * The real environment wins over the file, so a deployment can override one value with env[NAME]
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

    $path = __DIR__ . DIRECTORY_SEPARATOR . '.env';

    if (!is_file($path)) {
        // Not an error. The variables may well come from the FPM pool, which is the better arrangement
        // where you control it.
        return $parsed = [];
    }

    lexicon_refuse_if_web_accessible($path);

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

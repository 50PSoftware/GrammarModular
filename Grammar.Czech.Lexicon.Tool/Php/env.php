<?php

declare(strict_types=1);

/**
 * Reads configuration from the real environment, falling back to .env.php beside this one.
 *
 * A file is needed because getenv() under PHP-FPM sees only what the pool passes with env[NAME],
 * which is a setting people reasonably expect the shell to cover and it does not — and on shared
 * hosting the pool is not yours to configure at all.
 *
 * The file is PHP returning an array, not KEY=value, and that is the whole security design rather
 * than a preference. The admin serves from the document root, so the configuration has nowhere to
 * live except inside it, and a plain .env there is handed out as text by any server that has not been
 * told otherwise — https://example.com/.env giving up the database password with nothing logged but
 * an access line. A PHP file answers the same request by running, and prints nothing. That holds with
 * no .htaccess, with AllowOverride off, and on nginx, because it does not depend on the server being
 * configured to refuse anything.
 *
 * The real environment wins over the file, so one value can still be overridden with env[NAME]
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
 * Loads .env.php once and remembers it.
 *
 * @return array<string, string>
 */
function lexicon_env_file(): array
{
    static $parsed = null;

    if ($parsed !== null) {
        return $parsed;
    }

    $path = __DIR__ . DIRECTORY_SEPARATOR . '.env.php';

    if (!is_file($path)) {
        // Not an error. The variables may well come from the FPM pool, which is the better arrangement
        // where you control it.
        return $parsed = [];
    }

    $values = require $path;

    if (!is_array($values)) {
        error_log("lexicon: $path nevrací pole.");

        return $parsed = [];
    }

    $parsed = [];

    foreach ($values as $key => $value) {
        $parsed[(string) $key] = (string) $value;
    }

    return $parsed;
}

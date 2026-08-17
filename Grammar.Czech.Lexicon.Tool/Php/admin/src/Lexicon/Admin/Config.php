<?php

declare(strict_types=1);

namespace Lexicon\Admin;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Reads deployment configuration.
 *
 * A thin object over lexicon_config() in ../env.php rather than a replacement for it. That function is
 * shared with the API, which knows nothing about this namespace and must keep working with no classes
 * loaded at all, so the reading of .env.php stays where both halves can reach it.
 */
final class Config
{
    /**
     * Gets a configuration value, or an empty string when it is set nowhere.
     */
    public function get(string $name): string
    {
        return lexicon_config($name);
    }

    /**
     * Gets a configuration value, refusing to continue without it.
     *
     * @throws ConfigurationError When the value is missing or empty.
     */
    public function require(string $name, string $explanation): string
    {
        $value = $this->get($name);

        if ($value === '') {
            throw new ConfigurationError($explanation);
        }

        return $value;
    }
}

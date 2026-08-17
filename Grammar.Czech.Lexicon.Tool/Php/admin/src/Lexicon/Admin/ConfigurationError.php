<?php

declare(strict_types=1);

namespace Lexicon\Admin;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use RuntimeException;

/**
 * A deployment that cannot answer requests: a missing DSN, a missing password hash.
 *
 * Separate from every other failure because its message is safe to show. It names what is not set and
 * nothing about the server, whereas the generic error page deliberately says nothing at all.
 */
final class ConfigurationError extends RuntimeException
{
}

<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use RuntimeException;

/**
 * A request that cannot be answered normally: no such route, a bad form token.
 *
 * Carries the status the kernel should send along with a message that is safe to show — it says what
 * the caller did, never anything about the server.
 */
final class HttpException extends RuntimeException
{
    public function __construct(private readonly int $status, string $message)
    {
        parent::__construct($message);
    }

    public function status(): int
    {
        return $this->status;
    }

    /**
     * No route matched.
     */
    public static function notFound(): self
    {
        return new self(404, 'Taková stránka tu není.');
    }

    /**
     * The form token was missing or did not match the session.
     */
    public static function badToken(): self
    {
        return new self(400, 'Neplatný formulářový token. Načti stránku znovu.');
    }
}

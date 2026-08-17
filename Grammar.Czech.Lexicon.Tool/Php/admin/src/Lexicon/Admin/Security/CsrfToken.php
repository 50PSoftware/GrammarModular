<?php

declare(strict_types=1);

namespace Lexicon\Admin\Security;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * The token that says a form came from a page this session rendered.
 */
final class CsrfToken
{
    private const KEY = 'csrf';

    public function __construct(private readonly Session $session)
    {
    }

    /**
     * Gets the token for this session, creating it on first use.
     */
    public function value(): string
    {
        $token = $this->session->get(self::KEY);

        if (!is_string($token) || $token === '') {
            $token = bin2hex(random_bytes(32));
            $this->session->set(self::KEY, $token);
        }

        return $token;
    }

    /**
     * Determines whether a posted token is the one this session holds.
     */
    public function matches(?string $presented): bool
    {
        return hash_equals($this->value(), (string) $presented);
    }
}

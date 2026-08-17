<?php

declare(strict_types=1);

namespace Lexicon\Admin\Security;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Config;

/**
 * The single sign-in the admin has.
 *
 * There is no user table and no roles. One person maintains this dictionary, and a login exists so
 * that the form is not on the open internet, not to tell two editors apart.
 */
final class Authenticator
{
    private const KEY = 'lexicon_admin';

    public function __construct(
        private readonly Session $session,
        private readonly Config $config
    ) {
    }

    /**
     * Determines whether the current session is signed in.
     */
    public function isSignedIn(): bool
    {
        return $this->session->get(self::KEY) === true;
    }

    /**
     * Checks a password against the stored hash and signs in if it matches.
     *
     * The configuration holds a hash, never the password itself, so a leaked .env.php does not hand
     * over a working login. Generate it with:
     *
     *   php -r "echo password_hash('heslo', PASSWORD_DEFAULT), PHP_EOL;"
     */
    public function signIn(string $password): bool
    {
        $hash = $this->config->require(
            'LEXICON_ADMIN_PASSWORD_HASH',
            'Chybí LEXICON_ADMIN_PASSWORD_HASH. Bez něj se do administrace nedá přihlásit.'
        );

        if (!password_verify($password, $hash)) {
            return false;
        }

        // A new identifier for the new privilege level, so a session id captured before the login
        // cannot be reused after it.
        $this->session->regenerate();
        $this->session->set(self::KEY, true);

        return true;
    }

    /**
     * Ends the session.
     */
    public function signOut(): void
    {
        $this->session->destroy();
    }
}

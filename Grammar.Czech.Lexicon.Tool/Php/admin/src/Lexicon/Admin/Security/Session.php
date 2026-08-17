<?php

declare(strict_types=1);

namespace Lexicon\Admin\Security;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * The session, and the only place $_SESSION is touched.
 *
 * Everything that outlives a request — the sign-in, the form token, pending messages, the values of a
 * form that was refused — goes through here, so there is one list of what is kept and one place the
 * cookie settings are decided.
 */
final class Session
{
    private bool $started = false;

    public function __construct(private readonly bool $isSecure)
    {
    }

    /**
     * Starts the session and sends the cookie settings that matter.
     */
    public function start(): void
    {
        if ($this->started || session_status() === PHP_SESSION_ACTIVE) {
            $this->started = true;

            return;
        }

        session_set_cookie_params([
            'httponly' => true,

            // The admin writes the dictionary and the password crosses the wire, so the cookie is
            // marked secure whenever the request itself arrived over TLS. Serving this over plain HTTP
            // is not a supported arrangement.
            'secure' => $this->isSecure,
            'samesite' => 'Lax',
        ]);

        session_start();
        $this->started = true;
    }

    /**
     * Reads a value.
     */
    public function get(string $key, mixed $default = null): mixed
    {
        return $_SESSION[$key] ?? $default;
    }

    /**
     * Writes a value.
     */
    public function set(string $key, mixed $value): void
    {
        $_SESSION[$key] = $value;
    }

    /**
     * Reads a value and forgets it.
     */
    public function pull(string $key, mixed $default = null): mixed
    {
        $value = $_SESSION[$key] ?? $default;
        unset($_SESSION[$key]);

        return $value;
    }

    /**
     * Appends to a list kept in the session.
     */
    public function push(string $key, mixed $value): void
    {
        $_SESSION[$key][] = $value;
    }

    /**
     * Gives the session a new identifier, keeping what is in it.
     */
    public function regenerate(): void
    {
        session_regenerate_id(true);
    }

    /**
     * Empties and ends the session.
     */
    public function destroy(): void
    {
        $_SESSION = [];
        session_destroy();
        $this->started = false;
    }
}

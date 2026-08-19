<?php

declare(strict_types=1);

namespace Lexicon\Admin\Repository;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Database\Database;

/**
 * Personal `lexikon.ps1 pull` tokens, one row per token, in the lexicon's own database.
 *
 * The token itself is never stored — only its sha256, the same reasoning
 * Authenticator applies to the sign-in password: a leaked table hands over nothing usable.
 */
final class ApiTokenRepository
{
    public function __construct(private readonly Database $database)
    {
    }

    /**
     * Every token belonging to one website user, newest first.
     *
     * @return list<array{id: int, label: ?string, created_at: string, last_used_at: ?string}>
     */
    public function forUser(int $webUserId): array
    {
        return $this->database->all(
            'SELECT id, label, created_at, last_used_at FROM api_token '
                . 'WHERE web_user_id = ? ORDER BY created_at DESC',
            [$webUserId]
        );
    }

    /**
     * Generates a new personal token, stores its hash, and returns the token itself — the only moment
     * it exists outside the caller's own memory.
     */
    public function create(int $webUserId, ?string $label): string
    {
        $token = bin2hex(random_bytes(32));

        $this->database->insert(
            'INSERT INTO api_token (token_hash, web_user_id, label) VALUES (?, ?, ?)',
            [hash('sha256', $token), $webUserId, $label]
        );

        return $token;
    }

    /**
     * Revokes a token, but only if it belongs to this user — a stolen id in a form field cannot revoke
     * somebody else's token.
     */
    public function revoke(int $id, int $webUserId): void
    {
        $this->database->run(
            'DELETE FROM api_token WHERE id = ? AND web_user_id = ?',
            [$id, $webUserId]
        );
    }
}

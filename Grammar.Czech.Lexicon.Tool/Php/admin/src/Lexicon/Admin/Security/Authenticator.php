<?php

declare(strict_types=1);

namespace Lexicon\Admin\Security;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Config;
use Lexicon\Admin\Database\WebUserDatabase;

/**
 * Sign-in against the official website's own accounts.
 *
 * There is no user table here and no password hash in this app's own configuration: the website is the
 * only place an account, a password and its verification live, and this class only asks it a question —
 * "does this email/password pair belong to a verified account holding the required role?" — never
 * stores an answer beyond the session.
 */
final class Authenticator
{
    private const KEY = 'lexicon_user';

    public function __construct(
        private readonly Session $session,
        private readonly Config $config,
        private readonly WebUserDatabase $webUsers
    ) {
    }

    /**
     * Determines whether the current session belongs to a signed-in account.
     */
    public function isSignedIn(): bool
    {
        return $this->session->get(self::KEY) !== null;
    }

    /**
     * The signed-in account, or null when there is none.
     *
     * @return array{id: int, mail: string, name: ?string, roles: list<string>}|null
     */
    public function currentUser(): ?array
    {
        return $this->session->get(self::KEY);
    }

    /**
     * Whether the signed-in account carries the given role.
     *
     * Checked against the roles captured in the session at sign-in time, not against the website's
     * database again — a role revoked mid-session takes effect on the next sign-in, same as the
     * required role already does.
     */
    public function hasRole(string $role): bool
    {
        $user = $this->currentUser();

        return $user !== null && in_array($role, $user['roles'], true);
    }

    /**
     * Checks an email and password against the website's account, requires it to be verified and to
     * carry the role this admin gates on, and signs in on success.
     *
     * Every way this can fail returns the same false — wrong email, wrong password, unverified account,
     * missing role — because there is nothing to tell a stranger about which of those it was.
     */
    public function signIn(string $mail, string $password): bool
    {
        $requiredRole = $this->config->require(
            'LEXICON_ADMIN_REQUIRED_ROLE',
            'Chybí LEXICON_ADMIN_REQUIRED_ROLE. Bez něj nemá přihlášení, komu věřit.'
        );

        $account = $this->webUsers->findByEmail($mail);

        if ($account === null || $account['verified'] !== 1) {
            return false;
        }

        if (!password_verify($password, $account['password'])) {
            return false;
        }

        $roles = $this->roles($account['roles']);

        if (!in_array($requiredRole, $roles, true)) {
            return false;
        }

        // A new identifier for the new privilege level, so a session id captured before the login
        // cannot be reused after it.
        $this->session->regenerate();
        $this->session->set(self::KEY, [
            'id' => $account['id'],
            'mail' => $account['mail'],
            'name' => $account['name'],
            'roles' => $roles,
        ]);

        return true;
    }

    /**
     * Ends the session.
     */
    public function signOut(): void
    {
        $this->session->destroy();
    }

    /**
     * Decodes the `roles` JSON array on the account.
     *
     * Malformed JSON is treated as no roles at all rather than as an error — a broken column on the
     * website's side should refuse the login it can't make sense of, not turn into a 500 here.
     *
     * @return list<string>
     */
    private function roles(string $rolesJson): array
    {
        $roles = json_decode($rolesJson, true);

        return is_array($roles) ? array_values(array_filter($roles, 'is_string')) : [];
    }
}

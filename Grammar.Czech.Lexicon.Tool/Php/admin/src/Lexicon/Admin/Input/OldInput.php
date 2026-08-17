<?php

declare(strict_types=1);

namespace Lexicon\Admin\Input;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Security\Session;

/**
 * What a refused form held, kept across the redirect that follows.
 *
 * A save that is turned down — a vzor that does not exist, a lemma already taken by a homonym — used
 * to send the editor back to a form filled from the database, which is to say back to what they had
 * before they started typing. On a form with thirty fields that is the whole entry lost to one typo.
 *
 * Kept beside the flash and taken the same way: reading it clears it, so a later visit to the same
 * page shows what is actually stored.
 */
final class OldInput
{
    private const KEY = 'old_input';

    /** @var array<string, mixed>|null */
    private ?array $taken = null;

    public function __construct(private readonly Session $session)
    {
    }

    /**
     * Remembers the posted fields.
     *
     * @param array<string, mixed> $fields
     */
    public function keep(array $fields): void
    {
        // The token is per session and the view emits a fresh one; keeping the posted copy would only
        // put a stale value back into the form.
        unset($fields['csrf']);

        $this->session->set(self::KEY, $fields);
    }

    /**
     * Determines whether the form being rendered is a refused one coming back.
     */
    public function exists(): bool
    {
        return $this->fields() !== [];
    }

    /**
     * The value a field had, or the fallback when this is not a refused form.
     */
    public function value(string $name, mixed $fallback = null): mixed
    {
        $fields = $this->fields();

        if (!array_key_exists($name, $fields)) {
            return $this->exists() ? null : $fallback;
        }

        return $fields[$name];
    }

    /**
     * Reads the kept fields once and forgets them.
     *
     * @return array<string, mixed>
     */
    private function fields(): array
    {
        if ($this->taken === null) {
            $kept = $this->session->pull(self::KEY, []);
            $this->taken = is_array($kept) ? $kept : [];
        }

        return $this->taken;
    }
}

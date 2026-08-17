<?php

declare(strict_types=1);

namespace Lexicon\Admin\View;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Security\Session;

/**
 * Messages that survive the redirect after a write.
 */
final class Flash
{
    private const KEY = 'flash';

    public function __construct(private readonly Session $session)
    {
    }

    /**
     * Remembers a message to show after the redirect.
     */
    public function ok(string $message): void
    {
        $this->add($message, 'ok');
    }

    /**
     * Remembers a refusal to show after the redirect.
     */
    public function error(string $message): void
    {
        $this->add($message, 'err');
    }

    /**
     * Takes the pending messages and forgets them.
     *
     * @return list<array{message: string, kind: string}>
     */
    public function take(): array
    {
        $flashes = $this->session->pull(self::KEY, []);

        return is_array($flashes) ? $flashes : [];
    }

    private function add(string $message, string $kind): void
    {
        $this->session->push(self::KEY, ['message' => $message, 'kind' => $kind]);
    }
}

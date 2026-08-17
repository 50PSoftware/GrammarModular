<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * The route a request landed on, with the identifiers taken out of its path.
 */
final class RouteMatch
{
    /**
     * @param array<string, int> $parameters
     */
    public function __construct(
        public readonly Route $route,
        public readonly array $parameters
    ) {
    }

    /**
     * An identifier from the path.
     */
    public function id(string $name): int
    {
        return $this->parameters[$name] ?? 0;
    }
}

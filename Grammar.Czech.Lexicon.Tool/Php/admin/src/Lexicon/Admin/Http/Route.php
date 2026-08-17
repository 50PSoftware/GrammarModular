<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * One path the admin answers on, and what answers it.
 *
 * The pattern is a literal path with {name} standing for one path segment. A placeholder only ever
 * matches digits, because every identifier in this schema is one — which means a route like
 * /heslo/nove can sit beside /heslo/{id} without the two ever being confused.
 */
final class Route
{
    /**
     * @param class-string $controller
     */
    public function __construct(
        public readonly string $method,
        public readonly string $pattern,
        public readonly string $controller,
        public readonly string $action,
        public readonly bool $isPublic = false
    ) {
    }

    /**
     * Matches a path, returning the identifiers it carried, or null when it is a different path.
     *
     * @return array<string, int>|null
     */
    public function match(string $path): ?array
    {
        // preg_quote escapes the braces, so the placeholder is matched as \{name\} on the way to
        // becoming a named group. Only digits, because every identifier in this schema is one.
        $quoted = preg_quote($this->pattern, '#');
        $regex = '#^' . (string) preg_replace('/\\\{([a-zA-Z]+)\\\}/', '(?<$1>[0-9]+)', $quoted) . '$#';

        if (preg_match($regex, $path, $found) !== 1) {
            return null;
        }

        $parameters = [];

        foreach ($found as $name => $value) {
            if (is_string($name)) {
                $parameters[$name] = (int) $value;
            }
        }

        return $parameters;
    }
}

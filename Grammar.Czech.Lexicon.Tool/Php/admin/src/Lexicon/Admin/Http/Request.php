<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * One incoming request, read once from the superglobals and passed down from there.
 *
 * Nothing below the kernel touches $_GET, $_POST or $_SERVER. That is what makes a controller
 * readable on its own: the values it works with arrive as arguments instead of being reached for.
 */
final class Request
{
    /**
     * @param array<string, mixed> $query
     * @param array<string, mixed> $form
     */
    public function __construct(
        public readonly string $method,
        public readonly string $path,
        public readonly string $basePath,
        public readonly array $query,
        public readonly array $form,
        public readonly bool $isSecure
    ) {
    }

    /**
     * Builds the request from the environment PHP was handed.
     *
     * The base path is normally worked out from the environment. It can be given instead, for the
     * deployments where the environment does not say — see readBasePath().
     */
    public static function fromGlobals(?string $configuredBasePath = null): self
    {
        $basePath = self::readBasePath($configuredBasePath);

        return new self(
            strtoupper((string) ($_SERVER['REQUEST_METHOD'] ?? 'GET')),
            self::readPath($basePath),
            $basePath,
            $_GET,
            $_POST,
            !empty($_SERVER['HTTPS'])
        );
    }

    /**
     * Determines whether this request writes.
     */
    public function isPost(): bool
    {
        return $this->method === 'POST';
    }

    /**
     * Reads a query parameter as text, or null when it is absent or empty.
     */
    public function queryText(string $name): ?string
    {
        $value = trim((string) ($this->query[$name] ?? ''));

        return $value === '' ? null : $value;
    }

    /**
     * Reads a query parameter as a whole number, never below one.
     */
    public function queryPage(string $name): int
    {
        return max(1, (int) ($this->query[$name] ?? 1));
    }

    /**
     * The prefix every address of the admin sits under, without a trailing slash.
     *
     * Empty at the document root. In a subdirectory it is what stands in front of every page —
     * /czlex for https://example.cz/czlex/prihlaseni — and every link, form action and the stylesheet
     * href is built on top of it.
     *
     * dirname(SCRIPT_NAME) proposes it, and the request itself confirms or refuses the proposal.
     * SCRIPT_NAME is not always the address the browser used: hosting that maps a subdomain onto a
     * directory reports the path from the account root, so a site served at czlex.example.net/ can be
     * announced to PHP as /subdom/czlex/index.php. Believing that puts /subdom/czlex in front of every
     * address, and the first casualty is the stylesheet — it is the one address nothing redirects, so
     * it fails silently instead of landing somewhere that still works.
     *
     * A real prefix is one the current request is actually under. Checking that costs nothing and
     * catches exactly the case detection cannot otherwise see, so a prefix the request does not
     * confirm is dropped rather than trusted.
     */
    private static function readBasePath(?string $configured): string
    {
        if ($configured !== null && trim($configured) !== '') {
            return self::normalizeBasePath($configured);
        }

        $proposed = self::normalizeBasePath(dirname((string) ($_SERVER['SCRIPT_NAME'] ?? '/index.php')));

        return self::isUnder(self::requestPath(), $proposed) ? $proposed : '';
    }

    /**
     * The path of the current request, without the query string.
     */
    private static function requestPath(): string
    {
        return (string) parse_url((string) ($_SERVER['REQUEST_URI'] ?? '/'), PHP_URL_PATH);
    }

    /**
     * Determines whether a path sits under a prefix.
     *
     * On the segment boundary, not on the characters: /czlexicon starts with /czlex and is a different
     * place. Stripping by length alone would leave "icon" as the page being asked for.
     */
    private static function isUnder(string $path, string $prefix): bool
    {
        return $prefix === '' || $path === $prefix || str_starts_with($path, $prefix . '/');
    }

    /**
     * Puts a base path into the one shape the rest of the code expects: a leading slash, no trailing
     * one, and the empty string for the document root.
     *
     * That shape is also what makes '/' worth writing in the configuration: it normalises to the empty
     * string, which says "the root, and stop guessing" — an empty setting only says "guess".
     */
    private static function normalizeBasePath(string $path): string
    {
        $path = '/' . trim(trim($path), '/');

        return $path === '/' ? '' : $path;
    }

    /**
     * The path inside the admin, always starting with a slash.
     *
     * Normally this comes from the rewritten REQUEST_URI. The _path parameter is a fallback for a
     * deployment where mod_rewrite is off: index.php?_path=/heslo/42 reaches the same route. It is read
     * second so a working rewrite always wins, and it cannot be used to reach anything the router does
     * not already publish.
     */
    private static function readPath(string $basePath): string
    {
        $path = self::requestPath();

        if ($basePath !== '' && self::isUnder($path, $basePath)) {
            $path = substr($path, strlen($basePath));
        }

        // A rewrite that never fired leaves the script itself in the path.
        if ($path === '' || $path === '/index.php') {
            $path = trim((string) ($_GET['_path'] ?? '')) ?: '/';
        }

        return '/' . ltrim(rawurldecode($path), '/');
    }
}

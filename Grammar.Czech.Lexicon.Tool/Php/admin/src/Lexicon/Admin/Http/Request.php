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
     * Normally dirname(SCRIPT_NAME) answers this, because SCRIPT_NAME is the front controller's own
     * address as the server sees it. It does not answer it everywhere: hosting that maps a subdomain
     * onto a subdirectory, or that rewrites at a level above this one, can hand PHP a SCRIPT_NAME of
     * /index.php while the browser sits on /czlex/prihlaseni. Detection then reports the root, every
     * address comes out a directory too high, and the first thing to break is the stylesheet — it is
     * the one address nothing redirects, so it fails silently instead of landing somewhere useful.
     *
     * LEXICON_ADMIN_BASE_PATH settles it where detection cannot. Given, it wins outright: falling
     * back to a guess when an explicit answer exists would only bring the guessing back.
     */
    private static function readBasePath(?string $configured): string
    {
        if ($configured !== null && trim($configured) !== '') {
            return self::normalizeBasePath($configured);
        }

        return self::normalizeBasePath(dirname((string) ($_SERVER['SCRIPT_NAME'] ?? '/index.php')));
    }

    /**
     * Puts a base path into the one shape the rest of the code expects: a leading slash, no trailing
     * one, and the empty string for the document root.
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
        $uri = (string) ($_SERVER['REQUEST_URI'] ?? '/');
        $path = (string) parse_url($uri, PHP_URL_PATH);

        if ($basePath !== '' && str_starts_with($path, $basePath)) {
            $path = substr($path, strlen($basePath));
        }

        // A rewrite that never fired leaves the script itself in the path.
        if ($path === '' || $path === '/index.php') {
            $path = trim((string) ($_GET['_path'] ?? '')) ?: '/';
        }

        return '/' . ltrim(rawurldecode($path), '/');
    }
}

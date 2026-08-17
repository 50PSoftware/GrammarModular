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
     */
    public static function fromGlobals(): self
    {
        $basePath = self::readBasePath();

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
     * The directory the front controller is served from, without a trailing slash.
     *
     * Everything is addressed relative to this, so the admin works unchanged whether it sits at the
     * document root — the intended deployment — or in a subdirectory of one.
     */
    private static function readBasePath(): string
    {
        $script = (string) ($_SERVER['SCRIPT_NAME'] ?? '/index.php');
        $directory = rtrim(str_replace('\\', '/', dirname($script)), '/');

        return $directory === '/' ? '' : $directory;
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

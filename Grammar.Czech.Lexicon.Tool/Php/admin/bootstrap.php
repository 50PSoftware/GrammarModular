<?php

declare(strict_types=1);

/**
 * Wires the admin together: the class loader, the shared includes, and the kernel.
 *
 * Required from ../index.php, which is the only file the web server is meant to reach. Everything
 * below admin/ is denied wholesale by admin/.htaccess and is only ever pulled in from the filesystem.
 *
 * There is no composer here on purpose. The admin is deployed by copying Php/ into the document root
 * of shared hosting; a vendor/ directory would mean a build step between editing a file and seeing
 * the change, and the only thing composer would provide is the autoloader below, which is nine lines.
 */

// Files under admin/ must not be runnable directly even if .htaccess never applies. Each of them
// checks this constant, so it is defined before anything else is loaded.
defined('LEXICON_ADMIN') || define('LEXICON_ADMIN', true);

require_once __DIR__ . '/../env.php';
require_once __DIR__ . '/../schema-tables.php';

/**
 * Loads a class from admin/src/, mapping the namespace onto the directory tree.
 *
 * PSR-4 for the one prefix this application has. No directory scan and no class map: the file name is
 * computed from the class name, so a class that is not where its namespace says it is simply does not
 * load, which is the failure that is easiest to read.
 */
spl_autoload_register(static function (string $class): void {
    $prefix = 'Lexicon\\Admin\\';

    if (!str_starts_with($class, $prefix)) {
        return;
    }

    $relative = str_replace('\\', DIRECTORY_SEPARATOR, substr($class, strlen($prefix)));
    $path = __DIR__ . '/src/Lexicon/Admin/' . $relative . '.php';

    if (is_file($path)) {
        require_once $path;
    }
});

/**
 * Escapes text for HTML.
 *
 * A global function rather than a method on the view: the templates call it on every interpolation,
 * and a class prefix on each of them would bury the value being printed.
 */
function h(?string $value): string
{
    return htmlspecialchars((string) $value, ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
}

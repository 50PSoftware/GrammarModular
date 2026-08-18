<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\View\Url;
use ReflectionClass;
use ReflectionMethod;
use ReflectionNamedType;

/**
 * Checks that the routes the admin answers on and the addresses it publishes are the same set.
 *
 * The two lists are written out separately on purpose — Router says which path reaches which action,
 * Url builds paths with named methods so that a mistyped link is a fatal error at the call site rather
 * than a link that quietly goes nowhere. The price of that is a duplicate, and a duplicate nobody
 * compares drifts: rename a path in one and the other keeps producing the old one, which shows up as a
 * dead link somewhere nobody clicks often.
 *
 * So it is compared, and without a table mapping one to the other — a third list to keep in step would
 * only move the problem. Every Url method that takes nothing but identifiers is called with 1, 2, 3…,
 * every route pattern has its placeholders filled with the same numbers, and the two sets of concrete
 * paths have to match exactly.
 *
 * It builds its own Router and Url rather than taking them as arguments. Both are pure — one takes no
 * constructor arguments, the other a base path — and the check is about what the source code says, not
 * about the request being served. An empty base path keeps the comparison free of where the admin is
 * deployed.
 */
final class RouteParity
{
    /**
     * Everything that does not line up, as sentences that name what to do about it.
     *
     * Empty is the normal answer, and the whole point: this runs where somebody will read it.
     *
     * @return list<string>
     */
    public static function problems(): array
    {
        $routes = self::routePaths();
        $links = self::linkPaths();
        $problems = [];

        foreach (array_diff($routes, $links) as $path) {
            $problems[] = 'Router zná ' . $path . ', ale Url takovou adresu nestaví — na tu routu '
                . 'nevede odkaz.';
        }

        foreach (array_diff($links, $routes) as $path) {
            $problems[] = 'Url staví ' . $path . ', ale Router takovou routu nemá — ten odkaz skončí '
                . 'na 404.';
        }

        return $problems;
    }

    /**
     * Every path the router answers on, with the identifiers filled in.
     *
     * Deduplicated: reading and writing the same thing are two routes on one path.
     *
     * @return list<string>
     */
    private static function routePaths(): array
    {
        $paths = [];

        foreach ((new Router())->routes() as $route) {
            $paths[] = self::fillPlaceholders($route->pattern);
        }

        return array_values(array_unique($paths));
    }

    /**
     * Every path Url can build, with the same identifiers filled in.
     *
     * @return list<string>
     */
    private static function linkPaths(): array
    {
        $url = new Url('');
        $paths = [];

        foreach ((new ReflectionClass(Url::class))->getMethods(ReflectionMethod::IS_PUBLIC) as $method) {
            if ($method->isConstructor() || $method->isStatic()) {
                continue;
            }

            $arguments = self::identifiersFor($method);

            if ($arguments === null) {
                continue;
            }

            $paths[] = (string) $method->invokeArgs($url, $arguments);
        }

        return array_values(array_unique($paths));
    }

    /**
     * The arguments to call a Url method with, or null when it does not build a route.
     *
     * A method that takes anything other than identifiers is not addressing a page — asset() takes a
     * file name and belongs to the stylesheet, which the server hands out itself. Optional parameters
     * are left out: they add a query string to a path that is already in the list.
     *
     * @return list<int>|null
     */
    private static function identifiersFor(ReflectionMethod $method): ?array
    {
        $arguments = [];

        foreach ($method->getParameters() as $parameter) {
            if ($parameter->isOptional()) {
                break;
            }

            $type = $parameter->getType();

            if (!$type instanceof ReflectionNamedType || $type->getName() !== 'int') {
                return null;
            }

            $arguments[] = count($arguments) + 1;
        }

        return $arguments;
    }

    /**
     * Turns /heslo/{id}/podoba/{variantId}/smazat into /heslo/1/podoba/2/smazat.
     *
     * The numbers count up in the order the placeholders appear, which is the order a Url method takes
     * them in — so two paths built the same way come out identical only if they really are the same.
     */
    private static function fillPlaceholders(string $pattern): string
    {
        $next = 0;

        return (string) preg_replace_callback(
            '/\{[a-zA-Z]+\}/',
            static function () use (&$next): string {
                return (string) ++$next;
            },
            $pattern
        );
    }
}

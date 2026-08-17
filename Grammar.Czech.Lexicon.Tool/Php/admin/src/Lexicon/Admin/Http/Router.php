<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Controller\FrameController;
use Lexicon\Admin\Controller\LemmaController;
use Lexicon\Admin\Controller\LexemeController;
use Lexicon\Admin\Controller\ListController;
use Lexicon\Admin\Controller\SessionController;

/**
 * The table of what the admin answers on, and the lookup into it.
 *
 * Reading and writing the same thing are two routes, not one page that branches on the method, and
 * every write has a path of its own. The old admin carried the action in a hidden field and dispatched
 * on it with a switch; that made every form on the entry page a possible caller of every branch, which
 * is why saving a variant had to be guarded against overwriting the entry with the blanks its own form
 * did not contain.
 */
final class Router
{
    /** @var list<Route> */
    private array $routes;

    public function __construct()
    {
        $this->routes = [
            new Route('GET', '/', ListController::class, 'root'),
            new Route('GET', '/hesla', ListController::class, 'index'),

            new Route('GET', '/prihlaseni', SessionController::class, 'form', isPublic: true),
            new Route('POST', '/prihlaseni', SessionController::class, 'signIn', isPublic: true),
            new Route('POST', '/odhlaseni', SessionController::class, 'signOut'),

            new Route('GET', '/heslo/nove', LemmaController::class, 'create'),
            new Route('POST', '/heslo/nove', LemmaController::class, 'store'),
            new Route('GET', '/heslo/{id}', LemmaController::class, 'edit'),
            new Route('POST', '/heslo/{id}', LemmaController::class, 'update'),
            new Route('POST', '/heslo/{id}/smazat', LemmaController::class, 'destroy'),
            new Route('POST', '/heslo/{id}/podoba', LemmaController::class, 'addVariant'),
            new Route('POST', '/heslo/{id}/podoba/{variantId}/smazat', LemmaController::class, 'deleteVariant'),
            new Route('POST', '/heslo/{id}/vyznam/{luId}/zpusob-deje', LemmaController::class, 'saveSenseAktionsart'),

            new Route('GET', '/lexem/{id}', LexemeController::class, 'show'),
            new Route('POST', '/lexem/{id}', LexemeController::class, 'update'),
            new Route('POST', '/lexem/{id}/vyznam', LexemeController::class, 'addSense'),
            new Route('POST', '/lexem/{id}/vyznam/{luId}', LexemeController::class, 'updateSense'),
            new Route('POST', '/lexem/{id}/vyznam/{luId}/smazat', LexemeController::class, 'deleteSense'),
            new Route('POST', '/lexem/{id}/vyznam/{luId}/ramec', LexemeController::class, 'addFrame'),

            new Route('GET', '/ramec/{id}', FrameController::class, 'show'),
            new Route('POST', '/ramec/{id}', FrameController::class, 'update'),
            new Route('POST', '/ramec/{id}/slot', FrameController::class, 'addSlot'),
            new Route('POST', '/ramec/{id}/slot/{slotId}/smazat', FrameController::class, 'deleteSlot'),
            new Route('POST', '/ramec/{id}/slot/{slotId}/realizace', FrameController::class, 'addRealization'),
            new Route('POST', '/ramec/{id}/realizace/{realizationId}/smazat', FrameController::class, 'deleteRealization'),
        ];
    }

    /**
     * Finds the route for a request.
     *
     * @throws HttpException When nothing matches the path, or nothing matches it with this method.
     */
    public function match(Request $request): RouteMatch
    {
        $pathExists = false;

        foreach ($this->routes as $route) {
            $parameters = $route->match($request->path);

            if ($parameters === null) {
                continue;
            }

            if ($route->method === $request->method) {
                return new RouteMatch($route, $parameters);
            }

            $pathExists = true;
        }

        // A GET on a path that only accepts POST is somebody following a link that should have been a
        // form, not a missing page. Saying so is the difference between a bug that is obvious and one
        // that looks like a typo in the URL.
        throw $pathExists
            ? new HttpException(405, 'Tahle adresa se takhle nevolá.')
            : HttpException::notFound();
    }
}

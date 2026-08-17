<?php

declare(strict_types=1);

namespace Lexicon\Admin;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Http\HtmlResponse;
use Lexicon\Admin\Http\HttpException;
use Lexicon\Admin\Http\RedirectResponse;
use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Response;
use Lexicon\Admin\Http\Router;
use PDOException;
use Throwable;

/**
 * Turns a request into a response: session, form token, sign-in, route, controller.
 *
 * The order matters and is the same for every page, which is why it is here rather than repeated at
 * the top of each one. Everything runs under a single catch, because an unhandled PDO exception leaves
 * PHP printing a fatal error — with the server path, the file name and a stack trace — to whoever
 * caused it. The detail belongs in the log, not on the page.
 */
final class Kernel
{
    public function __construct(
        private readonly Application $application,
        private readonly Router $router
    ) {
    }

    /**
     * Answers one request.
     */
    public function handle(Request $request): Response
    {
        try {
            $this->application->session()->start();

            $match = $this->router->match($request);

            // The token is checked for every write before anything is dispatched, so no controller has
            // to remember to ask.
            if ($request->isPost() && !$this->application->csrf()->matches($request->form['csrf'] ?? null)) {
                throw HttpException::badToken();
            }

            if (!$match->route->isPublic && !$this->application->authenticator()->isSignedIn()) {
                return new RedirectResponse($this->application->url()->signIn());
            }

            $controller = $this->application->controller($match->route->controller);
            $action = $match->route->action;

            return $controller->$action($request, $match);
        } catch (HttpException $exception) {
            return $this->errorPage($exception->getMessage(), $exception->status());
        } catch (ConfigurationError $exception) {
            // Its message names what is not set and nothing about the server, so it is the one failure
            // worth repeating to the person looking at the screen.
            error_log('lexikon admin: ' . $exception);

            return $this->errorPage($exception->getMessage(), 500);
        } catch (Throwable $exception) {
            error_log('lexikon admin: ' . $exception);

            return $this->errorPage($this->explain($exception), 500);
        }
    }

    /**
     * Translates an exception into a sentence somebody can act on.
     *
     * Most of them say nothing intelligible and stay with the general message. One does, and is likely
     * enough to be worth naming: the schema on the server was created from schema.sql instead of
     * schema.mysql.sql, so the primary keys are not AUTO_INCREMENT. The admin never supplies an
     * identifier — it relies on the database assigning one — so the first insert of anything fails
     * with error 1364.
     */
    private function explain(Throwable $exception): string
    {
        $cause = $exception->getPrevious() ?? $exception;

        if (($exception instanceof PDOException || $cause instanceof PDOException)
            && str_contains($cause->getMessage(), "doesn't have a default value")) {
            return 'Tabulky nemají AUTO_INCREMENT na primárním klíči. Schéma nejspíš vzniklo '
                . 'z schema.sql, což je varianta pro SQLite — pro MariaDB platí schema.mysql.sql. '
                . 'Spusť Schema/repair.mysql-autoincrement.sql; data se tím nemění.';
        }

        return 'Něco se pokazilo. Podrobnost je v error logu serveru.';
    }

    /**
     * Renders the error page, which is a document of its own so that nothing it needs can fail twice.
     */
    private function errorPage(string $message, int $status): Response
    {
        try {
            return new HtmlResponse(
                $this->application->view()->render('error', ['message' => $message]),
                $status
            );
        } catch (Throwable) {
            // The template itself is broken or unreadable. Whatever is left has to be enough to say so.
            return new HtmlResponse(
                '<!doctype html><meta charset="utf-8"><p style="font:16px system-ui;padding:2rem">'
                . h($message) . '</p>',
                $status
            );
        }
    }
}

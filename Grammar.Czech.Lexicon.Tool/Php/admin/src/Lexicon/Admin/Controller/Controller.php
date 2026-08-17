<?php

declare(strict_types=1);

namespace Lexicon\Admin\Controller;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Http\HtmlResponse;
use Lexicon\Admin\Http\RedirectResponse;
use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Input\FormData;
use Lexicon\Admin\Input\OldInput;
use Lexicon\Admin\Schema;
use Lexicon\Admin\View\Flash;
use Lexicon\Admin\View\Url;
use Lexicon\Admin\View\View;

/**
 * Co má každý controller po ruce.
 *
 * Akce dostane request a identifikátory z cesty a vrátí odpověď. Nic netiskne a nikde nekončí
 * exitem — zápis vrací přesměrování, čtení vrací stránku, a odeslat to je věc Kernelu.
 */
abstract class Controller
{
    public function __construct(
        protected readonly View $view,
        protected readonly Url $url,
        protected readonly Flash $flash,
        protected readonly OldInput $old,
        protected readonly Schema $schema
    ) {
    }

    /**
     * Odeslaná pole formuláře.
     */
    protected function form(Request $request): FormData
    {
        return new FormData($request->form, $this->schema);
    }

    /**
     * Stránka.
     *
     * Bez lišty se vykresluje jen přihlášení: odhlásit se z něj nedá a nové heslo taky ne.
     *
     * @param array<string, mixed> $data
     */
    protected function page(string $template, array $data = [], bool $signedIn = true): HtmlResponse
    {
        return new HtmlResponse($this->view->page($template, $data, $signedIn));
    }

    /**
     * Přesměrování po zápisu, aby obnovení stránky formulář neodeslalo znovu.
     */
    protected function redirect(string $location): RedirectResponse
    {
        return new RedirectResponse($location);
    }

    /**
     * Odmítnutý zápis: hláška a návrat tam, odkud přišel.
     */
    protected function refuse(string $message, string $location): RedirectResponse
    {
        $this->flash->error($message);

        return $this->redirect($location);
    }

    /**
     * Odmítnuté uložení hesla: navíc si zapamatuje, co bylo ve formuláři.
     *
     * Jen pro hlavní formulář hesla, a to je záměr. Zapamatované hodnoty se vracejí do polí podle
     * jména sloupce, takže kdyby si je nechala i odmítnutá dubleta — formulář o dvou políčkách —
     * vykreslilo by se heslo o třiceti polích prázdné.
     */
    protected function refuseSave(string $message, string $location, Request $request): RedirectResponse
    {
        $this->old->keep($request->form);

        return $this->refuse($message, $location);
    }
}

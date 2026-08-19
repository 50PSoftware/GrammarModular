<?php

declare(strict_types=1);

namespace Lexicon\Admin\Controller;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Response;
use Lexicon\Admin\Http\RouteMatch;
use Lexicon\Admin\Input\OldInput;
use Lexicon\Admin\Repository\ApiTokenRepository;
use Lexicon\Admin\Schema;
use Lexicon\Admin\Security\Authenticator;
use Lexicon\Admin\View\Flash;
use Lexicon\Admin\View\Url;
use Lexicon\Admin\View\View;

/**
 * Osobní tokeny pro `lexikon.ps1 pull`, jeden účet, jedna stránka.
 */
final class TokenController extends Controller
{
    public function __construct(
        View $view,
        Url $url,
        Flash $flash,
        OldInput $old,
        Schema $schema,
        private readonly Authenticator $authenticator,
        private readonly ApiTokenRepository $tokens
    ) {
        parent::__construct($view, $url, $flash, $old, $schema);
    }

    /**
     * Seznam vlastních tokenů a formulář na nový.
     */
    public function index(Request $request, RouteMatch $route): Response
    {
        $userId = $this->authenticator->currentUser()['id'];

        return $this->page('tokens', [
            'tokens' => $this->tokens->forUser($userId),
            'newToken' => null,
        ]);
    }

    /**
     * Vygeneruje nový token a ukáže ho — naposledy, protože se ukládá jen jeho otisk.
     */
    public function store(Request $request, RouteMatch $route): Response
    {
        $userId = $this->authenticator->currentUser()['id'];
        $form = $this->formData($request);
        $label = $form->text('label');

        $token = $this->tokens->create($userId, $label);

        return $this->page('tokens', [
            'tokens' => $this->tokens->forUser($userId),
            'newToken' => $token,
        ]);
    }

    /**
     * Zruší token, jen pokud patří přihlášenému účtu.
     */
    public function destroy(Request $request, RouteMatch $route): Response
    {
        $userId = $this->authenticator->currentUser()['id'];
        $this->tokens->revoke($route->id('id'), $userId);
        $this->flash->ok('Token zrušen.');

        return $this->redirect($this->url->tokens());
    }
}

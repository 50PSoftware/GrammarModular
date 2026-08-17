<?php

declare(strict_types=1);

namespace Lexicon\Admin\Controller;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Response;
use Lexicon\Admin\Input\OldInput;
use Lexicon\Admin\Repository\LemmaRepository;
use Lexicon\Admin\Schema;
use Lexicon\Admin\View\Flash;
use Lexicon\Admin\View\Url;
use Lexicon\Admin\View\View;

/**
 * Seznam hesel.
 */
final class ListController extends Controller
{
    public function __construct(
        View $view,
        Url $url,
        Flash $flash,
        OldInput $old,
        Schema $schema,
        private readonly LemmaRepository $lemmas
    ) {
        parent::__construct($view, $url, $flash, $old, $schema);
    }

    /**
     * Kořen administrace je seznam; adresu má ale vlastní, aby se dala odkazovat s hledáním.
     */
    public function root(): Response
    {
        return $this->redirect($this->url->entries());
    }

    /**
     * Stránka seznamu s hledáním.
     */
    public function index(Request $request): Response
    {
        $query = $request->queryText('q');

        return $this->page('list/index', [
            'page' => $this->lemmas->search($query, $request->queryPage('strana')),
            'query' => $query,
        ]);
    }
}

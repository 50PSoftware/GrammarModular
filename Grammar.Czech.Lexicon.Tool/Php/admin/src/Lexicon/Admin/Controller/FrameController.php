<?php

declare(strict_types=1);

namespace Lexicon\Admin\Controller;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Database\IntegrityViolation;
use Lexicon\Admin\Http\HttpException;
use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Response;
use Lexicon\Admin\Http\RouteMatch;
use Lexicon\Admin\Input\OldInput;
use Lexicon\Admin\Read\FrameContext;
use Lexicon\Admin\Repository\FrameRepository;
use Lexicon\Admin\Schema;
use Lexicon\Admin\View\Flash;
use Lexicon\Admin\View\Url;
use Lexicon\Admin\View\View;

/**
 * Rámec, jeho sloty a povrchové realizace.
 */
final class FrameController extends Controller
{
    public function __construct(
        View $view,
        Url $url,
        Flash $flash,
        OldInput $old,
        Schema $schema,
        private readonly FrameRepository $frames
    ) {
        parent::__construct($view, $url, $flash, $old, $schema);
    }

    /**
     * Stránka rámce.
     */
    public function show(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $context = $this->frames->findById($id);

        if ($context === null) {
            $this->flash->error('Rámec neexistuje.');

            return $this->redirect($this->url->entries());
        }

        return $this->page('frame/show', [
            'context' => $context,
            'slots' => $this->frames->slots($id),
            'realizations' => $this->frames->realizationsBySlot($id),
        ]);
    }

    /**
     * Přepíše rámec.
     */
    public function update(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $this->requireFrame($id);
        $form = $this->formData($request);

        try {
            $this->frames->update(
                $id,
                $form->enumOr('kind', 'kind', 'Verbal'),
                $form->enumOr('diathesis', 'diathesis', 'Active'),
                $form->checkbox('is_default'),
                $form->enumOr('reflexive_type', 'reflexive_type', 'None')
            );
            $this->flash->ok('Uloženo.');
        } catch (IntegrityViolation) {
            // Diateze je půlka unikátního klíče, takže ji nejde přepsat na tu, kterou význam už má.
            // Dřív to nemohlo nastat — dokud byl každý rámec Active, měl význam nejvýš jeden.
            return $this->refuse(
                'Ten význam už rámec pro tuhle diatezi má. Jeden rámec na diatezi.',
                $this->url->frame($id)
            );
        }

        return $this->redirect($this->url->frame($id));
    }

    /**
     * Přidá rámci slot.
     */
    public function addSlot(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $this->requireFrame($id);
        $form = $this->formData($request);

        try {
            $this->frames->addSlot(
                $id,
                $form->enumOr('functor', 'functor', 'ACT'),
                $form->atLeast('canonical_order', 1),
                $form->enumOr('obligatoriness', 'obligatoriness', 'Optional'),
                $form->checkbox('can_drop_contextual'),
                $form->checkbox('can_drop_generic'),
                $form->enum('control_target', 'functor')
            );
            $this->flash->ok('Slot přidán. Bez realizace se nemůže vyjádřit.');
        } catch (IntegrityViolation) {
            return $this->refuse(
                'Tenhle funktor už v rámci je. Jeden slot na funktor.',
                $this->url->frame($id)
            );
        }

        return $this->redirect($this->url->frame($id));
    }

    /**
     * Smaže slot i s jeho realizacemi.
     */
    public function deleteSlot(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $slotId = $this->requireSlot($id, $route->id('slotId'));

        $this->frames->deleteSlotCascade($slotId, $id);
        $this->flash->ok('Slot smazán.');

        return $this->redirect($this->url->frame($id));
    }

    /**
     * Přidá slotu realizaci.
     */
    public function addRealization(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $slotId = $this->requireSlot($id, $route->id('slotId'));
        $form = $this->formData($request);

        $case = $form->enum('morph_case', 'morph_case');
        $clause = $form->text('clause_type');
        $infinitive = $form->checkbox('takes_infinitive');

        // Realizace musí být něčím: pádem, větou, nebo infinitivem. Řádek, který není ničím, by
        // databáze odmítla kontrolou ck_slot_realization_shape, ale hláška z ní uživateli nic neřekne.
        if ($case === null && $clause === null && $infinitive === 0) {
            return $this->refuse(
                'Realizace musí mít pád, typ věty, nebo být infinitivní.',
                $this->url->frame($id)
            );
        }

        if ($case === null && $form->text('preposition') !== null) {
            return $this->refuse(
                'Předložka bez pádu nic neřídí. Doplň pád, nebo předložku smaž.',
                $this->url->frame($id)
            );
        }

        $this->frames->addRealization(
            $slotId,
            $case,
            $form->text('preposition'),
            $clause,
            $infinitive,
            $form->atLeast('preference', 1)
        );
        $this->flash->ok('Realizace přidána.');

        return $this->redirect($this->url->frame($id));
    }

    /**
     * Smaže realizaci.
     */
    public function deleteRealization(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $this->requireFrame($id);

        $this->frames->deleteRealization($route->id('realizationId'), $id);
        $this->flash->ok('Realizace smazána.');

        return $this->redirect($this->url->frame($id));
    }

    /**
     * Načte rámec, na který akce sahá, nebo skončí.
     */
    private function requireFrame(int $id): FrameContext
    {
        $context = $this->frames->findById($id);

        if ($context === null) {
            throw HttpException::notFound();
        }

        return $context;
    }

    /**
     * Ověří, že slot patří tomuhle rámci, a vrátí jeho číslo.
     */
    private function requireSlot(int $frameId, int $slotId): int
    {
        $this->requireFrame($frameId);

        if (!$this->frames->hasSlot($frameId, $slotId)) {
            throw HttpException::notFound();
        }

        return $slotId;
    }
}

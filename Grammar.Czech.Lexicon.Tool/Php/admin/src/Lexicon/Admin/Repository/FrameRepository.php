<?php

declare(strict_types=1);

namespace Lexicon\Admin\Repository;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Database\Database;
use Lexicon\Admin\Entity\SlotRealization;
use Lexicon\Admin\Entity\ValencyFrame;
use Lexicon\Admin\Entity\ValencySlot;
use Lexicon\Admin\Read\FrameContext;

/**
 * Rámce, jejich sloty a povrchové realizace.
 */
final class FrameRepository
{
    public function __construct(private readonly Database $database)
    {
    }

    /**
     * Najde rámec i s významem a lexémem, na kterých visí.
     */
    public function findById(int $id): ?FrameContext
    {
        $row = $this->database->one(
            'SELECT f.*, u.sense_label, u.lexeme_id
               FROM valency_frame f
               JOIN lexical_unit u ON u.lu_id = f.lu_id
              WHERE f.frame_id = ?',
            [$id]
        );

        if ($row === null) {
            return null;
        }

        return new FrameContext(
            ValencyFrame::fromRow($row),
            $row['sense_label'] === null ? null : (string) $row['sense_label'],
            (int) $row['lexeme_id']
        );
    }

    /**
     * Přepíše rámec.
     */
    public function update(int $id, string $kind, string $diathesis, int $isDefault, string $reflexiveType): void
    {
        $this->database->run(
            'UPDATE valency_frame SET kind = ?, diathesis = ?, is_default = ?, reflexive_type = ?
             WHERE frame_id = ?',
            [$kind, $diathesis, $isDefault, $reflexiveType, $id]
        );
    }

    /**
     * Sloty rámce v kanonickém pořadí.
     *
     * @return list<ValencySlot>
     */
    public function slots(int $frameId): array
    {
        return array_map(
            ValencySlot::fromRow(...),
            $this->database->all(
                'SELECT * FROM valency_slot WHERE frame_id = ? ORDER BY canonical_order, slot_id',
                [$frameId]
            )
        );
    }

    /**
     * Realizace všech slotů rámce, seskupené po slotech.
     *
     * @return array<int, list<SlotRealization>>
     */
    public function realizationsBySlot(int $frameId): array
    {
        $realizations = [];

        $rows = $this->database->all(
            'SELECT r.* FROM slot_realization r
               JOIN valency_slot s ON s.slot_id = r.slot_id
              WHERE s.frame_id = ?
              ORDER BY r.preference, r.realization_id',
            [$frameId]
        );

        foreach ($rows as $row) {
            $realizations[(int) $row['slot_id']][] = SlotRealization::fromRow($row);
        }

        return $realizations;
    }

    /**
     * Přidá rámci slot.
     */
    public function addSlot(
        int $frameId,
        string $functor,
        int $canonicalOrder,
        string $obligatoriness,
        int $canDropContextual,
        int $canDropGeneric,
        ?string $controlTarget
    ): void {
        $this->database->run(
            'INSERT INTO valency_slot
                (frame_id, functor, canonical_order, obligatoriness,
                 can_drop_contextual, can_drop_generic, control_target)
             VALUES (?, ?, ?, ?, ?, ?, ?)',
            [
                $frameId,
                $functor,
                $canonicalOrder,
                $obligatoriness,
                $canDropContextual,
                $canDropGeneric,
                $controlTarget,
            ]
        );
    }

    /**
     * Určí, jestli slot patří tomuhle rámci.
     *
     * Volá se dřív než každý zápis, který dostal slot_id z formuláře — jinak by podvržené číslo sáhlo
     * do cizího rámce.
     */
    public function hasSlot(int $frameId, int $slotId): bool
    {
        return $this->database->one(
            'SELECT slot_id FROM valency_slot WHERE slot_id = ? AND frame_id = ?',
            [$slotId, $frameId]
        ) !== null;
    }

    /**
     * Smaže slot i s jeho realizacemi.
     *
     * V transakci: dva příkazy, které dohromady dávají jeden krok.
     */
    public function deleteSlotCascade(int $slotId, int $frameId): void
    {
        $this->database->transaction(function () use ($slotId, $frameId): void {
            $this->database->run('DELETE FROM slot_realization WHERE slot_id = ?', [$slotId]);
            $this->database->run('DELETE FROM valency_slot WHERE slot_id = ? AND frame_id = ?', [$slotId, $frameId]);
        });
    }

    /**
     * Přidá slotu realizaci.
     */
    public function addRealization(
        int $slotId,
        ?string $morphCase,
        ?string $preposition,
        ?string $clauseType,
        int $takesInfinitive,
        int $preference
    ): void {
        $this->database->run(
            'INSERT INTO slot_realization
                (slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
             VALUES (?, ?, ?, ?, ?, ?)',
            [$slotId, $morphCase, $preposition, $clauseType, $takesInfinitive, $preference]
        );
    }

    /**
     * Smaže realizaci.
     */
    public function deleteRealization(int $realizationId, int $frameId): void
    {
        // frame_id v podmínce přes sloty: číslo realizace přišlo z formuláře a samo o sobě by šlo
        // podvrhnout na realizaci cizího rámce.
        $this->database->run(
            'DELETE r FROM slot_realization r
               JOIN valency_slot s ON s.slot_id = r.slot_id
              WHERE r.realization_id = ? AND s.frame_id = ?',
            [$realizationId, $frameId]
        );
    }
}

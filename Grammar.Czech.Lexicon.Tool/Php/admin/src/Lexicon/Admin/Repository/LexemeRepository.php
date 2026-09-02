<?php

declare(strict_types=1);

namespace Lexicon\Admin\Repository;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Database\Database;
use Lexicon\Admin\Entity\Lexeme;
use Lexicon\Admin\Entity\LexicalUnit;
use Lexicon\Admin\Entity\SemanticRelation;
use Lexicon\Admin\Entity\ValencyFrame;
use Lexicon\Admin\Read\EntryChip;

/**
 * Lexémy, jejich významy a rámce na nich.
 */
final class LexemeRepository
{
    public function __construct(private readonly Database $database)
    {
    }

    /**
     * Najde lexém podle identifikátoru.
     */
    public function findById(int $id): ?Lexeme
    {
        $row = $this->database->one('SELECT * FROM lexeme WHERE lexeme_id = ?', [$id]);

        return $row === null ? null : Lexeme::fromRow($row);
    }

    /**
     * Všechny lexémy pro výběr ve formuláři hesla.
     *
     * @return list<Lexeme>
     */
    public function all(): array
    {
        return array_map(
            Lexeme::fromRow(...),
            $this->database->all('SELECT lexeme_id, primary_lemma, note FROM lexeme ORDER BY primary_lemma')
        );
    }

    /**
     * Založí lexém a vrátí jeho identifikátor.
     */
    public function create(string $primaryLemma): int
    {
        return $this->database->insert('INSERT INTO lexeme (primary_lemma) VALUES (?)', [$primaryLemma]);
    }

    /**
     * Přepíše lexém.
     */
    public function update(int $id, string $primaryLemma, ?string $note): void
    {
        $this->database->run(
            'UPDATE lexeme SET primary_lemma = ?, note = ? WHERE lexeme_id = ?',
            [$primaryLemma, $note, $id]
        );
    }

    /**
     * Hesla, která na lexém ukazují.
     *
     * @return list<EntryChip>
     */
    public function entries(int $lexemeId): array
    {
        return array_map(
            EntryChip::fromRow(...),
            $this->database->all(
                'SELECT lemma_entry_id, lemma, category, aspect FROM lemma_entry WHERE lexeme_id = ? ORDER BY lemma_key',
                [$lexemeId]
            )
        );
    }

    /**
     * Významy lexému.
     *
     * @return list<LexicalUnit>
     */
    public function senses(int $lexemeId): array
    {
        return array_map(
            LexicalUnit::fromRow(...),
            $this->database->all('SELECT * FROM lexical_unit WHERE lexeme_id = ? ORDER BY lu_id', [$lexemeId])
        );
    }

    /**
     * Určí, jestli význam patří tomuhle lexému.
     *
     * Volá se dřív než každý zápis, který dostal lu_id z formuláře. Bez toho by podvržené číslo sáhlo
     * na význam cizího lexému, aniž by to stránka dala najevo.
     */
    public function hasSense(int $lexemeId, int $luId): bool
    {
        return $this->database->one(
            'SELECT lu_id FROM lexical_unit WHERE lu_id = ? AND lexeme_id = ?',
            [$luId, $lexemeId]
        ) !== null;
    }

    /**
     * Přidá lexému význam.
     */
    public function addSense(int $lexemeId, ?string $senseLabel, ?string $gloss): void
    {
        $this->database->run(
            'INSERT INTO lexical_unit (lexeme_id, sense_label, gloss) VALUES (?, ?, ?)',
            [$lexemeId, $senseLabel, $gloss]
        );
    }

    /**
     * Přepíše význam.
     */
    public function updateSense(int $luId, int $lexemeId, ?string $senseLabel, ?string $gloss): void
    {
        $this->database->run(
            'UPDATE lexical_unit SET sense_label = ?, gloss = ? WHERE lu_id = ? AND lexeme_id = ?',
            [$senseLabel, $gloss, $luId, $lexemeId]
        );
    }

    /**
     * Smaže význam i všechno, co na něm visí.
     *
     * Rámce, sloty a realizace pod významem musí padnout s ním; MySQL by jinak cizí klíč odmítl a
     * smazání by z pohledu uživatele prostě nefungovalo. V transakci proto, že čtyři příkazy z toho
     * dělají jeden krok — selhání uprostřed by nechalo rámec bez slotů a nikdo by se k němu už
     * formulářem nedostal.
     */
    public function deleteSenseCascade(int $luId): void
    {
        $this->database->transaction(function () use ($luId): void {
            $this->database->run(
                'DELETE r FROM slot_realization r
                   JOIN valency_slot s ON s.slot_id = r.slot_id
                   JOIN valency_frame f ON f.frame_id = s.frame_id
                  WHERE f.lu_id = ?',
                [$luId]
            );
            $this->database->run(
                'DELETE s FROM valency_slot s
                   JOIN valency_frame f ON f.frame_id = s.frame_id
                  WHERE f.lu_id = ?',
                [$luId]
            );
            $this->database->run('DELETE FROM valency_frame WHERE lu_id = ?', [$luId]);
            $this->database->run('DELETE FROM lexical_unit WHERE lu_id = ?', [$luId]);
        });
    }

    /**
     * Rámce všech významů lexému, s počtem slotů.
     *
     * @return list<ValencyFrame>
     */
    public function frames(int $lexemeId): array
    {
        return array_map(
            ValencyFrame::fromRow(...),
            $this->database->all(
                'SELECT f.*,
                        (SELECT COUNT(*) FROM valency_slot s WHERE s.frame_id = f.frame_id) AS slots
                   FROM valency_frame f
                   JOIN lexical_unit u ON u.lu_id = f.lu_id
                  WHERE u.lexeme_id = ?
                  ORDER BY f.frame_id',
                [$lexemeId]
            )
        );
    }

    /**
     * Založí významu rámec.
     */
    public function addFrame(int $luId, string $kind, string $diathesis, int $isDefault): void
    {
        $this->database->run(
            'INSERT INTO valency_frame (lu_id, kind, diathesis, is_default) VALUES (?, ?, ?, ?)',
            [$luId, $kind, $diathesis, $isDefault]
        );
    }

    /**
     * Sémantické vztahy významu, s druhou stranou dvojice dotaženou pro zobrazení.
     *
     * Relace je symetrická a uložená jednou, takže OR na obou sloupcích je jediný způsob, jak najít
     * všechny vztahy jednoho významu bez ohledu na to, na které straně dvojice zrovna sedí. CASE ve
     * spojení vybírá tu druhou stranu, ať je to kterákoli.
     *
     * @return list<SemanticRelation>
     */
    public function relations(int $luId): array
    {
        return array_map(
            SemanticRelation::fromRow(...),
            $this->database->all(
                'SELECT r.*, ox.primary_lemma AS other_lemma, ou.sense_label AS other_sense_label
                   FROM semantic_relation r
                   JOIN lexical_unit ou ON ou.lu_id = CASE WHEN r.lu_id_a = ? THEN r.lu_id_b ELSE r.lu_id_a END
                   JOIN lexeme ox ON ox.lexeme_id = ou.lexeme_id
                  WHERE r.lu_id_a = ? OR r.lu_id_b = ?
                  ORDER BY r.relation_id',
                [$luId, $luId, $luId]
            )
        );
    }

    /**
     * Založí významu vztah k jinému významu.
     *
     * $otherLuId je cizí klíč na lexical_unit, ne na tenhle lexém — vztah spojuje dva významy bez ohledu
     * na to, jestli patří stejnému lexému, nebo dvěma různým.
     */
    public function addRelation(
        int $luId,
        int $otherLuId,
        string $relationType,
        ?string $antonymSubtype,
        ?float $strength,
        ?string $source,
        ?string $note
    ): void {
        $this->database->run(
            'INSERT INTO semantic_relation
                (lu_id_a, lu_id_b, relation_type, antonym_subtype, strength, source, note)
             VALUES (?, ?, ?, ?, ?, ?, ?)',
            [$luId, $otherLuId, $relationType, $antonymSubtype, $strength, $source, $note]
        );
    }

    /**
     * Určí, jestli vztah patří tomuhle významu (na kterékoli straně dvojice).
     *
     * Volá se před smazáním ze stejného důvodu jako hasSense: podvržené relationId by jinak smazalo
     * vztah cizího významu, aniž by to stránka dala najevo.
     */
    public function hasRelation(int $luId, int $relationId): bool
    {
        return $this->database->one(
            'SELECT relation_id FROM semantic_relation
              WHERE relation_id = ? AND (lu_id_a = ? OR lu_id_b = ?)',
            [$relationId, $luId, $luId]
        ) !== null;
    }

    /**
     * Smaže vztah.
     */
    public function deleteRelation(int $relationId): void
    {
        $this->database->run('DELETE FROM semantic_relation WHERE relation_id = ?', [$relationId]);
    }
}

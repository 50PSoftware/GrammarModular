<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Synonymie nebo antonymie mezi dvěma významy.
 *
 * otherLemma a otherSenseLabel nejsou sloupce semantic_relation — je to hlavní lemma a název významu
 * druhé strany dvojice, dotažené joinem, aby stránka jednoho významu nezobrazovala vztah jako holé číslo
 * lu_id, na které nikdo neklikne s jistotou, co za tím je.
 */
final class SemanticRelation
{
    public function __construct(
        public readonly ?int $id,
        public readonly int $luIdA,
        public readonly int $luIdB,
        public readonly string $relationType,
        public readonly ?string $antonymSubtype,
        public readonly ?float $strength,
        public readonly ?string $source,
        public readonly ?string $note,
        public readonly int $isVerified,
        public readonly string $otherLemma,
        public readonly ?string $otherSenseLabel
    ) {
    }

    /**
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            Value::int($row['relation_id'] ?? null),
            (int) $row['lu_id_a'],
            (int) $row['lu_id_b'],
            (string) $row['relation_type'],
            $row['antonym_subtype'] !== null ? (string) $row['antonym_subtype'] : null,
            $row['strength'] !== null ? (float) $row['strength'] : null,
            $row['source'] !== null ? (string) $row['source'] : null,
            $row['note'] !== null ? (string) $row['note'] : null,
            (int) ($row['is_verified'] ?? 0),
            (string) ($row['other_lemma'] ?? ''),
            $row['other_sense_label'] !== null ? (string) $row['other_sense_label'] : null
        );
    }

    /**
     * Identifikátor významu na druhé straně dvojice, viděno od $anchorLuId.
     *
     * Relace je symetrická a uložená jednou — bez tohohle by každé volající místo muselo samo řešit,
     * jestli je jeho význam lu_id_a nebo lu_id_b.
     */
    public function otherLuId(int $anchorLuId): int
    {
        return $this->luIdA === $anchorLuId ? $this->luIdB : $this->luIdA;
    }

    /**
     * Popisek druhé strany pro zobrazení: lemma a název významu, pokud ho má.
     */
    public function otherDisplayLabel(): string
    {
        return $this->otherSenseLabel === null
            ? $this->otherLemma
            : $this->otherLemma . ' – ' . $this->otherSenseLabel;
    }
}

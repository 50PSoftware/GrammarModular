<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * A second standard spelling of the same entry — setmět beside setmít.
 *
 * The dictionary recognises it but never produces it: a lookup under the variant returns the entry,
 * and what comes out is the entry's own lemma.
 */
final class LemmaVariant
{
    public function __construct(
        public readonly ?int $id,
        public readonly int $lemmaEntryId,
        public readonly string $lemma,
        public readonly string $lemmaKey,
        public readonly ?string $note
    ) {
    }

    /**
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            Value::int($row['variant_id'] ?? null),
            (int) $row['lemma_entry_id'],
            (string) $row['lemma'],
            (string) $row['lemma_key'],
            Value::text($row['note'] ?? null)
        );
    }
}

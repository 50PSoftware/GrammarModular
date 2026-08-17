<?php

declare(strict_types=1);

namespace Lexicon\Admin\Read;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Entity\Value;

/**
 * An entry named in passing on another page — the lemma, and just enough to tell two of them apart.
 */
final class EntryChip
{
    public function __construct(
        public readonly int $id,
        public readonly string $lemma,
        public readonly string $category,
        public readonly ?string $aspect
    ) {
    }

    /**
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            (int) $row['lemma_entry_id'],
            (string) $row['lemma'],
            (string) $row['category'],
            Value::text($row['aspect'] ?? null)
        );
    }
}

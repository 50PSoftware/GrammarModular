<?php

declare(strict_types=1);

namespace Lexicon\Admin\Read;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Entity\Value;

/**
 * One line of the entry list: the columns worth seeing side by side, plus how many frames the entry
 * reaches through its lexeme.
 */
final class ListRow
{
    public function __construct(
        public readonly int $id,
        public readonly string $lemma,
        public readonly string $category,
        public readonly ?string $gender,
        public readonly ?string $pattern,
        public readonly ?string $aspect,
        public readonly int $isVerified,
        public readonly ?int $lexemeId,
        public readonly int $frames
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
            Value::text($row['gender'] ?? null),
            Value::text($row['pattern'] ?? null),
            Value::text($row['aspect'] ?? null),
            (int) $row['is_verified'],
            Value::int($row['lexeme_id'] ?? null),
            (int) ($row['frames'] ?? 0)
        );
    }
}

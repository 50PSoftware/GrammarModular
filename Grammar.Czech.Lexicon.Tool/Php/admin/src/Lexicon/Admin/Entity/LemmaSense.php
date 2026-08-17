<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * What one entry says about one sense of its lexeme.
 *
 * It hangs on the pair, not on the sense alone, because a sense belongs to the lexeme and the lexeme
 * is the aspectual pair — a value recorded on the sense would land on the perfective counterpart too.
 * Zmrzlo is resultative in both of its senses where mrzne is stative, and one row could not tell them
 * apart.
 */
final class LemmaSense
{
    public function __construct(
        public readonly ?int $id,
        public readonly int $lemmaEntryId,
        public readonly int $luId,
        public readonly ?string $aktionsart,
        public readonly ?string $note
    ) {
    }

    /**
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            Value::int($row['lemma_sense_id'] ?? null),
            (int) $row['lemma_entry_id'],
            (int) $row['lu_id'],
            Value::text($row['aktionsart'] ?? null),
            Value::text($row['note'] ?? null)
        );
    }
}

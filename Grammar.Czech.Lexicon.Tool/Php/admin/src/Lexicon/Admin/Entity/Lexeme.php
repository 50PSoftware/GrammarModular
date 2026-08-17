<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * The abstract word behind one or more entries.
 *
 * An aspectual pair is one lexeme with two entries, which is what makes dát and dávat share a single
 * set of frames instead of two copies that drift apart.
 */
final class Lexeme
{
    public function __construct(
        public readonly ?int $id,
        public readonly string $primaryLemma,
        public readonly ?string $note
    ) {
    }

    /**
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            Value::int($row['lexeme_id'] ?? null),
            (string) $row['primary_lemma'],
            Value::text($row['note'] ?? null)
        );
    }
}

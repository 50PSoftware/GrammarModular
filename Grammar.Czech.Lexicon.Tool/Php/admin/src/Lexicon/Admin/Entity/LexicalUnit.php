<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * One sense of a lexeme — what the old JSON called a frameLabel.
 *
 * Frames hang off a sense, one per diathesis.
 */
final class LexicalUnit
{
    public function __construct(
        public readonly ?int $id,
        public readonly int $lexemeId,
        public readonly ?string $senseLabel,
        public readonly ?string $gloss,
        public readonly ?int $sscClassId
    ) {
    }

    /**
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            Value::int($row['lu_id'] ?? null),
            (int) $row['lexeme_id'],
            Value::text($row['sense_label'] ?? null),
            Value::text($row['gloss'] ?? null),
            Value::int($row['ssc_class_id'] ?? null)
        );
    }

    /**
     * What to call the sense on screen when it has no label.
     */
    public function displayLabel(): string
    {
        return $this->senseLabel ?? '(bez názvu)';
    }
}

<?php

declare(strict_types=1);

namespace Lexicon\Admin\Read;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Entity\Value;

/**
 * A sense of the lexeme together with what this entry says about it.
 *
 * The join is outer and the extra columns are usually empty: the vast majority of entry–sense pairs
 * record nothing, and the page has to list them all the same.
 */
final class EntrySense
{
    public function __construct(
        public readonly int $luId,
        public readonly ?string $senseLabel,
        public readonly ?string $gloss,
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
            (int) $row['lu_id'],
            Value::text($row['sense_label'] ?? null),
            Value::text($row['gloss'] ?? null),
            Value::text($row['aktionsart'] ?? null),
            Value::text($row['sense_note'] ?? null)
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

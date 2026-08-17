<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * How a slot can be said on the surface: a case, a clause, or an infinitive.
 *
 * Preference 1 is the one that gets generated; the rest are recognised and recorded.
 */
final class SlotRealization
{
    public function __construct(
        public readonly ?int $id,
        public readonly int $slotId,
        public readonly ?string $morphCase,
        public readonly ?string $preposition,
        public readonly ?string $clauseType,
        public readonly int $takesInfinitive,
        public readonly int $preference
    ) {
    }

    /**
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            Value::int($row['realization_id'] ?? null),
            (int) $row['slot_id'],
            Value::text($row['morph_case'] ?? null),
            Value::text($row['preposition'] ?? null),
            Value::text($row['clause_type'] ?? null),
            (int) $row['takes_infinitive'],
            (int) $row['preference']
        );
    }

    /**
     * Determines whether this is the realization the generator will use.
     */
    public function isGenerated(): bool
    {
        return $this->preference === 1;
    }
}

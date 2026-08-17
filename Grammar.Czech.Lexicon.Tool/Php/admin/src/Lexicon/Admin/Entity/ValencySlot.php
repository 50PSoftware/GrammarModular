<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * One participant of a frame, named by its functor.
 */
final class ValencySlot
{
    public function __construct(
        public readonly ?int $id,
        public readonly int $frameId,
        public readonly string $functor,
        public readonly int $canonicalOrder,
        public readonly string $obligatoriness,
        public readonly int $canDropContextual,
        public readonly int $canDropGeneric,
        public readonly ?string $controlTarget
    ) {
    }

    /**
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            Value::int($row['slot_id'] ?? null),
            (int) $row['frame_id'],
            (string) $row['functor'],
            (int) $row['canonical_order'],
            (string) $row['obligatoriness'],
            (int) $row['can_drop_contextual'],
            (int) $row['can_drop_generic'],
            Value::text($row['control_target'] ?? null)
        );
    }
}

<?php

declare(strict_types=1);

namespace Lexicon\Admin\Read;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * One page of a listing, and enough to draw the pager under it.
 *
 * @template T
 */
final class Page
{
    /**
     * @param list<T> $rows
     */
    public function __construct(
        public readonly array $rows,
        public readonly int $total,
        public readonly int $number,
        public readonly int $count
    ) {
    }

    /**
     * The page numbers to offer, with a gap marked by null.
     *
     * The old pager printed every number, which is fine at forty entries and a nav of hundreds of
     * links at a few thousand. This keeps the current page in a window of neighbours and always offers
     * the first and the last, so no page becomes unreachable.
     *
     * @return list<int|null>
     */
    public function numbers(int $window = 3): array
    {
        $wanted = [1, $this->count];

        for ($number = $this->number - $window; $number <= $this->number + $window; $number++) {
            if ($number >= 1 && $number <= $this->count) {
                $wanted[] = $number;
            }
        }

        $wanted = array_values(array_unique($wanted));
        sort($wanted);

        $numbers = [];
        $previous = 0;

        foreach ($wanted as $number) {
            if ($previous !== 0 && $number > $previous + 1) {
                $numbers[] = null;
            }

            $numbers[] = $number;
            $previous = $number;
        }

        return $numbers;
    }
}

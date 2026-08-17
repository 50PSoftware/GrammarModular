<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * One dictionary entry: a word as it is written, with everything the inflection services need to
 * decline or conjugate it.
 *
 * The flags are nullable on purpose and hold three states. Zero says the word does not have the
 * property, null says nobody has recorded whether it does, and the resolvers act on the difference —
 * a null lets the phonological rule decide, a zero overrules it.
 */
final class LemmaEntry
{
    public const TABLE = 'lemma_entry';

    public function __construct(
        public readonly ?int $id,
        public readonly string $lemma,
        public readonly string $lemmaKey,
        public readonly ?int $homonymIndex,
        public readonly string $category,
        public readonly ?string $gender,
        public readonly ?string $pattern,
        public readonly ?int $isAnimate,
        public readonly ?int $hasMobileE,
        public readonly ?int $hasGenitivePluralShortening,
        public readonly ?int $hasEpenthesisInGenitivePlural,
        public readonly ?int $isIndeclinable,
        public readonly ?int $isPluralOnly,
        public readonly ?int $isCountable,
        public readonly ?int $prefersShortForm,
        public readonly ?string $verbClass,
        public readonly ?string $aspect,
        public readonly ?string $aspectCounterpart,
        public readonly ?string $aktionsart,
        public readonly string $reflexiveType,
        public readonly ?string $baseVerbLemma,
        public readonly ?string $inherentFunctor,
        public readonly ?string $stem,
        public readonly ?string $presentStem,
        public readonly ?string $pastStem,
        public readonly ?string $futureStem,
        public readonly ?string $imperativeStem,
        public readonly ?string $passiveStem,
        public readonly ?string $infinitive,
        public readonly ?int $formsPassive,
        public readonly ?int $lexemeId,
        public readonly ?string $source,
        public readonly int $isVerified,
        public readonly ?string $note
    ) {
    }

    /**
     * Reads an entry out of a database row.
     *
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            Value::int($row['lemma_entry_id'] ?? null),
            (string) $row['lemma'],
            (string) $row['lemma_key'],
            Value::int($row['homonym_index'] ?? null),
            (string) $row['category'],
            Value::text($row['gender'] ?? null),
            Value::text($row['pattern'] ?? null),
            Value::int($row['is_animate'] ?? null),
            Value::int($row['has_mobile_e'] ?? null),
            Value::int($row['has_genitive_plural_shortening'] ?? null),
            Value::int($row['has_epenthesis_in_genitive_plural'] ?? null),
            Value::int($row['is_indeclinable'] ?? null),
            Value::int($row['is_plural_only'] ?? null),
            Value::int($row['is_countable'] ?? null),
            Value::int($row['prefers_short_form'] ?? null),
            Value::text($row['verb_class'] ?? null),
            Value::text($row['aspect'] ?? null),
            Value::text($row['aspect_counterpart'] ?? null),
            Value::text($row['aktionsart'] ?? null),
            (string) ($row['reflexive_type'] ?? 'None'),
            Value::text($row['base_verb_lemma'] ?? null),
            Value::text($row['inherent_functor'] ?? null),
            Value::text($row['stem'] ?? null),
            Value::text($row['present_stem'] ?? null),
            Value::text($row['past_stem'] ?? null),
            Value::text($row['future_stem'] ?? null),
            Value::text($row['imperative_stem'] ?? null),
            Value::text($row['passive_stem'] ?? null),
            Value::text($row['infinitive'] ?? null),
            Value::int($row['forms_passive'] ?? null),
            Value::int($row['lexeme_id'] ?? null),
            Value::text($row['source'] ?? null),
            (int) ($row['is_verified'] ?? 0),
            Value::text($row['note'] ?? null)
        );
    }

    /**
     * Writes the entry back out as columns.
     *
     * The identifier is not among them: the database assigns it, and the admin never supplies one. The
     * repository builds its statements from Schema::writableColumns() and looks each one up here, so a
     * column added to the schema and not to this map fails loudly on the next save rather than being
     * dropped from the insert.
     *
     * @return array<string, mixed>
     */
    public function toRow(): array
    {
        return [
            'lemma' => $this->lemma,
            'lemma_key' => $this->lemmaKey,
            'homonym_index' => $this->homonymIndex,
            'category' => $this->category,
            'gender' => $this->gender,
            'pattern' => $this->pattern,
            'is_animate' => $this->isAnimate,
            'has_mobile_e' => $this->hasMobileE,
            'has_genitive_plural_shortening' => $this->hasGenitivePluralShortening,
            'has_epenthesis_in_genitive_plural' => $this->hasEpenthesisInGenitivePlural,
            'is_indeclinable' => $this->isIndeclinable,
            'is_plural_only' => $this->isPluralOnly,
            'is_countable' => $this->isCountable,
            'prefers_short_form' => $this->prefersShortForm,
            'verb_class' => $this->verbClass,
            'aspect' => $this->aspect,
            'aspect_counterpart' => $this->aspectCounterpart,
            'aktionsart' => $this->aktionsart,
            'reflexive_type' => $this->reflexiveType,
            'base_verb_lemma' => $this->baseVerbLemma,
            'inherent_functor' => $this->inherentFunctor,
            'stem' => $this->stem,
            'present_stem' => $this->presentStem,
            'past_stem' => $this->pastStem,
            'future_stem' => $this->futureStem,
            'imperative_stem' => $this->imperativeStem,
            'passive_stem' => $this->passiveStem,
            'infinitive' => $this->infinitive,
            'forms_passive' => $this->formsPassive,
            'lexeme_id' => $this->lexemeId,
            'source' => $this->source,
            'is_verified' => $this->isVerified,
            'note' => $this->note,
        ];
    }
}

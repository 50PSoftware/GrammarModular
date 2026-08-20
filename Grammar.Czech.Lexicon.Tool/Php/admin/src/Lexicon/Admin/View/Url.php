<?php

declare(strict_types=1);

namespace Lexicon\Admin\View;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Every address the admin publishes, built in one place.
 *
 * Named methods rather than a route table with names: there are twenty of them, they are all known at
 * build time, and a mistyped method is a fatal error where a mistyped route name would be a link that
 * quietly goes nowhere. The base path lets the whole thing sit in a subdirectory of a document root.
 */
final class Url
{
    public function __construct(private readonly string $basePath)
    {
    }

    public function root(): string
    {
        return $this->to('/');
    }

    /**
     * The entry list, optionally filtered and paged.
     */
    public function entries(?string $query = null, ?int $page = null): string
    {
        return $this->to('/hesla', ['q' => $query, 'strana' => $page]);
    }

    public function signIn(): string
    {
        return $this->to('/prihlaseni');
    }

    public function signOut(): string
    {
        return $this->to('/odhlaseni');
    }

    public function tokens(): string
    {
        return $this->to('/tokeny');
    }

    public function export(): string
    {
        return $this->to('/export');
    }

    public function deleteToken(int $id): string
    {
        return $this->to("/tokeny/$id/smazat");
    }

    public function newEntry(): string
    {
        return $this->to('/heslo/nove');
    }

    public function entry(int $id): string
    {
        return $this->to("/heslo/$id");
    }

    public function deleteEntry(int $id): string
    {
        return $this->to("/heslo/$id/smazat");
    }

    public function addVariant(int $id): string
    {
        return $this->to("/heslo/$id/podoba");
    }

    public function deleteVariant(int $id, int $variantId): string
    {
        return $this->to("/heslo/$id/podoba/$variantId/smazat");
    }

    public function senseAktionsart(int $id, int $luId): string
    {
        return $this->to("/heslo/$id/vyznam/$luId/zpusob-deje");
    }

    public function lexeme(int $id): string
    {
        return $this->to("/lexem/$id");
    }

    public function addSense(int $id): string
    {
        return $this->to("/lexem/$id/vyznam");
    }

    public function sense(int $id, int $luId): string
    {
        return $this->to("/lexem/$id/vyznam/$luId");
    }

    public function deleteSense(int $id, int $luId): string
    {
        return $this->to("/lexem/$id/vyznam/$luId/smazat");
    }

    public function addFrame(int $id, int $luId): string
    {
        return $this->to("/lexem/$id/vyznam/$luId/ramec");
    }

    public function frame(int $id): string
    {
        return $this->to("/ramec/$id");
    }

    public function addSlot(int $id): string
    {
        return $this->to("/ramec/$id/slot");
    }

    public function deleteSlot(int $id, int $slotId): string
    {
        return $this->to("/ramec/$id/slot/$slotId/smazat");
    }

    public function addRealization(int $id, int $slotId): string
    {
        return $this->to("/ramec/$id/slot/$slotId/realizace");
    }

    public function deleteRealization(int $id, int $realizationId): string
    {
        return $this->to("/ramec/$id/realizace/$realizationId/smazat");
    }

    /**
     * A file served beside the front controller.
     */
    public function asset(string $file): string
    {
        return $this->to('/' . ltrim($file, '/'));
    }

    /**
     * Joins a path onto the base and appends the parameters that have a value.
     *
     * @param array<string, string|int|null> $parameters
     */
    private function to(string $path, array $parameters = []): string
    {
        $parameters = array_filter($parameters, static fn ($value): bool => $value !== null);
        $query = $parameters === [] ? '' : '?' . http_build_query($parameters);

        return ($this->basePath . $path) . $query;
    }
}

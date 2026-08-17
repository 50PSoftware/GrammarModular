<?php

declare(strict_types=1);

namespace Lexicon\Admin\View;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Schema;
use Throwable;

/**
 * Renders a template into a string.
 *
 * Templates are PHP files, included inside a method so they see exactly what they are given plus the
 * four helpers every page needs — $url, $form, $schema and $view — and nothing else. The old pages ran
 * in the front controller's scope, where every variable the controller happened to have was in reach.
 *
 * A template returns its markup instead of printing it, which is what lets a controller decide to
 * redirect after it has already built part of a page, with nothing to unsend.
 */
final class View
{
    /**
     * Templates are .phtml, not .php: the extension says at a glance which files are markup with holes
     * in it and which are classes, and it keeps them out of any tooling — a linter, a static analyser,
     * a glob in a build file — that goes looking for PHP sources.
     */
    private const EXTENSION = '.phtml';

    public function __construct(
        private readonly string $directory,
        private readonly Url $url,
        private readonly FormHelper $form,
        private readonly Schema $schema,
        private readonly Flash $flash
    ) {
    }

    /**
     * Renders a template inside the page frame.
     *
     * @param array<string, mixed> $data
     */
    public function page(string $template, array $data = [], bool $signedIn = true): string
    {
        return $this->render('layout', [
            'content' => $this->render($template, $data),
            'signedIn' => $signedIn,
            'flashes' => $this->flash->take(),
        ]);
    }

    /**
     * Renders a template on its own, for the parts a page is assembled from.
     *
     * @param array<string, mixed> $data
     */
    public function render(string $template, array $data = []): string
    {
        $path = $this->directory . '/' . $template . self::EXTENSION;

        if (!is_file($path)) {
            throw new \RuntimeException("Šablona '$template' neexistuje.");
        }

        $data['url'] = $this->url;
        $data['form'] = $this->form;
        $data['schema'] = $this->schema;
        $data['view'] = $this;

        ob_start();

        try {
            (static function (string $path, array $data): void {
                extract($data, EXTR_SKIP);

                require $path;
            })($path, $data);
        } catch (Throwable $exception) {
            ob_end_clean();

            throw $exception;
        }

        return (string) ob_get_clean();
    }
}

<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * What a controller answers with.
 *
 * A controller returns one of these instead of printing and calling exit(). Sending happens in one
 * place, at the end of the kernel, which is why a redirect no longer has to discard a half-rendered
 * page on its way out: nothing has been written yet.
 */
class Response
{
    /**
     * @param array<string, string> $headers
     */
    public function __construct(
        public readonly string $body = '',
        public readonly int $status = 200,
        public readonly array $headers = []
    ) {
    }

    /**
     * Writes the response out.
     */
    public function send(): void
    {
        http_response_code($this->status);

        foreach ($this->headers as $name => $value) {
            header($name . ': ' . $value);
        }

        echo $this->body;
    }
}

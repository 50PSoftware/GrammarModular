<?php

declare(strict_types=1);

namespace Lexicon\Admin;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use InvalidArgumentException;
use Lexicon\Admin\Controller\Controller;
use Lexicon\Admin\Controller\ExportController;
use Lexicon\Admin\Controller\FrameController;
use Lexicon\Admin\Controller\LemmaController;
use Lexicon\Admin\Controller\LexemeController;
use Lexicon\Admin\Controller\ListController;
use Lexicon\Admin\Controller\SessionController;
use Lexicon\Admin\Controller\TokenController;
use Lexicon\Admin\Database\Database;
use Lexicon\Admin\Database\WebUserDatabase;
use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Input\OldInput;
use Lexicon\Admin\Input\PatternValidator;
use Lexicon\Admin\Repository\ApiTokenRepository;
use Lexicon\Admin\Repository\FrameRepository;
use Lexicon\Admin\Repository\LemmaRepository;
use Lexicon\Admin\Repository\LexemeRepository;
use Lexicon\Admin\Security\Authenticator;
use Lexicon\Admin\Security\CsrfToken;
use Lexicon\Admin\Security\Session;
use Lexicon\Admin\View\Flash;
use Lexicon\Admin\View\FormHelper;
use Lexicon\Admin\View\Url;
use Lexicon\Admin\View\View;

/**
 * Builds the objects the request needs, and builds each of them once.
 *
 * Hand-written rather than a container that reads type hints. There are two dozen classes and their
 * wiring fits on a screen; a reflective container would add a dependency, a cache directory and a
 * failure mode where a missing constructor argument is discovered at runtime instead of here.
 *
 * Everything is built lazily, which is what keeps a page that never touches the dictionary — the login
 * screen, the error page — from opening a database connection to render.
 */
final class Application
{
    /** @var array<string, object> */
    private array $built = [];

    public function __construct(
        private readonly Request $request,
        private readonly string $viewDirectory
    ) {
    }

    public function session(): Session
    {
        return $this->once(Session::class, fn (): Session => new Session($this->request->isSecure));
    }

    public function config(): Config
    {
        return $this->once(Config::class, static fn (): Config => new Config());
    }

    public function schema(): Schema
    {
        return $this->once(Schema::class, static fn (): Schema => new Schema());
    }

    public function database(): Database
    {
        return $this->once(Database::class, fn (): Database => new Database($this->config()));
    }

    public function webUserDatabase(): WebUserDatabase
    {
        return $this->once(
            WebUserDatabase::class,
            fn (): WebUserDatabase => new WebUserDatabase($this->config())
        );
    }

    public function csrf(): CsrfToken
    {
        return $this->once(CsrfToken::class, fn (): CsrfToken => new CsrfToken($this->session()));
    }

    public function authenticator(): Authenticator
    {
        return $this->once(
            Authenticator::class,
            fn (): Authenticator => new Authenticator($this->session(), $this->config(), $this->webUserDatabase())
        );
    }

    public function url(): Url
    {
        return $this->once(Url::class, fn (): Url => new Url($this->request->basePath));
    }

    public function flash(): Flash
    {
        return $this->once(Flash::class, fn (): Flash => new Flash($this->session()));
    }

    public function oldInput(): OldInput
    {
        return $this->once(OldInput::class, fn (): OldInput => new OldInput($this->session()));
    }

    public function view(): View
    {
        return $this->once(View::class, fn (): View => new View(
            $this->viewDirectory,
            $this->url(),
            new FormHelper($this->schema(), $this->csrf()),
            $this->schema(),
            $this->flash()
        ));
    }

    /**
     * Builds the controller a route named.
     *
     * @param class-string $class
     */
    public function controller(string $class): Controller
    {
        return match ($class) {
            ListController::class => new ListController(
                $this->view(),
                $this->url(),
                $this->flash(),
                $this->oldInput(),
                $this->schema(),
                new LemmaRepository($this->database(), $this->schema())
            ),
            LemmaController::class => new LemmaController(
                $this->view(),
                $this->url(),
                $this->flash(),
                $this->oldInput(),
                $this->schema(),
                new LemmaRepository($this->database(), $this->schema()),
                new LexemeRepository($this->database()),
                new PatternValidator($this->schema()),
                $this->database()
            ),
            LexemeController::class => new LexemeController(
                $this->view(),
                $this->url(),
                $this->flash(),
                $this->oldInput(),
                $this->schema(),
                new LexemeRepository($this->database())
            ),
            FrameController::class => new FrameController(
                $this->view(),
                $this->url(),
                $this->flash(),
                $this->oldInput(),
                $this->schema(),
                new FrameRepository($this->database())
            ),
            SessionController::class => new SessionController(
                $this->view(),
                $this->url(),
                $this->flash(),
                $this->oldInput(),
                $this->schema(),
                $this->authenticator()
            ),
            TokenController::class => new TokenController(
                $this->view(),
                $this->url(),
                $this->flash(),
                $this->oldInput(),
                $this->schema(),
                $this->authenticator(),
                new ApiTokenRepository($this->database())
            ),
            ExportController::class => new ExportController(
                $this->view(),
                $this->url(),
                $this->flash(),
                $this->oldInput(),
                $this->schema(),
                $this->database()
            ),
            default => throw new InvalidArgumentException("Controller '$class' se nedá sestavit."),
        };
    }

    /**
     * @template T of object
     * @param class-string<T> $key
     * @param callable(): T $build
     * @return T
     */
    private function once(string $key, callable $build): object
    {
        /** @var T */
        return $this->built[$key] ??= $build();
    }
}

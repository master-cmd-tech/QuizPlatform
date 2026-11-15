# QuizLab — Interactive Learning and Quiz Platform

Final project for **FP 3222 Introduction to Functional Programming** (Astana IT University, 2026-2027).
A Blazor Server web app (C# 12 / .NET 8) that combines short lessons, timed quizzes, adaptive practice,
spaced-repetition flashcards, a live leaderboard and teacher tools. The domain logic is written in a
**functional style**: immutable records, discriminated unions, Option/Result/Validated, pure functions,
LINQ pipelines, currying, trampolining and lazy sequences.

> Team: **[Full name 1]**, **[Full name 2]** (max. 2 students — replace before the defense).

## Quick start

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download) (Windows, macOS or Linux). No database needed.

```bash
dotnet restore
dotnet test                                   # run the automated tests
dotnet run --project src/QuizPlatform.Web     # http://localhost:5080
```

Docker alternative: `docker build -t quizlab . && docker run -p 8080:8080 -v quizlab-data:/app/App_Data quizlab`

First steps in the app: open the home page → sign in with any name (Student) →
**Quizzes → Start**. To use teacher features choose the **Teacher** role and enter the teacher code
(`teacher2026` by default, change it in `src/QuizPlatform.Web/appsettings.json` → `Auth:TeacherCode`).

## Features

| Area | What it does |
|---|---|
| Quizzes | 4 question types (multiple choice, true/false, short answer, numeric), per-question timer, optional shuffle, instant feedback with explanations, streak bonus, letter grade on the university scale |
| Results | Score, bonus, grade, time, full answer review |
| Adaptive practice | Difficulty rises after 2 correct answers in a row, falls after a mistake |
| Flashcards | SM-2 spaced repetition — each card is rescheduled by how well you remembered it |
| Lessons | Short FP/C#/LINQ lessons with code samples and links to related quizzes |
| Leaderboard | Best attempt per player, updates **live** in every open browser |
| My stats | Averages, accuracy per topic, progress between attempts, longest correct streak |
| Teacher tools | Create quizzes from plain text with full validation, delete quizzes, see hardest questions (role-based access control, enforced on the server) |

## Solution layout

```
src/QuizPlatform.Core   pure functional domain library (no I/O, no UI)
  Functional/  Option, Result, Validated, Fn (curry/compose/pipe/memoize), Trampoline, Enumerables
  Domain/      Question & Answer unions, Quiz, Attempt, Flashcard, AppData, Permissions
  Logic/       Grader, Scoring, QuizSession, Analytics, Rankings, AdaptiveEngine, Sm2, parser, validator
  Content/     seed quizzes/flashcards/lessons, JSON options
src/QuizPlatform.Web    Blazor Server shell (I/O, DI, UI)
  Services/    JsonDataStore, QuizService, AccessService, AppState, LiveFeed
  Components/  Layout, Pages, Shared (reusable components)
tests/QuizPlatform.Tests  xUnit tests (unit + integration with a real temp file store)
docs/                     architecture, user guide, quiz format, QA plan, presentation, checklist
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/USER_GUIDE.md](docs/USER_GUIDE.md),
[docs/QUIZ_FORMAT.md](docs/QUIZ_FORMAT.md), [docs/TEST_PLAN.md](docs/TEST_PLAN.md),
[docs/REQUIREMENTS_CHECKLIST.md](docs/REQUIREMENTS_CHECKLIST.md) and [docs/PRESENTATION.md](docs/PRESENTATION.md).

## Where the course topics appear

| Syllabus topic | Where |
|---|---|
| Pure functions, immutability, records | `Core/Domain`, every `Logic/*` class; `with` expressions in `QuizSession` |
| Higher-order functions, lambdas, delegates | `Fn.cs`, `Option.Map/Bind`, `Result.Match`, `QuizValidator` rule lists |
| Currying, partial application, composition, pipelining | `Fn.Curry/Partial/Compose/Pipe` |
| Discriminated unions + pattern matching | `Question`, `Answer`, `Option`, `Result`, `Validated`; `Grader` tuple patterns, `Presenter` |
| Handling null / Maybe / Map vs Bind | `Option<T>`, `Parsing`, `OfNullable` |
| Exception handling in FP (Either/Result) | `Result<T>`, `Validated<T>`, `Result.Try`, `QuizService.ImportAsync` |
| LINQ, extension methods, pipelines | `Analytics`, `Rankings`, `Enumerables` (`Pairwise`, `Choose`, `Scan`, `Unfold`) |
| Lazy evaluation / infinite sequences | `Enumerables.Unfold`, deferred LINQ queries |
| Recursion and trampolining | `Trampoline<T>`, `Analytics.LongestRun` (1,000,000 elements in a test) |
| Immutable state / temporal coupling | `QuizSession` transitions, `JsonDataStore` immutable snapshots |
| Files, streams, serialization | `JsonDataStore` (atomic write), polymorphic System.Text.Json |
| Threads / race conditions | `SemaphoreSlim` in `JsonDataStore`, parallel-writers test |
| Memory management / safe practices | `IDisposable` components, event unsubscription, `CancellationToken` timers |

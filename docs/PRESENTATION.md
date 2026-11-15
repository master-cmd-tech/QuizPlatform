# Presentation outline (15–20 minutes)

## 1. Title — who, what
QuizLab: Interactive Learning and Quiz Platform · [Full names] · FP 3222 · Astana IT University.

## 2. Topic and aim
Aim: build a web platform where students learn, test and retain knowledge, and teachers publish quizzes in minutes —
using functional-programming principles in C#.

## 3. Goals and plan
Goals: (1) pure, testable domain core; (2) rich learning modes; (3) live, secure, resilient web app.
Plan (10 weeks, mirrors the course): domain model → grading/scoring → parser/validator → persistence → Blazor UI → real-time + RBAC → tests → docs → QA.

## 4. Analysis of existing technologies (comparison)
| | Kahoot | Quizlet | Google Forms | Moodle Quiz | Anki | **QuizLab** |
|---|---|---|---|---|---|---|
| Quizzes with explanations | ✔ | ✔ | limited | ✔ | – | ✔ |
| Spaced repetition | – | partial | – | – | ✔ | ✔ (SM-2) |
| Adaptive difficulty | – | – | – | plugin | – | ✔ |
| Live leaderboard | ✔ | – | – | – | – | ✔ |
| Teacher authoring | UI | UI | UI | UI | – | text DSL + validation |
| Self-hostable, open code | – | – | – | ✔ | ✔ | ✔ |
| Course integration (FP lessons) | – | – | – | manual | – | ✔ |

Technology choice (web UI): **Blazor Server** vs Blazor WebAssembly vs ASP.NET MVC vs React+API —
Blazor Server: one language (C#) end to end, instant SignalR push for the live leaderboard, no API layer, small download, server-side trust for RBAC.
Trade-off: needs a connection and holds a circuit per user (WASM/React work offline but need an API and duplicate validation).

## 5. Architecture
Show the "functional core, imperative shell" diagram from ARCHITECTURE.md. Explain the layers and why `QuizSession` is an immutable state machine.

## 6. UML / design
Class diagram of the unions (Question, Answer), sequence diagram "taking a quiz". Mention SOLID: DI of `IDataStore`/`IClock`, open for new question kinds.

## 7. Relevance / practical significance
Direct classroom use (this very course has weekly quizzes); retention research (spaced repetition, retrieval practice); lower teacher workload (quiz in plain text, auto-grading, analytics for hardest questions);
self-hosted → data stays in the university.

## 8. Methodology and mockups
Iterative: domain first (tests), UI second. Wireframes:

```
┌ Home ──────────────────────┐   ┌ Quiz ──────────────────────┐   ┌ Leaderboard ───────┐
│ Hero · 4 stat cards        │   │ ▓▓▓▓░░ Question 3 of 8  ⏱18│   │ # Player  Pts  %   │
│ [Sign in] name · role      │   │ What is a pure function?   │   │ 🥇 Ann    92  95%  │
└────────────────────────────┘   │ (A) … (B) … (C) … (D) …    │   │ 🥈 Bob    88  90%  │
                                 │ [Submit]                   │   │ ● Live             │
                                 └────────────────────────────┘   └────────────────────┘
```

## 9. Live demo (5–6 min) — rehearse this order
1. Sign in as student → take "Functional Programming Basics" (let one timer expire) → results review.
2. Open the leaderboard in a second window → finish another attempt → show it update live.
3. Flashcards: rate cards; show the schedule logic.
4. Teacher sign-in → paste an invalid quiz (all errors listed) → fix → save → play it.
5. Show student blocked from `/create`.

## 10. Functional programming in the code (show 3 snippets)
* `Grader.IsCorrect` — tuple pattern matching over two unions.
* `QuizTextParser` — Option/Result pipeline (`Bind`, `OrElse`, `Sequence`) with no exceptions.
* `Analytics.LongestRun` — tail recursion on a trampoline, tested with 1,000,000 elements.

## 11. Testing and quality
~50 automated tests (unit, file-system integration, RBAC, concurrency, corruption recovery); manual cross-browser matrix; give the real coverage number.

## 12. Challenges and solutions
Closed unions in C# (private ctor + nested records) · polymorphic JSON · concurrent writes (semaphore + atomic move) · live updates (in-process pub/sub over SignalR circuits) · not losing data on corruption.

## 13. Future work
EF Core/SQL store, Azure SignalR backplane for several servers, bUnit + Playwright tests, import/export, image questions, real authentication (ASP.NET Identity).

## 14. Conclusion / Q&A

---

# Likely defense questions (be ready to answer both of you)
1. Why records and `with` instead of classes with setters? (immutability, value equality, thread safety, easy testing)
2. What is the difference between `Option.Map` and `Option.Bind`? Show where each is used.
3. Why `Result` *and* `Validated`? (fail-fast vs accumulate)
4. How does the trampoline avoid a stack overflow? Why is the naive recursion unsafe for 1,000,000 elements?
5. What does the `private` constructor in `Option<T>` achieve?
6. How is a race condition avoided in `JsonDataStore`? What if the process crashes during a write? (temp file + atomic move)
7. How does the leaderboard update in real time? What would change with 3 servers?
8. Where is authorization enforced? Why not only in the UI?
9. Explain SM-2: what happens when you rate a card "Again"? How does the interval grow?
10. How would you add a new question type? (new sealed record + `JsonDerivedType` + a `Grader`/`Presenter`/`Validator` case + a `QuestionView` case)
11. What is deferred execution in LINQ and where did it matter? (`Unfold`, Leaderboard)
12. Which parts are pure and which are impure? Why does it matter for testing?
13. Why is prerendering disabled? Why a `PeriodicTimer`, and how is it cancelled?
14. Live-code task examples: add a new scoring rule; make `Pairwise` return a tuple; add a `Difficulty.Expert` level.

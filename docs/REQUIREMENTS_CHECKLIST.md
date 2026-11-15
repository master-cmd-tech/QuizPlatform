# Mapping to the 20 evaluation criteria (5 % each)

Honest status: **Done** = implemented in the code; **Needs your action** = cannot be proven by code alone.

| # | Criterion | Where / how | Status |
|---|---|---|---|
| 1 | Code structure & design | Core (pure) / Web (shell) split, `IDataStore` + `IClock` abstractions, single-purpose modules, see ARCHITECTURE.md | Done |
| 2 | Component reusability | `QuestionView` (parameters, `EventCallback<Answer>`, `RenderFragment Actions`), templated `DataList<TItem>`, `ProgressBar`, `StatCard`, `CountdownTimer`, `ToastHost` + cascading `ToastApi`, `StateAwareComponent` base. `QuestionView` is reused by Play and Practice | Done |
| 3 | Cross-platform | .NET 8 (Windows/macOS/Linux), Dockerfile, responsive web UI | Code done; **run it on at least two OSes/devices and tick TEST_PLAN.md** |
| 4 | State management | `AppState` scoped container with `Changed` event, `ProtectedSessionStorage` restore, cascading `ToastApi`, immutable `QuizSession` state machine | Done |
| 5 | UI design | `wwwroot/app.css`: design tokens, cards, dark mode, consistent components | Done (**review visually, adjust colours to your taste**) |
| 6 | UX | clear nav, instant feedback, skip link, ARIA, keyboard operation, edge cases (empty states, timeouts, not found) | Done |
| 7 | Responsiveness | CSS grid + media queries at 820 px and 480 px, off-canvas menu, scrollable tables | Done |
| 8 | Core functionality | quizzes, results, practice, flashcards, lessons, leaderboard, stats, teacher tools | Done |
| 9 | Advanced features | live leaderboard (SignalR circuits), role-based access control enforced server-side, SM-2, adaptive engine, text-DSL parser | Done |
| 10 | Error handling & resilience | Result/Validated, error boundary, save-retry in Play, corrupt-file quarantine, atomic writes, isolated event handlers, toast errors | Done |
| 11 | Efficiency | immutable snapshots (lock-free reads), no-prerender, `PeriodicTimer`, deferred LINQ, trampolined recursion | Done (no load test was run) |
| 12 | Scalability | `IDataStore` seam, stateless pure core, notes in ARCHITECTURE.md | Design done; **single-node by design** |
| 13 | Resource utilization | event unsubscription, `IDisposable`, cancellation of timers, no polling (push updates) | Done |
| 14 | Testing coverage | ~50 xUnit tests (unit + file-system integration + service-level) | Unit/integration done; **no UI/E2E tests** — measure coverage with coverlet and quote the real number |
| 15 | Quality assurance | TEST_PLAN.md manual matrix | **Needs your action:** actually run and fill the matrix, fix findings |
| 16 | Documentation | README, USER_GUIDE, ARCHITECTURE (Mermaid UML), QUIZ_FORMAT, TEST_PLAN | Done |
| 17 | Innovation | quiz DSL with applicative validation, SM-2 + adaptive difficulty in one platform, functional core with trampolined analytics | Done |
| 18 | Creative use of Blazor/C#/.NET | templated components, cascading values, `PeriodicTimer`, `ProtectedSessionStorage`, polymorphic System.Text.Json, records/`with`, relational + tuple patterns, LINQ query syntax over Option | Done |
| 19 | Professionalism | consistent code style, docs, Docker, health endpoint `/health`, config via appsettings | Done |
| 20 | Presentation | PRESENTATION.md (slides, notes, defense Q&A) | Done as outline; **you must prepare the slides and rehearse (15–20 min)** |

## Mandatory presentation content (from the requirements)
Team members + topic/aim/goals/plan ✔ slides 1–3 · analysis of technologies + comparison ✔ slide 4 ·
architecture/UML ✔ slides 5–6 · relevance ✔ slide 7 · methodology/mockups ✔ slide 8.

## Before you submit
1. `dotnet build` and `dotnet test` on your machine — this project was written without a compiler available, so fix any compile error first (usually a one-line fix).
2. Put both full names in README and on slide 1; every team member must be able to explain every part of the code (oral code defense, 15–20 min).
3. Rotate the default teacher code in `appsettings.json`.
4. Check the course AI policy: generative AI may be used only with the instructor's explicit permission and must be credited.

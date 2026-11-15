# Test plan

## Automated (`dotnet test`)
| Level | What | Files |
|---|---|---|
| Unit | Option/Result/Validated laws, Fn (curry, partial, compose, memoize), Enumerables (Pairwise, Choose, Scan, lazy Unfold), Trampoline (1,000,000 steps) | `FunctionalTests.cs` |
| Unit | Grader (all kinds + wrong kind + tolerance), Scoring/bonus/letter scale, QuizSession immutability, shuffle determinism, Analytics, Rankings, Adaptive engine, SM-2, parser (valid + 6 error classes), validator (error accumulation), name validation | `LogicTests.cs` |
| Integration | Seed content is valid and answerable, JSON round-trip of polymorphic types | `LogicTests.cs` |
| Integration | `JsonDataStore` on real temp files: create, persist across restart, corrupt-file recovery, 40 parallel writers, failed update keeps state | `WebServiceTests.cs` |
| Integration | `QuizService`: RBAC (student forbidden / teacher allowed), duplicate ids, event notification, flashcard scheduling per player; `AccessService`; `AppState` | `WebServiceTests.cs` |

Coverage report: `dotnet test --collect:"XPlat Code Coverage"` (coverlet is included).
Not automated: Razor UI rendering and browser end-to-end tests (see manual plan; bUnit/Playwright would be the next step).

## Manual QA checklist (fill in before the defense)
| # | Scenario | Chrome | Firefox | Edge | Safari/iOS | Android Chrome | Result |
|---|---|---|---|---|---|---|---|
| 1 | Sign in as student, invalid name rejected | | | | | | |
| 2 | Complete a quiz of each question type | | | | | | |
| 3 | Timer runs out → question scored 0 | | | | | | |
| 4 | Results page shows correct grade and review | | | | | | |
| 5 | Leaderboard updates live (2 browsers) | | | | | | |
| 6 | Flashcards: rate cards, reload, schedule kept | | | | | | |
| 7 | Teacher: wrong code rejected; create invalid quiz shows all errors; save valid quiz | | | | | | |
| 8 | Student opens /create → access denied | | | | | | |
| 9 | Corrupt `App_Data/quizlab.json` → app starts with seed data | | | | | | |
| 10 | Layout at 360 px, 768 px, 1280 px; dark mode | | | | | | |
| 11 | Keyboard-only run through a quiz | | | | | | |
| 12 | Refresh in the middle of a quiz (session is lost, no crash) | | | | | | |

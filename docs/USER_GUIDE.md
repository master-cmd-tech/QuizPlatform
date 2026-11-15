# User guide

## Students
1. **Sign in** (home page): enter a name (2-30 letters/digits). Your session is remembered in the browser tab.
2. **Quizzes** → pick a quiz → *Start*. Choose an answer and *Submit*; you immediately see whether you were right
   and why. With a timer, an unanswered question is scored 0 when time runs out. Answering several questions in a row
   earns a streak bonus.
3. **Results** show your percentage, letter grade (A … F, university scale), points, bonus, time and a review of every question.
4. **Adaptive practice**: pick a topic; difficulty adapts to you. Practice is not saved on the leaderboard.
5. **Flashcards**: reveal the answer, then rate *Again / Hard / Good / Easy*. Cards you know come back later,
   cards you forget come back tomorrow.
6. **Lessons** explain the ideas; each lesson links to quizzes on the topic.
7. **Leaderboard**: choose a quiz or "All quizzes". It refreshes live when anybody finishes a quiz.
8. **My stats**: averages, weakest topics (practise those first), progress between attempts, history.

## Teachers
Sign in with the **Teacher** role and the teacher code. Additional menu: **Create quiz** — write the quiz in the
[text format](QUIZ_FORMAT.md), *Validate*, then *Save*. On *Quizzes* you can delete quizzes; *My stats* also shows the hardest questions.
Teacher actions are checked on the server, so a student cannot perform them even by manipulating the UI.

## Accessibility and devices
Keyboard operable (Tab/Enter), skip link, visible focus, ARIA roles for answers, progress, timer and live regions,
light/dark theme following the system setting, layout from phone to desktop.

## Data
All data is stored in `src/QuizPlatform.Web/App_Data/quizlab.json` (created on first start). Delete the file to reset to the seed content.
If the file is ever corrupted it is renamed to `*.corrupt-<time>` and the app starts with fresh seed data.

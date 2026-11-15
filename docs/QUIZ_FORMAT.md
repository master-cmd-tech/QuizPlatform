# Quiz text format

Teachers create quizzes by writing plain text. Blocks are separated by a blank line; lines starting with `#` are comments.

## Header block (first block)

```
id: my-quiz            (optional, unique; generated if omitted)
title: Required title
description: Optional
category: General       (default: General)
time: 30               (seconds per question; 0 = no limit; otherwise 5-600)
```

## Question blocks

Every question starts with exactly one of `q:`, `tf:`, `sa:` or `num:`. Optional fields for all kinds:

| Field | Meaning | Default |
|---|---|---|
| `t:` | topic | General |
| `d:` | difficulty: easy / medium / hard | medium |
| `p:` | points (1-100) | 10 |
| `e:` | explanation shown after answering | empty |

### Multiple choice — `q:` + option lines (`+` correct, `-` wrong; exactly one `+`, 2-6 options)
```
q: Which keyword creates an immutable data type?
d: easy
+ record
- static
- abstract
```

### True / false — `tf:` + `a:`
```
tf: Pure functions have no side effects.
a: true
```

### Short answer — `sa:` + `a:` (alternatives separated by `|`, case/space-insensitive)
```
sa: Name the last-call optimisation.
a: tail call | tail recursion
```

### Numeric — `num:` + `a:` (+ optional `tol:` tolerance)
```
num: What is 22 / 7 (2 decimals)?
a: 3.14
tol: 0.01
```

## Validation
The editor (**Create quiz → Validate**) reports **all** problems at once (applicative validation), for example
missing title, no/many correct options, duplicate options, points out of range, invalid timer.

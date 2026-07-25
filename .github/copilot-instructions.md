# Copilot Instructions — Clean Code Standards

These instructions apply to all code written or modified in this repository.
They are based on the principles in *Clean Code: A Handbook of Agile Software
Craftsmanship* by Robert C. Martin ("Uncle Bob"), plus related refactoring and
SOLID practices. Apply them pragmatically: **readability and correctness always
win over dogmatic rule-following.**

## 1. Naming

- Use intention-revealing names. A name should answer why it exists, what it
  does, and how it is used (`elapsedTimeInDays`, not `d`).
- Avoid disinformation and noise words (`data`, `info`, `manager`, `Object`).
- Make meaningful distinctions — no `a1`, `a2`, or `ProductData` vs `ProductInfo`.
- Use pronounceable, searchable names. Single-letter names only for short loop
  scopes.
- Classes are nouns (`Customer`, `WikiPage`); methods are verbs (`postPayment`,
  `deletePage`). Use consistent vocabulary (`get`/`fetch`/`retrieve` — pick one).
- Don't encode types or scope into names (no Hungarian notation, no `m_` prefixes).

## 2. Functions

- Keep functions **small** — ideally under ~20 lines. Smaller is better.
- A function should do **one thing**, at a **single level of abstraction**.
- Prefer few arguments: 0 (niladic) or 1 (monadic) is ideal, 2 is acceptable,
  3+ needs strong justification or an argument object.
- **No flag arguments.** A boolean parameter means the function does two things —
  split it into two functions.
- Avoid output/side-effect arguments; prefer return values.
- Command-Query Separation: a function either does something or answers
  something, never both.
- Extract `try`/`catch` bodies into their own functions; error handling is one thing.
- Functions should have no hidden side effects.

## 3. Comments

- Prefer expressive code over comments. A comment often signals a failure to
  express intent in code.
- Good comments: legal headers, explanation of intent, clarification of a
  non-obvious decision, warnings of consequences, `TODO`s, public API docs.
- Bad comments: redundant restatement of code, commented-out code (delete it —
  version control remembers), misleading or outdated comments, noise.
- Comment the **why**, not the **what**.

## 4. Formatting & Structure

- Keep files short and vertically organized; related code stays close together.
- Declare variables near their first use; keep dependent functions near each other.
- Follow the language's idiomatic style guide and the existing conventions of
  this repository.
- Use an auto-formatter/linter where one exists; don't hand-fight it.

## 5. Objects, Data & Classes

- Follow the **Single Responsibility Principle** — a class has one reason to change.
- Keep classes small; measure by responsibilities, not lines.
- Hide internals; expose behavior, not data (Law of Demeter — don't reach through
  chains of objects).
- Prefer composition over inheritance.
- Keep the rest of **SOLID** in mind: Open/Closed, Liskov Substitution, Interface
  Segregation, Dependency Inversion. Depend on abstractions, inject dependencies.

## 6. Error Handling

- Prefer exceptions to returning error codes.
- **Never return `null`; never pass `null`.** Use empty collections, optionals,
  null-object patterns, or explicit result types instead.
- Provide context with each error (what failed and why).
- Fail fast with clear, actionable messages.
- Don't swallow exceptions silently; don't use exceptions for normal control flow.

## 7. DRY & Simple Design

- Eliminate duplication — extract shared logic into well-named functions/modules.
- Follow the Four Rules of Simple Design, in priority order:
  1. Passes all tests.
  2. Reveals intent (readable).
  3. No duplication.
  4. Fewest elements (no needless complexity).
- Prefer the simplest thing that works; avoid speculative generality (YAGNI).

## 8. Tests

- Treat test code as first-class: keep it as clean as production code.
- Follow **F.I.R.S.T.**: Fast, Independent, Repeatable, Self-validating, Timely.
- One assert-concept per test; a single logical assertion where practical.
- Use the Build-Operate-Check (Arrange-Act-Assert) structure.
- Name tests to describe the scenario and expected behavior.
- Keep tests deterministic — no reliance on order, wall-clock, network, or shared
  mutable state unless explicitly isolated.

## 9. Boundaries & Dependencies

- Wrap third-party APIs behind your own interfaces to limit blast radius.
- Keep boundary/adapter code thin and well-tested.
- Program to interfaces at module boundaries.

## 10. Continuous Improvement

- Follow the **Boy Scout Rule**: leave the code cleaner than you found it.
- Refactor in small, safe, verifiable steps; keep tests green throughout.
- Address code smells: long methods, large classes, long parameter lists,
  duplicated code, feature envy, primitive obsession, shotgun surgery.

---

**Guiding principle:** These are heuristics, not laws. When a rule would make the
code harder to read or maintain, favor clarity. Optimize for the next person who
reads this code.

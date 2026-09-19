# Connect 4

![Deploy](https://github.com/tzer0m/Connect4/actions/workflows/deploy.yml/badge.svg)

A Connect 4 engine that plays perfectly and always beats you when it moves first, with a console game and a simple web UI.

Connect 4 is a solved game: with perfect play the first player always wins. The computer moves first, opens in the centre column, and searches to the end of the game, so it never loses a game it starts.

## Projects

| Project | Purpose |
|---|---|
| `Connect4.Engine` | The board (`Position`), the solver (`Solver`, `TranspositionTable`), the opening book, and the engine (`Connect4Engine`) |
| `Connect4.Cli` | Play in the console, and generate the opening book |
| `Connect4.Web` | An ASP.NET Core minimal API and a single-page web UI |
| `Connect4.Tests` | NUnit tests |

## How it works

- **Board:** Two 64-bit integers hold the stones, so win detection is a handful of shifts and ANDs.
- **Solver:** Negamax with alpha-beta pruning scores a position exactly. Positive means the player to move wins (bigger is sooner), 0 is a draw, and negative is a loss. A binary search on the score plus a transposition table keeps it fast.
- **Opening book:** The first four computer moves (315 positions) are precomputed, because those are the slowest positions to solve. It's keyed by position, so any move order reaches the same entry.
- **Engine:** Uses the book if it can, takes an immediate win, then finds winning moves with a fast win/loss search and picks the fastest.

## Credit

The engine follows the design in Pascal Pons's tutorial [Solving Connect 4: how to build a perfect AI](https://blog.gamesolver.org/), rewritten in C#. It uses the same core techniques: bitboards, negamax with alpha-beta pruning, a transposition table, and centre-first move ordering.

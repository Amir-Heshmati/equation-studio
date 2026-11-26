# Equation Studio

Equation Studio is a browser-based workspace for solving and graphing linear algebra problems. Enter an equation to see the exact answer, each transformation, and a graph of both sides. Enter a linear expression to plot it as a function of x.

[Open the live demo](https://amir-heshmati.github.io/equation-studio/) · [Build and test](https://github.com/Amir-Heshmati/equation-studio/actions/workflows/ci.yml)

![Equation Studio showing the exact solution of a fractional equation and the intersection of both sides](https://amir-heshmati.github.io/equation-studio/preview.png)

## Try it

| Equation | Result |
| --- | --- |
| `2x + 5` or `y = 2x + 5` | Plots the line with slope `2` and y-intercept `(0, 5)`. |
| Paste `f(x) = 2x + 5` | Converts the function notation to `y = 2x + 5` and plots the same line. |
| `2x + 3y = 6` | Plots a line in standard form and marks both intercepts. |
| `x = 3` → **Plot as a line** | Draws the vertical line through `(3, 0)`. **Solve** still gives `x = 3`. |
| `y = 3` | Draws the horizontal line through `(0, 3)`. |
| `2x + 3 = 11` | `x = 4`. The lines meet at `(4, 11)`. |
| `3(x - 2) = x + 4` | `x = 5`. The steps show how terms move between sides. |
| `x / 3 = 1 / 2` | `x = 3/2`. The answer stays exact. |
| `2x + 1 = 2x + 2` | No solution. The lines are parallel. |
| `2(x + 1) = 2x + 2` | Infinitely many solutions. The lines overlap. |

Use **Try a harder one** to practice equations with variables on both sides, then parentheses and fractions. Each new exercise starts with its answer hidden.

## Run locally

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), open a terminal in the repository, and run:

```sh
dotnet run --project src/EquationStudio.Web --launch-profile http
```

Open the HTTP address printed in the terminal. Blazor WebAssembly runs the solver and graph in the browser. The published app can be served as static files.

## Design

| Project | Responsibility |
| --- | --- |
| `EquationStudio.Core` | Parse linear equations, perform exact rational arithmetic, produce structured steps and graph data, and generate practice equations. |
| `EquationStudio.Web` | Render the solver, graph, and practice workflow with Blazor WebAssembly. |
| `EquationStudio.Tests` | Verify exact answers, step ordering, solution types, invalid input, and exercise generation. |

The parser builds an expression tree for each side. The linear solver reduces each side to a slope and intercept, then isolates `x` using equivalent operations. Each recorded step contains its operation and the equation before and after it. The graph projects the original sides into screen coordinates.

Input supports integers, decimals such as `.5`, fractions, unary signs, and matching `()`, `[]`, or `{}`. Use explicit `2 * (x + 1)` or common implicit multiplication such as `2(x + 1)`. Multiplication and division have the same precedence and associate left to right. Use `x / (2 * 3)` to group a denominator. Division requires a constant, nonzero divisor. Persian and Arabic digits and common Unicode operator symbols are accepted.

The solver handles linear equations in `x`. The plotter also accepts linear equations in `x` and `y` and distinguishes a single line, an empty plane, and an equation satisfied by every point. Powers and products of variable terms are outside the linear input grammar.

Use **X Zoom** and **Y Zoom** to change each axis independently, or drag the axes directly. Drag the plot to pan. The axis handles stay available if you pan an axis out of view. **Reset view** restores the initial center and equal scales. With the plot focused, arrow keys pan and Shift + arrow keys scale an axis.

Open the **Math keypad** to enter numbers, `x`, `y`, and operators at the caret. It opens when the field is focused on touch devices. Typing and pasting follow the same character rules. An unsupported paste is rejected as a whole.

## Verify and publish

```sh
dotnet test
dotnet publish src/EquationStudio.Web --configuration Release
```

Serve the publish output's `wwwroot` directory with a static web host. The GitHub Actions workflow builds the site, runs the unit tests and Playwright smoke checks in desktop and touch-enabled Chromium, and captures the screenshot above from the built app. Successful `main` builds deploy to GitHub Pages.

Maintained by [Amir Heshmati](https://github.com/Amir-Heshmati).

<div align="center">
<img src="https://github.com/user-attachments/assets/ded90492-45ec-446f-a997-bea92e87c27e" alt="Fluence Logo" width="400"/>
<h1>The Fluence Programming Language</h1>
<p>
<strong>An expressive, embeddable scripting language engineered for developer ergonomics and a unique "smartass shorthand" philosophy.</strong>
</p>
<p>
<a href="https://github.com/your-repo/fluence/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="License"></a>
</p>
</div>

Fluence is a dynamically-typed, Interpreted, multi-paradigm scripting language that rejects verbosity and boilerplate. The language provides a rich suite of unique operators and constructs that enable a declarative, pipeline-oriented style.


- [Core Philosophy](#core-philosophy)
- [Performance](#performance)
- [Language Tour](#language-tour)
  - [Fundamentals](#fundamentals)
  - [Functions](#functions)
  - [Control Flow](#control-flow)
  - [Data Structures & Object Model](#data-structures--object-model)
  - [Namespaces & Modules](#namespaces--modules)
- [The Soul of Fluence: The Operator Suite](#the-soul-of-fluence-the-operator-suite)
  - [Pipeline Operators](#pipeline-operators)
  - [Advanced Assignment Operators](#advanced-assignment-operators)
  - [Collective Comparison Operators](#collective-comparison-operators)
  - [The "Dot Family" Operators](#the-dot-family-operators)
  - [Syntactic Sugar & QoL](#syntactic-sugar--qol)
- [Roadmap](#roadmap)
- [Examples](#examples)

## Performance

Fluence is designed for high performance. The interpreter features a multi-stage pipeline that includes:
1.  **Optimizer:** A crucial peephole optimization pass that fuses common instruction patterns to reduce instruction count.
2.  **Virtual Machine (VM):** A highly optimized, dispatch-table based VM that executes the bytecode.
---

## Core Philosophy

Fluence is guided by a simple principle: to express complex logic with minimum friction.

-   **Versatility:** Most constructs, including `if`, `match`, and even `loop`, can be used as expressions to produce a value.
-   **Readability:** Data flows naturally from left to right through a series of transformative pipes, minimizing intermediate variables and enhancing readability.
-   **Compactness:** The language comes with a vast array of shorthand operators, leading to a higher code density and ease of navigation.
-   **Embeddable:** Built to be a powerful scripting engine.

## Language Tour

### Fundamentals


-   **Variable Declaration:** Implicit. Variables are created upon their first assignment.
    ```cs
    my_var = 10;
    ```
    
-  #### Variables & The `solid` Keyword
   The `solid` keyword creates a **readonly** binding.
  
    ```cs
    # A mutable variable.
    my_var = 10;
    my_var = 20; // This is fine.
    
    # A readonly (immutable) variable.
    solid immutable_var = "Hello";
    immutable_var = "World"; # This will cause a compile-time or runtime error.
    ```
-   **Comments:** Single-line (`#`) and multi-line, nestable (`#* ... *#`).
-   **Strings:** Simple (`"..."`) and formatted (`f"..."`) strings with `{expression}` interpolation.
    ```cs
    world = "World";
    greeting = f"Hello, {world}!";
    ```
-   **Ranges:** Inclusive ranges defined by `start..end` (e.g., `1..10`). If used outside a `for .. in ..` loop, the range operator returns a list of the elements from a to b.

#### Data Types
Fluence is dynamically typed. The core types include:
-   **Numbers:** Integers, doubles, long and floating-point numbers (`10`, `3.14`, `3.14f`).
-   **Strings:** Simple (`"..."`) and formatted (`f"..."`) strings with `{expression}` interpolation.
-   **Chars:** Simple (`'c'`) characters.
-   **Booleans:** `true` and `false`.
-   **Nil:** A null-like value.
-   **Lists:** There are no arrays in Fluence, only lists.
-   **Structs:** User defined and intrinsic.
-   **Enums:** Simple C-style enumerations.
-   **Ranges:** Inclusive ranges defined by `start..end` (e.g., `1..10`). When used in a `for n in a...b` loop, a special iterator object is created. Otherwise the range returns a list of values from a to b.

### Functions

Functions are first-class citizens.
-   **Single-Expression Functions:** The `=>` arrow provides an implicit return.
    ```cs
    func square(x) => x ** 2;
    ```
-   **Block-Body Functions:** Use `{}` for more complex logic. `return` is explicit; otherwise, the function implicitly returns `nil`.
    ```cs
    func get_status(code) => {
        if code == 200 -> return "OK";
        return "Unknown";
    }
    ```
Lambdas are yet to be implemented.

### Control Flow

All control structures support a single-line `->` syntax for simple bodies.
-   **`if`/`else if`/`else`:** Standard conditional branching.
-   **`unless`:** The inverse of `if`. `unless condition` is equivalent to `if not condition`.
    ```cs
    unless user.is_authenticated -> redirectToLoginPage();
    ```
-   **Loops:** A comprehensive suite for any use case.
    -   `for item in collection -> ...`
    -   `while condition -> ...`
    -   `loop { ... if condition -> break; }` A special infinite loop, until it encounters a `break` statement.
    -   `for i = 0; i < N; i++ -> ...`
    -   **Natural Language Loop:** A way to loop a fixed number of times.
        ```cs
        5 times { printl("Hello!"); }
        N = 10;
        N times as i { create_enemy(i * 20); }
        ```
        
#### `match` — Versatile Pattern Matching
The `match` statement is a flexible tool for handling complex conditional logic based on a value's identity. It can be used as a traditional statement (like a `switch`) or as an expression that returns a value.

*   **Expression-Style `match` (`->`)**

    This is the most common form. It's an expression that evaluates to the value of the first matching case. It must be exhaustive, meaning a `rest` (default) case is required if all other possibilities aren't covered.

    ```cs
    # Before: A clunky if/else if/else chain.
    icon = "";
    if tile == Tile.Hit {
        icon = "X";
    } else if tile == Tile.Miss {
        icon = "0";
    } else {
        icon = "?";
    }
    row_str += f" {icon} ";

    # After: A clean, single match expression.
    row_str += f" {match tile {
                   Tile.Hit -> "X";
                   Tile.Miss -> "0";
                   rest -> "?";
               }} ";
    ```

*   **Statement-Style `match` (`:`)**

    For more complex logic, you can use a colon (`:`) to define a block of statements for each case. This form does **not** return a value and supports fallthrough and `break`.

    ```cs
    # A statement-style match for handling different command types.
    match command.type {
        Command.MOVE:
            handle_move(command.payload);
            # Implicit fallthrough to the next case.
        Command.UPDATE_UI:
            ui.needs_redraw = true;
            break; # Exit the match block.
        Command.QUIT:
            game.is_running = false;
            break;
        rest:
            log_unknown_command(command);
    }
    ```

*   **Block Bodies in `match` Expressions (`=>`)**

    You can combine the power of an expression with multi-line logic. A case with a block arrow (`=>`) can contain multiple statements, but the block **must** end with a `return` to provide a value for the `match` expression.

    ```cs
    result_message = match player_grid.shoot(shot_point) {
        Tile.Ship => {
            player_grid.update_tile(shot_point, Tile.Hit);
            game.score += 100;
            return ">>> HIT! <<<"; # This block returns a value.
        },
        rest -> ">>> Miss. <<<";
    };
    printl(result_message);
    ```

  
### Data Structures & Object Model

-   **Structs:** Simple data aggregates with instance fields, static fields, methods, and a special `init` constructor. `self` is used to refer to the instance.
    ```cs
    struct Vector2 {
        # Instance fields with default values.
        x = 0; 
        y = 0;
        z; # A field with no default value, defaults to nil on initialization.
        
        # A static, readonly field.
        solid ORIGIN = Point(0, 0); 
        
        # Constructor.
        func init(x, y) => { self.x, self.y <~| x, y; }
        
        # Instance Method.
        func length() => (self.x**2 + self.y**2)**0.5;

        func from_angle(angle, magnitude) => { ... }
    }
    v = Vector2(3, 4);      # Constructor call.
    p = Point{x: 10, y: 20};  # Direct initializer.
    origin = Vector2.ORIGIN;  # Accessing a static field.
    ```
-   **Enums:** Simple, C-style enumerations for named constants.
    ```cs
    enum Status { Pending, Complete, Failed, }
    task_status = Status.Pending;
    ```

### Namespaces & Modules
Organize code with `space` and import with `use`. The `use` keyword can import multiple namespaces at once.
```cs
space MyGame {
    use FluenceIO, FluenceMath;
    # or
    use FluenceIO;
    use FluenceMath;
    ...
}
```

---


## Operators

Operators can be considered Fluence its most distinct features. The operators are designed with ease of use and readability in mind.

### Pipeline Operators
These operators create a linear, left-to-right flow of data, eliminating nested calls.

| Operator | Name | Description | Example |
| :--- | :--- | :--- | :--- |
| **`\|>`** | Pipe | Pipes the LHS result into an argument of the RHS function, marked by `_`. Implicit for single-argument functions. | `input() \|> trim(_) \|> to_upper(_)` | The pipe must be followed by an `_` in the function you pipe into.
| **`\|?`** | Optional Pipe |  If LHS is `nil`, the chain stops and returns `nil`. | `user \|? _.get_profile() \|? _.name` |
| **`\|??`**| Guard Pipe | If the current value is `false`, the chain stops and returns `false`. | `is_valid \|?? check_a() \|?? check_b()` |
| **`\|>>`** | Map Pipe | Transforms each element of a list into a new list. | `[1,2,3] \|>> _ * 2` -> `[2,4,6]` |
| **`\|>>=`**| Reducer Pipe | Reduces a list to a single value. Takes an `(initial, (acc, el) => ...)` lambda. | `[1,2,3] \|>>= (0, (s, n) => s + n)` -> `6` |
| **`\|~>`** | Scan Pipe | Reduces a list but returns all intermediate results. Takes `(count, (el) => ...)` lambda. | `1 \|~> (4, (x) => x * 2)` -> `[1,2,4,8,16]` |
| **`~>`** | Composition Pipe| Composes two functions into a new one. `f = g ~> h` is `(x) => h(g(x))`. | `add_one_sq = (_+1) ~> (_**2)` |

As of now, the following are not yet implemented: Scan Pipe, Composition pipe, reducer pipe, map pipe, optional pipe.


### Advanced Assignment Operators
Elevates assignment from a simple statement to a powerful expression tool for reducing boilerplate.

| Operator | Name | Description | Example |
| :--- | :--- | :--- | :--- |
| **`<~\|`** | Sequential Assign | Assigns `n` distinct values to `n` variables. | `x, y, z <~\| 10, "hello", true` |
| **`<\|`** | Rest Assign | Assigns a single value to all remaining variables in the list. | `x, y, z <\| 0` |
| **`<?\|`** | Optional Rest Assign | Assigns the RHS to the rest of the variables, but only if the RHS is not `nil`. | `theme <?\| read_config()` |
| **`<n\|`** | Chain N Assign | Assigns the same value to the next `n` variables in the chain. | `x,y,z <2\| 0 <\| "+"` -> `x=0, y=0, z="+"` |
| **`<n!\|`** | Unique Chain Assign | Evaluates the RHS expression `n` times, assigning each unique result to the next `n` variables. | `a,b <2!\| input()` -> `a=input(), b=input()` |
| **`<??\|`** | Guard Chain (AND) | Assigns `true` to a variable if a chain of comma-separated expressions are all truthy. Short-circuits. | `is_valid <??\| check1, check2` |
| **`<\|\|??\|`**| Guard Chain (OR) | Assigns `true` to a variable if any expression in a chain is truthy. Short-circuits. | `has_flag <\|\|??\| cond1, cond2` |

### Collective Comparison Operators
Check a condition against multiple variables at once, eliminating long `&&` or `||` chains.

| Operator | Name | Description | Example |
| :--- | :--- | :--- | :--- |
| **`<==\|`** | Collective AND | Returns `true` if all variables on the left meet the condition. | `if x, y <>\| 5` (if x>5 AND y>5) |
| **`<\|\|==\|`**| Collective OR | Returns `true` if any variable on the left meets the condition. | `if x, y <\|\|==\| nil` (if x is nil OR y is nil) |
*(Variants exist for `!=`, `<`, `>`, `<=`, `>=` by changing the middle symbol, e.g., `<>=\|`, `<\|\|<\|`)*


### The "Dot Family" Operators
These operators provide function-style syntax for common operations.

| Operator | Name | Description | Example |
| :--- | :--- | :--- | :--- |
| **`.and()`** | And Function | Function-call syntax for `&&`, useful for long, comma-separated lists of conditions. | `if .and(c1, c2, c3)` |
| **`.or()`** | Or Function | Function-call syntax for `\|\|`. | `if .or(c1, c2, c3)` |
| **`.++()`** | Multi-Increment | Increments multiple variables. | `.++(x, y)` |
| **`.--()`** | Multi-Decrement | Decrements multiple variables. | `.--(x, y)` |
| **`.-=`** | Multi-Op-Assign | Applies an op-assignment to multiple variables with corresponding values. | `a,b .+= 5,10` | 
*(Variants exist for `+`, `-`, `/`, `*`, `%`, by changing the middle symbol, e.g., `.-=`, `.*=`)*



### Syntactic Sugar & QoL

| Operator | Name | Description | Example |
| :--- | :--- | :--- | :--- |
| **`->>`** | Train Operator | Chains single-line statements sequentially without needing a block. Must end with a `<<-`. | `->> a=5 ->> b=10 ->> printl(a+b) <<-` |
| **`><`** | Swap | Swaps two variables in-place. | `a >< b;` |
| **`!!`** | Boolean Flip | Toggles a boolean value. | `is_active!!;` |
| **`?:`** | Ternary Operator | A concise `if/else` expression. | `msg = ok ?: "Success", "Fail";` |
| **`is`/`not`** | Aliases | `is` is an alias for `==`. `not` is an alias for `!=`. | `if x is 5 and y not 10` |


### Building from Source
Fluence is built on .NET. To create a command-line executable for your platform:
```sh
# For Windows
dotnet publish -c Release -r win-x64 --self-contained true
```
This creates a `publish` directory containing `fluence.exe`. Add this directory to your system's PATH to make the `fluence` command available everywhere.

### Running a Script
Once the command is in your PATH, you can run scripts from any terminal:
```sh
fluence -run my_script.fl
```

# Roadmap
Fluence is still in its early stages of development. Many features are yet to be implemented.

-   **Enhanced Type System:**
    -   **Access Modifiers:** Introducing `private` and `public` keywords for fields and methods to provide robust encapsulation.
    -   **Traits (`impl`):** Finalizing the trait-based system for structs (`struct Vector2 impl Vector`) to enable a powerful form of polymorphism and interface-driven design.

-   **Richer Functions & Lambdas:**
    -   **Full Lambda Implementation:** Completing the implementation of first-class lambda expressions.
    -   **Pipe-Enabled Lambdas:** Implementing the full suite of functional pipeline operators that operate on lambdas, including `|?` (Optional Pipe), `|>>` (Map Pipe), `|>>=` (Reducer Pipe), and `|~>` (Scan Pipe).
    -   **Default Argument Values:** Allowing functions and methods to have default values for parameters, e.g., `func connect(host, port=8080) => ...`.
    -   **Multiple Constructors:** Enabling structs to have multiple `init` methods with different arities for more flexible object creation.

-   **Advanced Control Flow:**
    -   **`.returnmatch` Statement:** Implementing a powerful, single-statement conditional return mechanism to further reduce `if/else` boilerplate in functions.

-   **Standard Library Expansion:**
    -   **New Libraries:** Creating new intrinsic modules for common scripting needs, such as `FluenceHttp` (for web requests), `FluenceData` (for CSV/JSON parsing), and `FluenceOS` (for interacting with the operating system) and many others.


---


# Examples:

## A simple calculator in Fluence:
```cs
use FluenceIO;

func Main() => {
    num1, num2, op <2!| to_int(input()) <| input();

    if num1, num2, op <!=| nil ->
        ->> result = match op {
            "+" -> num1 + num2;
            "-" -> num1 - num2;
            "*" -> num1 * num2;
            "/" -> num2 == 0 ? nil : num1 / num2;
            rest -> nil;
        } ->> print(result is nil ?: "Error: Invalid operation or division by zero.", f"Result: {result}") <<-;
    else -> print("Error: Invalid input, one or more arguments were null.");
}
```

## Collatz Conjecture
```cs
use FluenceIO;

func Collatz() => {
    max_len, num_with_max_len, limit <2| 0 <| 100000;

    for n in 1..limit {
        len, term <~| 1, n;
        while term != 1 {
            if term % 2 == 0 -> term /= 2;
            else -> term = term * 3 + 1;
            len += 1;
        }
        if len > max_len -> max_len, num_with_max_len <~| len, n;
    }
    return num_with_max_len; 
}

func Main() => printl(Collatz());
```















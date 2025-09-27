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

Fluence is a dynamically-typed, interpreted, multi-paradigm scripting language that rejects verbosity and boilerplate. It provides a rich suite of unique operators and constructs that enable a declarative, pipeline-oriented style. Designed for embedding in applications or standalone scripting, Fluence prioritizes concise syntax, powerful control flow, and ergonomic features to boost developer productivity.

## Table of Contents

- [Language Fundamentals](#language-fundamentals)
  - [Variables](#variables)
  - [Comments](#comments)
  - [Built-in Types](#built-in-types)
- [Functions](#functions)
  - [Reference Arguments](#reference-arguments)
- [Control Flow](#control-flow)
  - [If and Unless](#if-and-unless)
  - [Loops](#loops)
  - [Match](#match)
- [Data Structures](#data-structures)
  - [Structs](#structs)
  - [Enums](#enums)
  - [Namespaces & Modules](#namespaces--modules)
- [Operators](#operators)
  - [Basic Operators](#basic-operators)
  - [Ternary Operator](#ternary-operator)
  - [DOT Family Operators](#dot-family-operators)
  - [Miscellaneous Operators](#miscellaneous-operators)
  - [Train Operator](#train-operator)
  - [Collective Comparison Operators](#collective-comparison-operators)
  - [Advanced Assignment Operators](#advanced-assignment-operators)
  - [Broadcast Call](#broadcast-call)
  - [Pipe Operator](#pipe-operator)
- [Lambdas](#lambdas)
  - [Lambda Pipes](#lambda-pipes)
- [Examples](#examples)
- [Contributing](#contributing)
- [Roadmap](#roadmap)
- [License](#license)
- [Acknowledgments](#acknowledgments)



## Language Fundamentals
### Variables

Implicit. Variables are created upon their first assignment.
```cs
my_var = 10;
```
If uninitialized, variables default to 'Nil', the null in Fluence.
```cs
my_var;
printl(my_var); # prints 'nil'
```

**Read only variables**

Declared using the 'solid' keyword, the variable will be set as read only.
```
solid my_var = 10;
my_var = 1; # error.
```

### Comments

Single line comments with '#'
```cs
my_var = 5; # some comment.
```

Multi-line comments with '#*' -> '*#'.
```cs
my_var = 5;
#* some comment line1
line2
line3
*#
```

### Built-in Types

Fluence supports: Int, Double, Float and Long numeric types.
```rust
int = 5;
double = 0.5;
double2 = .5; # 0 can be omitted.
float = 0.5f;

int2 = 1_000_000; # Underscores can be used in numbers as a cosmetic separator.

my_long = 9_223_372_036_854_775_807;

scientific = 1.23e4; # scientific notation is also supported.
```

Strings:
```rust
str = "Hello World!";
```
F-Strings:
```cs
str1 = "Hello";
str2 = " World!"
printl(f"{str1}{str2}"); # prints 'Hello World!'.
```

Lists:
```rust
list = [....];

list2 = [1..5]; # [1, 2, 3, 4, 5]
```

Ranges:
Ranges are defined as \[Start\]..\[End\] where both start and end are inclusive. They support both numbers and variables, and expressions.
```rust
1..5; # 1,2,3,4,5

a = 1;
a..5; # 1,2,3,4,5

b = 5;

a..b; # 1,2,3,4,5
```

Chars and Booleans:
```rust
a = 'a';
truthy = true;
falsy = false;
```

Nil:
The representation of null in Fluence;
```rust
my_var = nil;
```

## Functions
Functions are first-class citizens. All functions start with the 'func' keyword and come in two formats.

```rust
# Expression bodied, returns the value result of an expression.
func Square(a) => a ** 2; # since it expects an expression, it must end with a semicolon.

# A block bodied function.
func Square(a) => {
  result = a ** 2;
  return result;
}
```

The entry point to the script or application is defined as:
```rust
func Main() => {
  ...
}

# or, a one liner.
func Main() => ....;
```

Block bodied functions must define an explicit 'return expr' to return a value, otherwise they will return 'nil' by default.
When calling a function you always pass arguments by value, this means that any change to them does not affect them outside the function.

### Reference Arguments
To pass an argument by reference you may use the 'ref' keyword.
```rust
func ByRef(a, ref b) => {
  b += 5;
  return a + b;
}

func Main() => {
  a = 1;
  b = 2;
  printl(b); # prints 2

  # to pass by ref, you must use the ref keyword. 
  printl(ByRef(a, ref b)); # prints 8.
  printl(b); # prints 7.
}
```

## Control Flow
Control structures in Fluence do not require parentheses, for the most part.

### If and Unless
Standard conditional branching. 
```rust
my_var = 5;

# A single line statement.
if my_var == 5 -> printl(my_var);

# Block body.
if my_var == 5 {
  printl(my_var);
}

# Both types can be used together.
if a == 5 -> printl(5);
else if a == 6 -> printl(6);
else {
  printl(a);
}
```

**Unless** is the inverse of `If`, does not have an else unless/else.

```rust
truthy = true;

# Unless the condition is true, do something, neither of these will print anything.
unless truthy -> printl("False");

unless truthy {
  printl("False");
}
```

### Loops

- **For-In**:
Represents a loop over a list or a range. Supports both single line and block bodies.
Format is: for `variable` in `expression/list/range`

```rust
list = [1..5];

# Both of these will print 1 to 5 inclusively.

for i in list -> printl(i);

for i in 1..5 {
  printl(i);
}

```

- **While/Until**:
A simple while loop.

```rust
while true -> ....

while true {
  ...
}
```

The inverse of `While`
Until condition -> do something

```rust
a = 0;

until a > 5 {
  a++;
  printl(a);
}

# Single line is supported
until cond -> ....
```

- **C-Style For**:
Supports both single line and block body, expects 3 expressions separated with a semicolon.

```rust
for i = 0; i < 10; i++; -> printl(i);

for i = 0; i < 10; i++; {
    printl(i);
}
```

- **Loop** (Infinite):
Represents an infinite loop which can be exited only using a `break` statement.
Does not support a single line form.

```rust
a = 0;

loop {
  if a >= 10 -> break;
  a++;
  printl(a);
}
```

- **Times**:
A fancy way to loop a certain amount of times.
Supports one line and block bodies.
Accepts only integer numbers and integer variables.

```rust

5 times -> printl(1); # prints '1' five times.
5 times {
  printl(1);
}

my_var = 10;

my_var times -> ...
```

`Times As` loops.
A slightly expanded way to do a `Times` loop.
Supports one line and block bodies.

```rust
# defines a variable i as 0, increments until it reaches 5 ( exclusive ), aka 0,1,2,3,4.
5 times as i -> printl(i);
```
By default this variable defined is mutable, you can modify it inside the loop but be careful or you will get an infinite loop.
You can mark it as solid this way:
```rust
5 times as solid i {
  printl(i);
  i += 1; # error
}
```

### Match
The `match` statement is a flexible tool for handling complex conditional logic based on a value's identity. It can be used as a traditional statement (like a `switch`) or as an expression that returns a value.

**Expression-Style**
This is the most common form. It's an expression that evaluates to the value of the first matching case. It must be exhaustive, meaning a `rest` (default) case is required if all other possibilities aren't covered.

```rust
# Before: A clunky if/else if/else chain.

icon = "";
if tile == Tile.Hit {
    icon = "X";
} else if tile == Tile.Miss {
    icon = "0";
} else {
    icon = "?";
}

# After: A clean, single match expression.
row_str += match tile
    {
        Tile.Hit -> "X";
        Tile.Miss -> "0";
        rest -> "?";
    }; # Since this form of match is an expression, it expects a semicolon.
```

 **Statement-Style `match` (`:`)**

  For more complex logic, you can use a colon (`:`) to define a block of statements for each case. This form does **not** return a value and supports fallthrough and `break`.
  This is basically a switch statement. Rest statement must end with a `break;`.

  ```rust
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
          break;
  }
  ```
NOTE! That a switch style match does not require a semicolon after its closing brace!

 **Block Bodies in `match` Expressions (`=>`)**

  You can combine the power of an expression with multi-line logic. A case with a block arrow (`=>`) can contain multiple statements, but the block **must** end with a `return` to provide a value for the `match` expression.

  ```rust
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

## Data Structures

### Structs
Simple data aggregates with instance fields, static fields, methods, and a special `init` constructor. `self` is used to refer to the instance.
A field marked as `solid` is both readonly and static.

```rust
    struct Point {
        x,y;
        func init(x,y) => self.x,self.y <~| x,y; # will be explained later.
    }

    struct Vector2 {
        # Instance fields with default values.
        x = 0; 
        y = 0;
        z; # A field with no default value, defaults to nil on initialization.
        
        # A static, readonly field.
        solid ORIGIN = Point(0, 0); 
        
        # Constructor.
        func init(x, y) => {
          self.x = x;
          self.y = y;
        }
        
        # Instance Method.
        func length() => (self.x**2 + self.y**2)**0.5;

        # Static function, can not have 'self' inside.
        func length(x,y) => (x**2 + y**2) ** 0.5;
    }

  func Main() => {
    
    pos1 = Vector2(1,2);
    printl(pos1.length()); # 2.236....

    # Static functions are called with the struct name.
    printl(Vector2.length(1,2)); # 2.236....

  }
```

There are two ways to create a new instance. Constructor and Direct.
```rust
# constructor, requires a constructor with, in this case, 2 arguments.
vector = Vector2(a,b); 

# direct, Required fields by name, and their value after the colon, with a comma separator.
vector = Vector2 { x:1, y:2 };
```

### Enums
Simple C-style enumerations.

```rust
    enum Tile {
        Empty,
        Ship,
        Hit,
        Miss,
    }
```
All enum members end with a comma.

```rust
a = Tile.Hit;

if a == Tile.Hit -> printl("Hit!"); # true, prints.
```

### Namespaces & Modules
Organize code with `space` keyword and import with `use`. The `use` keyword can import multiple namespaces at once.
```cs
space MyGame {
    use FluenceIO, FluenceMath;
    # or
    use FluenceIO;
    use FluenceMath;
    ...
}
```

Any code outside any space automatically belongs to the global namespace!

## Operators
Operators can be considered the most distinct feature of Flunce. The operators are designed with ease of use and readability in mind.

### Basic Operators
Starting Simple:
```rust
# generic operators
+ - * / %

** (power aka exponentiation)

# Decrement, Increment
-- ++

# Bitwise operators
<< (bitwise left shift)
>> (bitwise right shift)
& (AND)
| (OR)
^ (XOR)
~ (NOT)

# Operator + assignment
+= -= /= *= %=

# Bitwise + assignment
&=

# Unary negation
-10

# Comparison
== != < > <= >=

# Logical
&& ||
! ( !true, !false )
is ( alias of == )
not ( alias of != )
```

### Ternary Operator
Supports two formats
```rust
# Standard ternary.
a = true;

b = a ? 1 : 2;

# A more Fluence style
b = a ?: 1, 2;
```

### DOT Family Operators
A special set of operators that begin with a dot.

#### `.and()` and `.or()`
```rust
# Logical
.or()  .and()
```
These represent a grouping of conditions with `&&` for .`and()` and `||` for `.or()`.
```rust
a = true;
b = true;

# instead of
if a && b -> ...
# you can do
if .and(a,b) -> ....

# Same for .or()
```

#### `.++()` and `.--()`
These represent a grouping of increments, decrements.
```rust
a = 1;
b = 1;

.++(a,b);
printl(f"{a},{b}"); # prints 2 for both.

.--(a,b);

printl(f"{a},{b}"); # prints 1 for both.
```

#### `.op=`
These represent a grouping of an operator + assignment.
Only applicable for - + / *

```rust
.-= .+= ./= .*=
```

```rust
a = 1;
b = 1;

a += 4;
b += 4;

printl(f"{a},{b}"); # prints 5 for both.a

a, b .-= 4,4;

printl(f"{a},{b}"); # prints 1 for both.a
```

### Miscellaneous Operators
A small set of small, special operators.

#### `!!` - Boolean flip
```rust
a = true;
a!!;
printl(a); # prints false;
```

#### `><` - Swap operator
```rust
a = 1;
b = 2;
a >< b; 
printl(a); # prints 2;
printl(b); # prints 1;
```

### Train Operator
Allows the chaining of expressions and statements without a semicolon.
Starts with `->>` and must end with `<<-`

```rust
func Main() => {
  ->> a = 5 
      ->>
      b = 10
      ->> printl(a + b) <<- # prints 15.
}
```

### Collective Comparison Operators
These check a condition against multiple variables at once, eliminating long `&&` or `||` chains.
```rust
a = 1;
b = 3;

# This reads as such, "if both 'a' and 'b' are smaller than 10 then".
if a, b <<| 10 -> printl(...);
```
The following collective comparison operators are supported
```rust
<==| # all equal to
<!=| # all not equal to
<<|  # all smaller than
<<=| # all smaller or equal to
<>|  # all greater than
<>=| # all greater or equal to
```

Collective comparison operators also support OR variants
```rust
a = 1;
b = 3;
c = 5;

# This reads as such, "if either 'a' or 'b' or 'c' are smaller than 10 then".
if a, b, c <||<| 10 -> printl(...);
```
The following OR collective comparison operators are supported, simply add `||` to the standard ones.
```rust
<||==| # any equal to
<||!=| # any not equal to
<||<|  # any smaller than
<||<=| # any smaller or equal to
<||>|  # any greater than
<||>=| # any greater or equal to
```

### Advanced Assignment Operators
These elevate assignments from a simple statement to a powerful expression tool for reducing boilerplate.

### Sequential Rest Assignment `<~|`
Sequentially assign `N` distinct values to `N` variables.
```rust
a, b, c <~| 1, 2, 3; # a becomes 1, b becomes 2, c becomes 3.
```

#### Optional Sequential Rest Assignment `<~?|`
Sequentially assign `N` distinct values to `N` variables, but only if those values are not `nil`.
```rust
b = 5;
a, b, c <~?| 1, nil, 3; # a becomes 1, b stays as 5, because we attempt to assign 'nil' to it, c becomes 3.
printl(f"{a},{b},{c}");
```

### Chain N Assignment `<N|`
Assigns the same value to the next `N` variables in the chain.
```rust
a, b, c <2| 1 <1| 2;
printl(f"{a},{b},{c}"); # a and b are '1', c is '2'.
```
To avoid using `<1|` Chain N Assignment is often paired with:
### Rest Assignment `<|`
Assigns a single value to all remaining variables on the left.
```rust
a, b, c <1| 1 <| 2;
printl(f"{a},{b},{c}"); # a is '1', b and c become '2'.
```
NOTE! That not all special assignment operators can be used together!

Both Chain N Assignment and Rest Assignment operators support an Optional variant: `<N?|` and `<?|`.
These work identically to Optional Sequential Rest Assignment.

### Unique Chain N Assignment `<N!|`
Works identical to `<N|` But evaluates the values for each variable.
For example:
```rust
# To avoid num1, num2 being assigned to the same value, we use Unique Chain N assignment
# and not Chain N Assignment.
num1, num2, op <2!| to_int(input()) <| input();
```

#### Optional Unique Chain N Assignment `<N!?|`
Works identically to all other Optional variants.

### Guard Chain `<??|`
Assigns `true` to a variable if a chain of comma-separated expressions are all truthy. Short-circuits.
```rust
a, b, c <~| 1,2,3;

truthy <??| a < b, b < c, c < 10;

printl(truthy); # true.

```

#### OR Guard Chain `<||??|`
Assigns `true` to a variable if in a chain of comma-separated expressions at least one is truthy. Short-circuits.
```rust
a, b, c <~| 1,2,3;

truthy <||??| a < b, b > c, c > 10;

printl(truthy); # true.
```
### Guard Pipe Truthy-propagating. `|??`
An alternative to the Guard Chain. If the right-side expression is false, the pipeline breaks and returns false.
```rust
a, b, c <~| 1,2,3;

truthy |?? a < b
       |?? b < c
       |?? c < 10;

printl(truthy); # true.
```

### Broadcast Call `<|`
An overload of the rest assignment operator, pipes multiple values into a function.
```rust

# The underscore is a special character, tells which argument is the value we are passing.
printl(_) <| 1, 2, 3, "Hello World!";

# prints
1
2
3
Hello World!
```

### The Pipe Operator `|>`
Pipes a value into a chain of function calls. "Take the result of the expression on the left and feed it into the function on the right."
There are to options when using the pipe:
```rust
# This calls a function that belongs to the type on the left. There is no need to write the variable name in this case.
... |> .func()

# This passes the value on the left to the spot defined by '_' of the function on the right,
# if further pipes are present we pass the value of the, in this case 'printl' to the next pipe.
... |> printl(_)
```
Examples:
```rust
text = "   hello";
result = text 
    |> .trim() 
    |> .upper() 
    |> .sub(2);

printl(result); # Prints 'LLO'.
```
This is also valid
```rust
text = "   hello";
# If a structs function returns the struct itself, you can chain it like this.
result = text.trim().upper().sub(2);
printl(result); # prints 'LLO'.
```
For this reason usually it is logical to use the pipe operator when also using its second form of use.

Explicit Placeholder for Multi-Argument Functions.
If the function on the right takes more than one argument, the _ placeholder is mandatory. It tells Fluence exactly where to insert the piped-in value.
```rust
func multiply_and_add(a, b, c) => a * b + c;
 
func Main() => {
    # This becomes multiply_and_add(5, 10, 2) = 52
    result = 10 |> multiply_and_add(5, _, 2);
    printl(result);
}
```
```rust
func subtract(a, b) => a - b;
 
func Main() => {
    result1 = 20 |> subtract(100, _); 
    result2 = 20 |> subtract(_, 100);  

    printl(f"{result1},{result2}"); # 80, -80
}
```

## Lambdas
Lambdas are defined as follows:
```rust
lambda = () => ...
```
A lambda can accept any number of arguments, and since it is a function it can come in a single line expression, or a block body.
```rust
lambda = (a,b) => a + b;

lambda = (a,b) => {
  result = a + b;
  return result;
}
```
Since lambdas are functions, if they have a block body they must explicitely return a value using the `return` statement, otherwise they will return `nil`.

```rust
    add = (a,b) => a + b;
    printl(add(1,2)); # 3

    add = (a,b) => {
        a + b;
    }

    printl(add(1,2)); # nil
```

Lambdas can accept arguments passed by reference.
```rust
    add = (ref a, b) => {
        a += 4;
        return a + b;
    }

    a, b <~| 1, 2;

    printl(add(ref a,b)); # 6
    printl(a); # 5
```

### Lambda Pipes
The following are pipe like operators that use lambdas.

### Reducer Pipe `|>>=`
Iterates over a collection, applying a function at each step to accumulate a final result.
```rust
numbers = [5, 2, 8];
sum = numbers |>>= (0, (total, n) => total + n);
printl(sum); # 15
```
How it Works in Fluence: The (initial_value, lambda) Pair

The reducer pipe always takes two arguments on its right-hand side, enclosed in parentheses: (initial_value, lambda).

  initial_value: This is the starting value for your accumulation. It's what the accumulator will be on the first iteration. This value must be a compile time constant, f.e empty list `[]` or `0` or `""`. Not a variable.

  lambda: This is the function that performs the work at each step. It must take two arguments:
      The Accumulator: The current accumulated value (e.g., total).
      The Current Element: The current item from the collection being iterated over (e.g., n).
      The lambda must return the new value for the accumulator, which will be passed into the next iteration.
        
Modified example with block body.
```rust
  numbers = [5, 2, 8];
  sum = numbers |>>= (0, (total, n) => {
      n++;
      return total + n;
  });
  printl(sum);
```

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

## Examples

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















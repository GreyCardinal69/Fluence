# Benchmarks

This document contains performance metrics for the Fluence programming language (v0.1.1).

**Environment:**
*   **OS:** Windows 11 (10.0.26100)
*   **Runtime:** .NET 8.0.20 (X64 RyuJIT AVX2)
*   **Measurement Tool:** BenchmarkDotNet

---

## 1. Logic & Arithmetic Throughput
**Test:** Calculate `a + b` inside a loop iterating 1,000,000,000 (1 Billion) times. This tests the raw instruction dispatch speed and integer arithmetic optimization.

### Code
```rust
func Main() => {
    # 1 Billion Iterations
    1_000_000_000 times {
        a = 5;
        b = 5; 
        c = a + b;
    }
}
```

### Results
| Time | Mean (us) | Error | StdDev | Gen0 | Allocated |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **11.99 s** | 11,992,689.3 | 233,918.73 | 312,274.81 | - | 44.79 KB |

---

## 2. Recursion Overhead
**Test:** Recursive Fibonacci sequence calculation for N=30. This tests the overhead of function calls, stack frame creation, and return logic.

### Code
```rust
use FluenceIO;

func fib(n) => {
    if n < 0 -> return 0;
    if n == 1 -> return 1;
    return fib(n-1) + fib(n-2);
}

func Main() => fib(30);
```

### Results
| Time | Mean (us) | Error | StdDev | Gen0 | Allocated |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **276.84 ms** | 276,841.7 | 2,316.92 | 2,167.25 | - | 62.99 KB |

---

## 3. Complex Simulation (Game of Life)
**Test:** Conway's Game of Life simulation running for 500 generations on a 20x10 grid. This tests array access, modulo arithmetic, string manipulation (in `draw`), and logic branching.

### Code
```rust
use FluenceIO;

WIDTH = 20;
HEIGHT = 10;
TOTAL_CELLS = WIDTH * HEIGHT;

func get_idx(x, y) => {
    wrapped_x, wrapped_y <~| (x + WIDTH) % WIDTH, (y + HEIGHT) % HEIGHT; 
    return wrapped_y * WIDTH + wrapped_x;
}

func count_neighbors(grid, x, y) => {
    count = 0;
    for dy in -1..1 -> for dx in (-1)..1 {
        if dx, dy <==| 0 -> continue;
        idx = get_idx(x + dx, y + dy);
        if grid[idx] == 1 -> count++;
    }
    return count;
}

func draw(grid, gen) => {
    buffer = f"\n--- Generation {gen} ---\n";
    for y in 0..HEIGHT-1 {
        line = "";
        for x in 0..WIDTH-1 {
            cell, char <~| grid[y * WIDTH + x], cell == 1 ?: "O ", ". ";
            line += char;
        }
        buffer = f"{buffer}{line}\n";
    }
}

func Main() => {
    grid = [];
    next_grid = [];
    
    TOTAL_CELLS times { 
        grid.push(0); 
        next_grid.push(0); 
    };

    # 80 random cells
    80 times {
        grid[Random.between_exclusive(-1, TOTAL_CELLS)] = 1;
    } 

    generation = 0;
    
    500 times {
        draw(grid, generation);
        
        for y in 0..HEIGHT-1 -> for x in 0..WIDTH-1 {
            current_idx, is_alive, neighbors, new_state <~| y * WIDTH + x, grid[current_idx], count_neighbors(grid, x, y), match neighbors {
                3 -> 1;
                2 -> is_alive;
                rest -> 0;
            };
            next_grid[current_idx] = new_state;
        }

        grid >< next_grid; # Pointer swap
        generation++;
    }
}
```

### Results
| Time | Mean (us) | Error | StdDev | Gen0 | Allocated |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **332.05 ms** | 332,053.1 | 6,623.70 | 11,773.62 | 2000.0 | 14,967.02 KB |

---

## 4. Array Manipulation (Sieve of Eratosthenes)
**Test:** Finding all prime numbers up to 1,000,000. This tests dense array writes (`SetElement`) and nested loops.

### Code
```rust
use FluenceMath;
use FluenceIO;

func primes(limit) => {
    maxSquareRoot = sqrt(limit);
    eliminated = [false] * (limit + 1);

    for i = 2; i <= maxSquareRoot; i += 1; {
        if !eliminated[i] {
            for j = i*i; j <= limit; j += i; {
                eliminated[j] = true;
            }
        }
    }

    output = [];
    for i = 2; i <= limit; i += 1; {
        if !eliminated[i] -> output.push(i);
    }
    
    return output;
}

func Main() => {
    n = 1_000_000;
    x = primes(n);
}
```

### Results
| Time | Mean (us) | Error | StdDev | Gen0 | Allocated |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **227.69 ms** | 227,690.5 | 4,187.83 | 8,266.36 | 333.3 | 29,669.83 KB |

---

## 5. Branching Stress (Collatz Conjecture)
**Test:** Calculating the Collatz sequence for every number from 1 to 100,000. This tests conditional jump performance.

### Code
```rust
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
}

func Main() => Collatz();
```

### Results
| Time | Mean (us) | Error | StdDev | Gen0 | Allocated |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **542.78 ms** | 542,779.9 | 10,806.41 | 18,350.10 | - | 52.64 KB |

---

## 6. String Algorithm (Levenshtein Distance)
**Test:** Calculating the edit distance between two large strings.
*   **Input 1:** ~3,500 characters.
*   **Input 2:** ~17,000 characters.

### Code
```rust
use FluenceIO;
use FluenceMath;

func min(a, b, c) => (a < b ? (a < c ? a : c) : (b < c ? b : c));

func levenshtein(s1, s2) => {
    m, n <~| s1.length(), s2.length();
    dp <~| [0..n];

    for i in 1..m {
        prev_row_prev_col <~| i - 1;
        dp[0] = i;

        for j in 1..n {
            temp <~| dp[j];
            cost <~| 0;
            if s1[i-1] != s2[j-1] -> cost = 1;
 
            dp[j] = min(dp[j] + 1, dp[j-1] + 1, prev_row_prev_col + cost);
            prev_row_prev_col = temp;
        }
    }
    return dp[n];
} 

func Main() => {
    # Inputs are loaded from file in actual benchmark
    s1, s2 <~| "...", "..."; 
    dist <~| levenshtein(s1, s2);
}
```

### Results
| Time | Mean (us) | Error | StdDev | Gen0 | Allocated |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **18.62 s** | 18,622,482.9 | 372,447.50 | 671,599.73 | - | 1,732.16 KB |

---

## 7. Compiler Performance
**Test:** Parsing and compiling a large source file containing complex constructs, namespaces, functions, and structs.
*   **Source Size:** ~28,000 characters (~1,750 lines) featuring 31 namespaces, 31 structs, 31 enums, several functions per namespace ( struct method or function ).

### Results
| Component | Time | Mean (us) | Allocated |
| :--- | :--- | :--- | :--- |
| **Lexer** | **0.23 ms** | 231.8 | 78.27 KB |
| **Parser + Lexer** | **1.55 ms** | 1,553.1 | 1,124.8 KB |

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3447/23H2/2023Update/SunValley3)
Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 6.0.421
  [Host]     : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2


```
| Method                   | n  | Mean        | Error       | StdDev      |
|------------------------- |--- |------------:|------------:|------------:|
| **nthroot**                  | **2**  |    **620.9 μs** |    **11.22 μs** |     **9.94 μs** |
| nthroot_typecheck        | 2  |    890.2 μs |    17.31 μs |    22.51 μs |
| nthroot_typecheck_inline | 2  |    687.9 μs |     7.26 μs |     6.79 μs |
| **nthroot**                  | **3**  |  **1,529.5 μs** |    **22.01 μs** |    **20.59 μs** |
| nthroot_typecheck        | 3  |  1,837.4 μs |    35.78 μs |    35.14 μs |
| nthroot_typecheck_inline | 3  |  1,530.7 μs |     7.82 μs |     6.11 μs |
| **nthroot**                  | **5**  |  **5,321.5 μs** |    **61.20 μs** |    **57.25 μs** |
| nthroot_typecheck        | 5  |  6,275.4 μs |    76.85 μs |    68.13 μs |
| nthroot_typecheck_inline | 5  |  5,412.9 μs |    43.83 μs |    41.00 μs |
| **nthroot**                  | **10** | **38,391.9 μs** |   **296.42 μs** |   **262.77 μs** |
| nthroot_typecheck        | 10 | 47,059.5 μs | 1,029.75 μs | 3,020.08 μs |
| nthroot_typecheck_inline | 10 | 38,641.6 μs |   644.45 μs |   602.82 μs |

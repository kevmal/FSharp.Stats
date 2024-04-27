```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3447/23H2/2023Update/SunValley3)
Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 6.0.421
  [Host]     : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2


```
| Method | N     | Mean     | Error    | StdDev   |
|------- |------ |---------:|---------:|---------:|
| **Sha256** | **1000**  | **53.00 μs** | **0.943 μs** | **1.652 μs** |
| Md5    | 1000  | 20.47 μs | 0.276 μs | 0.258 μs |
| **Sha256** | **10000** | **52.70 μs** | **1.042 μs** | **1.115 μs** |
| Md5    | 10000 | 20.46 μs | 0.300 μs | 0.266 μs |

# Base64

Extension methods to enable conversion to and from Base64 strings with reduced memory allocation. Strings can be converted in place, or written/read from streams.

## TODO:

- Fully optimized FromUtf8Base64String, based on char* end to end. Profiler shows that 20% time spent on 
ASCII string => bytes, 20% on bytes => ASCII char. Can just convert to char* from string instantly, save 40%.


## References

- https://tools.ietf.org/html/rfc4648
- https://commons.apache.org/proper/commons-codec/xref-test/org/apache/commons/codec/binary/Base64Test.html

### Using SIMD instructions

- https://github.com/aklomp/base64
- https://devblogs.microsoft.com/dotnet/the-jit-finally-proposed-jit-and-simd-are-getting-married/
- https://docs.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics.x86?view=netcore-3.1

System.Numerics doesn't support bit shift operations on Vector<T>. Need to be on .NET Core with the intrinsics APIs to try this.

## Benches

### StreamToBase64

|    Method | input |         Mean |       Error |      StdDev |       Median | Ratio |   Gen 0 | Allocated |
|---------- |------ |-------------:|------------:|------------:|-------------:|------:|--------:|----------:|
| ConvertTo |   256 |     950.0 ns |    17.72 ns |    16.58 ns |     944.7 ns |  1.00 |  0.2384 |    1003 B |
|  StreamTo |   256 |     584.3 ns |    11.70 ns |    16.02 ns |     578.5 ns |  0.62 |  0.1717 |     722 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |   512 |   1,886.4 ns |    37.55 ns |    36.88 ns |   1,865.8 ns |  1.00 |  0.4616 |    1942 B |
|  StreamTo |   512 |   1,111.2 ns |    14.75 ns |    13.08 ns |   1,106.7 ns |  0.59 |  0.3338 |    1404 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |  1024 |   3,609.6 ns |    36.22 ns |    30.24 ns |   3,606.8 ns |  1.00 |  0.9117 |    3828 B |
|  StreamTo |  1024 |   2,237.3 ns |    44.48 ns |    75.52 ns |   2,248.6 ns |  0.61 |  0.6599 |    2778 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |  2048 |   7,036.0 ns |    34.34 ns |    32.12 ns |   7,035.2 ns |  1.00 |  1.8082 |    7595 B |
|  StreamTo |  2048 |   4,214.0 ns |    46.12 ns |    40.89 ns |   4,209.8 ns |  0.60 |  1.3123 |    5519 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |  4096 |  14,342.8 ns |   282.10 ns |   376.59 ns |  14,131.3 ns |  1.00 |  3.5858 |   15127 B |
|  StreamTo |  4096 |   8,209.6 ns |    95.10 ns |    84.30 ns |   8,184.6 ns |  0.57 |  2.6093 |   10984 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |  8192 |  28,121.2 ns |   550.64 ns |   565.46 ns |  27,831.6 ns |  1.00 |  7.1411 |   30144 B |
|  StreamTo |  8192 |  16,720.5 ns |   329.76 ns |   493.58 ns |  16,554.1 ns |  0.60 |  5.1880 |   21904 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo | 16384 |  54,929.6 ns |   369.26 ns |   308.35 ns |  54,981.0 ns |  1.00 | 14.2822 |   60184 B |
|  StreamTo | 16384 |  33,705.6 ns |   658.32 ns |   583.58 ns |  33,909.3 ns |  0.61 | 10.3760 |   43752 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo | 32768 | 117,409.5 ns | 2,308.33 ns | 2,834.84 ns | 115,435.3 ns |  1.00 | 27.7100 |  120232 B |
|  StreamTo | 32768 |  67,350.4 ns |   853.04 ns |   712.33 ns |  67,137.2 ns |  0.58 | 27.7100 |   87416 B |



### StreamFromBase64

|      Method |  input |       Mean |      Error |     StdDev |     Median | Ratio |    Gen 0 | Allocated |
|------------ |------- |-----------:|-----------:|-----------:|-----------:|------:|---------:|----------:|
| ConvertFrom |    256 |   1.482 us |  0.0294 us |  0.0422 us |   1.475 us |  1.00 |   0.1888 |     797 B |
|  StreamFrom |    256 |   3.474 us |  0.0627 us |  0.0616 us |   3.474 us |  2.36 |   0.0610 |     264 B |
|             |        |            |            |            |            |       |          |           |
| ConvertFrom |    512 |   2.796 us |  0.0529 us |  0.0588 us |   2.778 us |  1.00 |   0.3700 |    1566 B |
|  StreamFrom |    512 |   5.296 us |  0.1049 us |  0.1077 us |   5.282 us |  1.89 |   0.0610 |     264 B |
|             |        |            |            |            |            |       |          |           |
| ConvertFrom |   1024 |   5.276 us |  0.0969 us |  0.1116 us |   5.227 us |  1.00 |   0.7324 |    3105 B |
|  StreamFrom |   1024 |   8.522 us |  0.0694 us |  0.0580 us |   8.511 us |  1.61 |   0.0610 |     265 B |
|             |        |            |            |            |            |       |          |           |
| ConvertFrom |   2048 |  10.992 us |  0.2694 us |  0.7729 us |  10.680 us |  1.00 |   1.4648 |    6183 B |
|  StreamFrom |   2048 |  15.547 us |  0.1903 us |  0.1589 us |  15.490 us |  1.46 |   0.0610 |     265 B |
|             |        |            |            |            |            |       |          |           |
| ConvertFrom |   4096 |  20.937 us |  0.4165 us |  0.8508 us |  20.771 us |  1.00 |   2.9297 |   12340 B |
|  StreamFrom |   4096 |  31.543 us |  0.6056 us |  0.7437 us |  31.253 us |  1.53 |   0.0610 |     413 B |
|             |        |            |            |            |            |       |          |           |
| ConvertFrom |   8192 |  42.058 us |  0.8296 us |  1.1898 us |  42.047 us |  1.00 |   5.8594 |   24628 B |
|  StreamFrom |   8192 |  63.658 us |  1.2499 us |  2.4671 us |  62.715 us |  1.52 |   0.1221 |     561 B |
|             |        |            |            |            |            |       |          |           |
| ConvertFrom |  16384 |  83.405 us |  1.6537 us |  2.7629 us |  82.989 us |  1.00 |  11.5967 |   49204 B |
|  StreamFrom |  16384 | 124.832 us |  2.4889 us |  4.9129 us | 123.378 us |  1.50 |        - |    1006 B |
|             |        |            |            |            |            |       |          |           |
| ConvertFrom |  32768 | 163.363 us |  3.0391 us |  7.1636 us | 160.397 us |  1.00 |  23.1934 |   98356 B |
|  StreamFrom |  32768 | 241.901 us |  4.1513 us |  4.2631 us | 241.647 us |  1.42 |        - |    1748 B |
|             |        |            |            |            |            |       |          |           |
| ConvertFrom |  65536 | 380.923 us |  7.6017 us | 18.9309 us | 376.869 us |  1.00 |  41.5039 |  196648 B |
|  StreamFrom |  65536 | 503.032 us |  9.8727 us | 21.0395 us | 498.511 us |  1.32 |        - |    3384 B |
|             |        |            |            |            |            |       |          |           |
| ConvertFrom | 131072 | 772.906 us | 12.8717 us | 12.0402 us | 772.701 us |  1.00 | 124.0234 |  393248 B |
|  StreamFrom | 131072 | 965.072 us | 14.5211 us | 12.1258 us | 959.965 us |  1.25 |        - |    6496 B |


### StringToBase64


|    Method |   data |         Mean |       Error |       StdDev | Ratio |    Gen 0 | Allocated |
|---------- |------- |-------------:|------------:|-------------:|------:|---------:|----------:|
| ConvertTo |   1024 |   3,697.6 ns |    35.39 ns |     33.10 ns |  1.00 |   0.9079 |    3828 B |
|  StreamTo |   1024 |   4,104.2 ns |    44.44 ns |     34.70 ns |  1.11 |   0.7858 |    3298 B |
|           |        |              |             |              |       |          |           |
| ConvertTo | 131072 | 457,084.6 ns | 6,405.96 ns |  5,992.14 ns |  1.00 | 142.5781 |  480656 B |
|  StreamTo | 131072 | 418,116.2 ns | 8,025.71 ns | 12,729.62 ns |  0.92 |  99.6094 |  350615 B |
|           |        |              |             |              |       |          |           |
| ConvertTo |  16384 |  60,829.2 ns | 1,229.04 ns |  3,585.17 ns |  1.00 |  14.2822 |   60184 B |
|  StreamTo |  16384 |  55,634.1 ns | 1,392.20 ns |  4,061.12 ns |  0.92 |  10.4980 |   44312 B |
|           |        |              |             |              |       |          |           |
| ConvertTo |   2048 |   7,719.2 ns |   152.65 ns |    335.08 ns |  1.00 |   1.8082 |    7595 B |
|  StreamTo |   2048 |   7,392.0 ns |   145.82 ns |    297.87 ns |  0.96 |   1.4343 |    6034 B |
|           |        |              |             |              |       |          |           |
| ConvertTo |    256 |     978.3 ns |    19.58 ns |     28.70 ns |  1.00 |   0.2384 |    1003 B |
|  StreamTo |    256 |   1,917.8 ns |    38.41 ns |     74.01 ns |  1.96 |   0.2956 |    1244 B |
|           |        |              |             |              |       |          |           |
| ConvertTo |  32768 | 120,500.4 ns | 2,284.94 ns |  2,539.70 ns |  1.00 |  27.7100 |  120232 B |
|  StreamTo |  32768 | 100,625.3 ns | 1,979.01 ns |  3,081.08 ns |  0.84 |  27.7100 |   88099 B |
|           |        |              |             |              |       |          |           |
| ConvertTo |   4096 |  15,362.9 ns |   305.06 ns |    759.70 ns |  1.00 |   3.5706 |   15127 B |
|  StreamTo |   4096 |  15,423.5 ns |   456.46 ns |  1,345.89 ns |  1.01 |   2.7466 |   11524 B |
|           |        |              |             |              |       |          |           |
| ConvertTo |    512 |   1,973.4 ns |    40.11 ns |    117.65 ns |  1.00 |   0.4616 |    1942 B |
|  StreamTo |    512 |   2,741.3 ns |    54.74 ns |    150.78 ns |  1.39 |   0.4578 |    1926 B |
|           |        |              |             |              |       |          |           |
| ConvertTo |  65536 | 238,210.8 ns | 4,699.23 ns |  6,432.36 ns |  1.00 |  55.4199 |  240384 B |
|  StreamTo |  65536 | 193,640.5 ns | 1,622.06 ns |  1,437.92 ns |  0.81 |  55.4199 |  175704 B |
|           |        |              |             |              |       |          |           |
| ConvertTo |   8192 |  28,244.5 ns |   110.96 ns |     92.65 ns |  1.00 |   7.1411 |   30144 B |
|  StreamTo |   8192 |  25,210.0 ns |   419.09 ns |    349.96 ns |  0.89 |   5.3406 |   22430 B |


### StringFromBase64


|      Method |   data |         Mean |      Error |     StdDev |       Median | Ratio |    Gen 0 | Allocated |
|------------ |------- |-------------:|-----------:|-----------:|-------------:|------:|---------:|----------:|
| ConvertFrom |    256 |     1.431 us |  0.0240 us |  0.0225 us |     1.422 us |  1.00 |   0.1888 |     797 B |
|  StreamFrom |    256 |     1.434 us |  0.0121 us |  0.0095 us |     1.437 us |  1.00 |   0.1888 |     797 B |
|             |        |              |            |            |              |       |          |           |
| ConvertFrom |    512 |     2.653 us |  0.0411 us |  0.0343 us |     2.649 us |  1.00 |   0.3700 |    1566 B |
|  StreamFrom |    512 |     2.750 us |  0.0538 us |  0.0736 us |     2.762 us |  1.04 |   0.3700 |    1566 B |
|             |        |              |            |            |              |       |          |           |
| ConvertFrom |   1024 |     5.182 us |  0.0357 us |  0.0298 us |     5.191 us |  1.00 |   0.7401 |    3105 B |
|  StreamFrom |   1024 |    12.637 us |  0.2507 us |  0.3346 us |    12.434 us |  2.46 |   0.6104 |    2564 B |
|             |        |              |            |            |              |       |          |           |
| ConvertFrom |   2048 |    10.099 us |  0.0789 us |  0.0616 us |    10.093 us |  1.00 |   1.4648 |    6183 B |
|  StreamFrom |   2048 |    21.218 us |  0.4204 us |  0.5004 us |    21.106 us |  2.09 |   1.0986 |    4619 B |
|             |        |              |            |            |              |       |          |           |
| ConvertFrom |   4096 |    20.243 us |  0.3419 us |  0.2855 us |    20.129 us |  1.00 |   2.9297 |   12340 B |
|  StreamFrom |   4096 |    39.915 us |  0.7939 us |  1.1637 us |    39.909 us |  1.97 |   2.0752 |    8878 B |
|             |        |              |            |            |              |       |          |           |
| ConvertFrom |   8192 |    40.005 us |  0.6649 us |  0.5552 us |    39.743 us |  1.00 |   5.8594 |   24628 B |
|  StreamFrom |   8192 |    73.411 us |  0.6841 us |  0.6399 us |    73.551 us |  1.83 |   4.0283 |   17218 B |
|             |        |              |            |            |              |       |          |           |
| ConvertFrom |  16384 |    79.310 us |  0.7240 us |  0.6046 us |    79.353 us |  1.00 |  11.5967 |   49204 B |
|  StreamFrom |  16384 |   145.456 us |  2.9057 us |  2.9839 us |   144.455 us |  1.84 |   8.0566 |   34052 B |
|             |        |              |            |            |              |       |          |           |
| ConvertFrom |  32768 |   157.254 us |  0.9824 us |  0.7670 us |   157.071 us |  1.00 |  23.1934 |   98356 B |
|  StreamFrom |  32768 |   282.910 us |  3.5762 us |  2.7921 us |   282.367 us |  1.80 |  15.6250 |   67566 B |
|             |        |              |            |            |              |       |          |           |
| ConvertFrom |  65536 |   322.867 us |  1.0735 us |  0.8964 us |   323.180 us |  1.00 |  41.5039 |  196648 B |
|  StreamFrom |  65536 |   580.539 us | 11.4230 us | 14.0285 us |   576.197 us |  1.79 |  41.0156 |  134848 B |
|             |        |              |            |            |              |       |          |           |
| ConvertFrom | 131072 |   727.617 us |  6.1183 us |  5.7231 us |   727.427 us |  1.00 | 116.2109 |  393248 B |
|  StreamFrom | 131072 | 1,161.373 us | 20.9996 us | 19.6431 us | 1,165.704 us |  1.60 |  80.0781 |  269048 B |

This is comparing to crypto stream, memory allocs are excessive and perf tanks:

|           Method |   data |          Mean |         Error |        StdDev |        Median | Ratio |     Gen 0 | Allocated |
|----------------- |------- |--------------:|--------------:|--------------:|--------------:|------:|----------:|----------:|
|      ConvertFrom |   1024 |      5.212 us |     0.0772 us |     0.0685 us |      5.192 us |  1.00 |    0.7401 |    3105 B |
|       StreamFrom |   1024 |     14.102 us |     0.2803 us |     0.4526 us |     13.966 us |  2.73 |    0.6104 |    2564 B |
| CryptoStreamFrom |   1024 |    340.905 us |     6.6863 us |     7.1542 us |    337.914 us | 65.36 |   15.1367 |   64173 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom | 131072 |    724.461 us |    14.1122 us |    16.2516 us |    728.312 us |  1.00 |  116.2109 |  393248 B |
|       StreamFrom | 131072 |  1,333.259 us |    25.6557 us |    34.2496 us |  1,342.586 us |  1.84 |   78.1250 |  269204 B |
| CryptoStreamFrom | 131072 | 48,468.583 us | 2,781.3085 us | 8,113.2075 us | 48,924.591 us | 62.92 | 1937.5000 | 8138814 B |

# Base64

Extension methods to enable conversion to and from Base64 strings with reduced memory allocation. Strings can be converted in place, or written/read from streams.

## TODO:

- Perf tests for crypto API stream, to demonstrate how bad it is

- Fully optimized FromUtf8Base64String, based on char end to end. Profiler shows that 20% time spent on 
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
| ConvertTo |  1024 |   3,609.6 ns |    36.22 ns |    30.24 ns |   3,606.8 ns |  1.00 |  0.9117 |    3828 B |
|  StreamTo |  1024 |   2,237.3 ns |    44.48 ns |    75.52 ns |   2,248.6 ns |  0.61 |  0.6599 |    2778 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo | 16384 |  54,929.6 ns |   369.26 ns |   308.35 ns |  54,981.0 ns |  1.00 | 14.2822 |   60184 B |
|  StreamTo | 16384 |  33,705.6 ns |   658.32 ns |   583.58 ns |  33,909.3 ns |  0.61 | 10.3760 |   43752 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |  2048 |   7,036.0 ns |    34.34 ns |    32.12 ns |   7,035.2 ns |  1.00 |  1.8082 |    7595 B |
|  StreamTo |  2048 |   4,214.0 ns |    46.12 ns |    40.89 ns |   4,209.8 ns |  0.60 |  1.3123 |    5519 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |   256 |     950.0 ns |    17.72 ns |    16.58 ns |     944.7 ns |  1.00 |  0.2384 |    1003 B |
|  StreamTo |   256 |     584.3 ns |    11.70 ns |    16.02 ns |     578.5 ns |  0.62 |  0.1717 |     722 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo | 32768 | 117,409.5 ns | 2,308.33 ns | 2,834.84 ns | 115,435.3 ns |  1.00 | 27.7100 |  120232 B |
|  StreamTo | 32768 |  67,350.4 ns |   853.04 ns |   712.33 ns |  67,137.2 ns |  0.58 | 27.7100 |   87416 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |  4096 |  14,342.8 ns |   282.10 ns |   376.59 ns |  14,131.3 ns |  1.00 |  3.5858 |   15127 B |
|  StreamTo |  4096 |   8,209.6 ns |    95.10 ns |    84.30 ns |   8,184.6 ns |  0.57 |  2.6093 |   10984 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |   512 |   1,886.4 ns |    37.55 ns |    36.88 ns |   1,865.8 ns |  1.00 |  0.4616 |    1942 B |
|  StreamTo |   512 |   1,111.2 ns |    14.75 ns |    13.08 ns |   1,106.7 ns |  0.59 |  0.3338 |    1404 B |
|           |       |              |             |             |              |       |         |           |
| ConvertTo |  8192 |  28,121.2 ns |   550.64 ns |   565.46 ns |  27,831.6 ns |  1.00 |  7.1411 |   30144 B |
|  StreamTo |  8192 |  16,720.5 ns |   329.76 ns |   493.58 ns |  16,554.1 ns |  0.60 |  5.1880 |   21904 B |


### StreamFromBase64

|      Method | input |       Mean |     Error |    StdDev | Ratio |   Gen 0 | Allocated |
|------------ |------ |-----------:|----------:|----------:|------:|--------:|----------:|
| ConvertFrom |  1024 |   5.248 us | 0.1049 us | 0.1288 us |  1.00 |  0.7477 |    3138 B |
|  StreamFrom |  1024 |  11.666 us | 0.2218 us | 0.1852 us |  2.21 |  0.1221 |     514 B |
|             |       |            |           |           |       |         |           |
| ConvertFrom | 16384 |  79.253 us | 1.3395 us | 1.1874 us |  1.00 | 11.5967 |   49256 B |
|  StreamFrom | 16384 | 169.290 us | 2.1816 us | 1.9339 us |  2.14 |  0.2441 |    1998 B |
|             |       |            |           |           |       |         |           |
| ConvertFrom |  2048 |   9.988 us | 0.0699 us | 0.0620 us |  1.00 |  1.4801 |    6220 B |
|  StreamFrom |  2048 |  21.650 us | 0.3039 us | 0.2373 us |  2.17 |  0.1221 |     514 B |
|             |       |            |           |           |       |         |           |
| ConvertFrom |   256 |   1.418 us | 0.0276 us | 0.0317 us |  1.00 |  0.1965 |     826 B |
|  StreamFrom |   256 |   4.042 us | 0.0581 us | 0.0515 us |  2.85 |  0.1221 |     514 B |
|             |       |            |           |           |       |         |           |
| ConvertFrom | 32768 | 157.945 us | 1.0822 us | 0.9593 us |  1.00 | 23.1934 |   98408 B |
|  StreamFrom | 32768 | 344.292 us | 6.6779 us | 8.6832 us |  2.18 |  0.4883 |    3484 B |
|             |       |            |           |           |       |         |           |
| ConvertFrom |  4096 |  20.292 us | 0.3895 us | 0.4000 us |  1.00 |  2.9297 |   12380 B |
|  StreamFrom |  4096 |  44.137 us | 0.8801 us | 1.2338 us |  2.19 |  0.1831 |     811 B |
|             |       |            |           |           |       |         |           |
| ConvertFrom |   512 |   2.697 us | 0.0318 us | 0.0282 us |  1.00 |  0.3777 |    1597 B |
|  StreamFrom |   512 |   6.655 us | 0.0942 us | 0.0787 us |  2.47 |  0.1221 |     514 B |
|             |       |            |           |           |       |         |           |
| ConvertFrom |  8192 |  39.543 us | 0.3859 us | 0.3222 us |  1.00 |  5.8594 |   24680 B |
|  StreamFrom |  8192 |  84.216 us | 0.7181 us | 0.6365 us |  2.13 |  0.2441 |    1108 B |


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
|             |        |              |            |            |              |       |          |           |
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

This is comparing to crypto stream:

|           Method |   data |          Mean |         Error |        StdDev |        Median | Ratio |     Gen 0 | Allocated |
|----------------- |------- |--------------:|--------------:|--------------:|--------------:|------:|----------:|----------:|
|      ConvertFrom |   1024 |      5.212 us |     0.0772 us |     0.0685 us |      5.192 us |  1.00 |    0.7401 |    3105 B |
|       StreamFrom |   1024 |     14.102 us |     0.2803 us |     0.4526 us |     13.966 us |  2.73 |    0.6104 |    2564 B |
| CryptoStreamFrom |   1024 |    340.905 us |     6.6863 us |     7.1542 us |    337.914 us | 65.36 |   15.1367 |   64173 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom | 131072 |    724.461 us |    14.1122 us |    16.2516 us |    728.312 us |  1.00 |  116.2109 |  393248 B |
|       StreamFrom | 131072 |  1,333.259 us |    25.6557 us |    34.2496 us |  1,342.586 us |  1.84 |   78.1250 |  269204 B |
| CryptoStreamFrom | 131072 | 48,468.583 us | 2,781.3085 us | 8,113.2075 us | 48,924.591 us | 62.92 | 1937.5000 | 8138814 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom |  16384 |     79.516 us |     0.4226 us |     0.3746 us |     79.597 us |  1.00 |   11.5967 |   49204 B |
|       StreamFrom |  16384 |    164.855 us |     2.1987 us |     1.8360 us |    164.543 us |  2.07 |    8.0566 |   34052 B |
| CryptoStreamFrom |  16384 |  5,683.180 us |   105.5068 us |   170.3739 us |  5,657.788 us | 71.00 |  242.1875 | 1017862 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom |   2048 |     10.369 us |     0.2059 us |     0.2451 us |     10.329 us |  1.00 |    1.4648 |    6183 B |
|       StreamFrom |   2048 |     24.164 us |     0.4755 us |     0.8074 us |     23.877 us |  2.31 |    1.0986 |    4619 B |
| CryptoStreamFrom |   2048 |    691.252 us |    13.6848 us |    16.2908 us |    686.989 us | 66.69 |   30.2734 |  127697 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom |    256 |      1.465 us |     0.0285 us |     0.0435 us |      1.456 us |  1.00 |    0.1888 |     797 B |
|       StreamFrom |    256 |      1.437 us |     0.0154 us |     0.0128 us |      1.437 us |  0.98 |    0.1888 |     797 B |
| CryptoStreamFrom |    256 |     91.390 us |     1.8028 us |     2.4677 us |     90.778 us | 62.27 |    3.9063 |   16485 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom |  32768 |    159.837 us |     3.0095 us |     2.8151 us |    158.512 us |  1.00 |   23.1934 |   98356 B |
|       StreamFrom |  32768 |    333.685 us |     6.6702 us |    10.7711 us |    332.841 us |  2.06 |   15.6250 |   67566 B |
| CryptoStreamFrom |  32768 |  7,377.096 us |   145.2380 us |   221.7937 us |  7,276.863 us | 46.52 |  484.3750 | 2035086 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom |   4096 |     20.483 us |     0.3952 us |     0.5668 us |     20.174 us |  1.00 |    2.9297 |   12340 B |
|       StreamFrom |   4096 |     43.803 us |     0.1993 us |     0.1556 us |     43.812 us |  2.12 |    2.0752 |    8878 B |
| CryptoStreamFrom |   4096 |  1,464.109 us |    29.0035 us |    71.1460 us |  1,465.179 us | 71.87 |   60.5469 |  254914 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom |    512 |      2.661 us |     0.0526 us |     0.0466 us |      2.641 us |  1.00 |    0.3700 |    1566 B |
|       StreamFrom |    512 |      2.721 us |     0.0542 us |     0.0704 us |      2.688 us |  1.02 |    0.3700 |    1566 B |
| CryptoStreamFrom |    512 |    169.252 us |     3.2611 us |     2.8908 us |    169.536 us | 63.62 |    7.5684 |   32320 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom |  65536 |    326.723 us |     4.4098 us |     4.1249 us |    325.087 us |  1.00 |   41.5039 |  196648 B |
|       StreamFrom |  65536 |    652.175 us |     8.4286 us |     7.4718 us |    651.282 us |  2.00 |   41.0156 |  134848 B |
| CryptoStreamFrom |  65536 | 26,765.923 us |   498.0563 us |   511.4671 us | 26,772.116 us | 81.91 |  937.5000 | 4069672 B |
|                  |        |               |               |               |               |       |           |           |
|      ConvertFrom |   8192 |     40.626 us |     0.7880 us |     1.2499 us |     40.054 us |  1.00 |    5.8594 |   24628 B |
|       StreamFrom |   8192 |     83.399 us |     0.7535 us |     0.6292 us |     83.319 us |  2.05 |    4.0283 |   17218 B |
| CryptoStreamFrom |   8192 |  2,843.263 us |    55.1496 us |    63.5103 us |  2,835.220 us | 70.00 |  121.0938 |  509187 B |
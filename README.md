# Base64

Extension methods to enable conversion to and from Base64 strings with reduced memory allocations.

## TODO:

- Functional tests for stream version of FromBase64
	- space (leading, trailing, inner)
	- padding (leading, trailing, inner)
	- 1, 2 and 3 blocks
	- incorrect length

- Perf tests for crypto API stream, to demonstrate how bad it is

- Fully optimized FromUtf8Base64String, based on char end to end. Profiler shows that 20% time spent on 
ASCII string => bytes, 20% on bytes => ASCII char. Can just convert to char* from string instantly, save 40%.


## References

- https://tools.ietf.org/html/rfc4648
- https://commons.apache.org/proper/commons-codec/xref-test/org/apache/commons/codec/binary/Base64Test.html

- https://github.com/aklomp/base64
- https://devblogs.microsoft.com/dotnet/the-jit-finally-proposed-jit-and-simd-are-getting-married/

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


|      Method | count |       Mean |     Error |    StdDev | Ratio |   Gen 0 | Allocated |
|------------ |------ |-----------:|----------:|----------:|------:|--------:|----------:|
| ConvertFrom |  1024 |   5.151 us | 0.0322 us | 0.0301 us |  1.00 |  0.7477 |   3.06 KB |
|  StreamFrom |  1024 |  13.039 us | 0.1345 us | 0.1050 us |  2.53 |  0.7019 |   2.92 KB |
|             |       |            |           |           |       |         |           |
| ConvertFrom |  2048 |  10.082 us | 0.0836 us | 0.0698 us |  1.00 |  1.4801 |   6.07 KB |
|  StreamFrom |  2048 |  22.835 us | 0.4533 us | 0.6355 us |  2.27 |  1.1902 |   4.93 KB |
|             |       |            |           |           |       |         |           |
| ConvertFrom |  4096 |  20.437 us | 0.3901 us | 0.5957 us |  1.00 |  2.9297 |  12.09 KB |
|  StreamFrom |  4096 |  42.762 us | 0.2422 us | 0.2022 us |  2.11 |  2.1973 |   9.24 KB |
|             |       |            |           |           |       |         |           |
| ConvertFrom |  8192 |  39.599 us | 0.2580 us | 0.2154 us |  1.00 |  5.8594 |   24.1 KB |
|  StreamFrom |  8192 |  82.677 us | 1.4932 us | 1.4666 us |  2.09 |  4.2725 |  17.53 KB |
|             |       |            |           |           |       |         |           |
| ConvertFrom | 16384 |  79.199 us | 0.8044 us | 0.6717 us |  1.00 | 11.5967 |   48.1 KB |
|  StreamFrom | 16384 | 160.927 us | 1.0911 us | 0.9673 us |  2.03 |  8.3008 |  34.39 KB |
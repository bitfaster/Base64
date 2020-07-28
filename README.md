# Base64

Extension methods to enable conversion to and from Base64 strings with reduced memory allocations.

##TODO:

- Functional tests for transform block
	* space (leading, trailing, inner)
	* padding (leading, trailing, inner)
	* 1, 2 and 3 blocks
	- incorrect length
	- https://commons.apache.org/proper/commons-codec/xref-test/org/apache/commons/codec/binary/Base64Test.html

- Integration test for both string and stream where we throw an exception (e.g. invalid format), then run again 
and verify that good input works and that state is reset correctly.

- Functional tests for stream version of FromBase64
	- space (leading, trailing, inner)
	- padding (leading, trailing, inner)
	- 1, 2 and 3 blocks
	- incorrect length
	- https://commons.apache.org/proper/commons-codec/xref-test/org/apache/commons/codec/binary/Base64Test.html
- Perf tests for stream version of FromBase64

- Fully optimized FromUtf8Base64String, based on char end to end. Profiler shows that 20% time spent on 
ASCII string => bytes, 20% on bytes => ASCII char. Can just convert to char* from string instantly, save 40%.


## References

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


|    Method | count |         Mean |        Error |       StdDev |       Median | Ratio |   Gen 0 | Allocated |
|---------- |------ |-------------:|-------------:|-------------:|-------------:|------:|--------:|----------:|
| ConvertTo |     0 |     47.31 ns |     0.416 ns |     0.347 ns |     47.27 ns |  1.00 |  0.0134 |      56 B |
|  StreamTo |     0 |     58.59 ns |     1.241 ns |     1.657 ns |     57.94 ns |  1.24 |  0.0134 |      56 B |
|           |       |              |              |              |              |       |         |           |
| ConvertTo |     4 |     81.07 ns |     0.465 ns |     0.435 ns |     81.03 ns |  1.00 |  0.0191 |      80 B |
|  StreamTo |     4 |     93.65 ns |     1.937 ns |     3.237 ns |     92.85 ns |  1.15 |  0.0191 |      80 B |
|           |       |              |              |              |              |       |         |           |
| ConvertTo |    16 |    128.32 ns |     0.794 ns |     0.663 ns |    128.14 ns |  1.00 |  0.0286 |     120 B |
|  StreamTo |    16 |    142.43 ns |     2.903 ns |     4.069 ns |    140.56 ns |  1.10 |  0.0286 |     120 B |
|           |       |              |              |              |              |       |         |           |
| ConvertTo |   128 |    512.44 ns |    10.093 ns |    14.475 ns |    507.46 ns |  1.00 |  0.1259 |     530 B |
|  StreamTo |   128 |    514.71 ns |     3.220 ns |     2.514 ns |    514.56 ns |  1.01 |  0.1259 |     530 B |
|           |       |              |              |              |              |       |         |           |
| ConvertTo |   256 |    970.73 ns |    19.500 ns |    30.929 ns |    970.09 ns |  1.00 |  0.2384 |    1003 B |
|  StreamTo |   256 |  1,809.28 ns |    35.551 ns |    60.368 ns |  1,827.34 ns |  1.87 |  0.2956 |    1244 B |
|           |       |              |              |              |              |       |         |           |
| ConvertTo |   512 |  1,821.42 ns |    15.700 ns |    13.917 ns |  1,817.16 ns |  1.00 |  0.4616 |    1942 B |
|  StreamTo |   512 |  2,534.40 ns |    50.010 ns |    79.322 ns |  2,487.45 ns |  1.39 |  0.4578 |    1926 B |
|           |       |              |              |              |              |       |         |           |
| ConvertTo |  2048 |  7,025.11 ns |    63.735 ns |    49.760 ns |  7,023.44 ns |  1.00 |  1.8082 |    7595 B |
|  StreamTo |  2048 |  7,003.99 ns |   136.208 ns |   190.945 ns |  6,901.27 ns |  1.00 |  1.4343 |    6034 B |
|           |       |              |              |              |              |       |         |           |
| ConvertTo |  4096 | 13,824.71 ns |    64.025 ns |    49.986 ns | 13,827.03 ns |  1.00 |  3.5858 |   15127 B |
|  StreamTo |  4096 | 12,876.74 ns |   167.052 ns |   130.423 ns | 12,865.37 ns |  0.93 |  2.7466 |   11524 B |
|           |       |              |              |              |              |       |         |           |
| ConvertTo |  8192 | 27,409.10 ns |   120.927 ns |    94.412 ns | 27,434.60 ns |  1.00 |  7.1411 |   30144 B |
|  StreamTo |  8192 | 25,104.59 ns |   500.543 ns |   701.691 ns | 25,333.77 ns |  0.92 |  5.3406 |   22430 B |
|           |       |              |              |              |              |       |         |           |
| ConvertTo | 16384 | 55,087.65 ns | 1,061.406 ns | 1,303.502 ns | 54,484.09 ns |  1.00 | 14.2822 |   60184 B |
|  StreamTo | 16384 | 48,582.83 ns |   946.523 ns | 1,416.711 ns | 48,272.89 ns |  0.89 | 10.4980 |   44312 B |


### StringFromBase64

Extracted method for startPtr, ASCII encoding


|      Method | count |       Mean |     Error |    StdDev |     Median | Ratio |   Gen 0 | Allocated |
|------------ |------ |-----------:|----------:|----------:|-----------:|------:|--------:|----------:|
| ConvertFrom |  1024 |   4.957 us | 0.0395 us | 0.0330 us |   4.949 us |  1.00 |  0.7477 |   3.06 KB |
|  StreamFrom |  1024 |  14.676 us | 0.2801 us | 0.3739 us |  14.614 us |  2.95 |  0.7172 |   2.94 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom |  2048 |   9.710 us | 0.0326 us | 0.0305 us |   9.705 us |  1.00 |  1.4801 |   6.07 KB |
|  StreamFrom |  2048 |  25.565 us | 0.1947 us | 0.1626 us |  25.539 us |  2.63 |  1.1902 |   4.95 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom |  4096 |  19.311 us | 0.0852 us | 0.0712 us |  19.302 us |  1.00 |  2.9297 |  12.09 KB |
|  StreamFrom |  4096 |  50.784 us | 0.9999 us | 1.4340 us |  49.915 us |  2.61 |  2.1973 |   9.25 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom |  8192 |  38.374 us | 0.2309 us | 0.1803 us |  38.334 us |  1.00 |  5.8594 |   24.1 KB |
|  StreamFrom |  8192 |  97.642 us | 1.9169 us | 2.6872 us |  98.667 us |  2.53 |  4.2725 |  17.56 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom | 16384 |  76.272 us | 0.1966 us | 0.1535 us |  76.312 us |  1.00 | 11.5967 |   48.1 KB |
|  StreamFrom | 16384 | 187.573 us | 1.5583 us | 1.3012 us | 186.950 us |  2.46 |  8.3008 |  34.41 KB |


Removed some buffer.block copy calls

|      Method | count |       Mean |     Error |    StdDev |     Median | Ratio |   Gen 0 | Allocated |
|------------ |------ |-----------:|----------:|----------:|-----------:|------:|--------:|----------:|
| ConvertFrom |  1024 |   4.942 us | 0.0245 us | 0.0191 us |   4.942 us |  1.00 |  0.7477 |   3.06 KB |
|  StreamFrom |  1024 |  14.796 us | 0.2918 us | 0.4091 us |  15.030 us |  3.01 |  0.7172 |   2.94 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom |  2048 |   9.762 us | 0.0546 us | 0.0426 us |   9.754 us |  1.00 |  1.4801 |   6.07 KB |
|  StreamFrom |  2048 |  26.054 us | 0.5070 us | 0.6593 us |  25.642 us |  2.68 |  1.1902 |   4.95 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom |  4096 |  19.363 us | 0.0561 us | 0.0497 us |  19.348 us |  1.00 |  2.9297 |  12.09 KB |
|  StreamFrom |  4096 |  49.056 us | 0.4045 us | 0.3378 us |  49.106 us |  2.53 |  2.1973 |   9.25 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom |  8192 |  40.053 us | 0.7903 us | 1.0276 us |  39.767 us |  1.00 |  5.8594 |   24.1 KB |
|  StreamFrom |  8192 | 100.808 us | 1.7564 us | 1.5570 us | 101.147 us |  2.53 |  4.2725 |  17.56 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom | 16384 |  79.304 us | 1.5678 us | 2.1979 us |  78.574 us |  1.00 | 11.5967 |   48.1 KB |
|  StreamFrom | 16384 | 188.690 us | 1.8855 us | 1.5745 us | 188.349 us |  2.37 |  8.3008 |  34.41 KB |


Extract 'block' class

|      Method | count |       Mean |     Error |    StdDev |     Median | Ratio |   Gen 0 | Allocated |
|------------ |------ |-----------:|----------:|----------:|-----------:|------:|--------:|----------:|
| ConvertFrom |  1024 |   5.103 us | 0.0969 us | 0.1037 us |   5.050 us |  1.00 |  0.7477 |   3.06 KB |
|  StreamFrom |  1024 |  14.587 us | 0.0873 us | 0.0682 us |  14.581 us |  2.85 |  0.7019 |   2.92 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom |  2048 |   9.912 us | 0.0836 us | 0.0741 us |   9.905 us |  1.00 |  1.4801 |   6.07 KB |
|  StreamFrom |  2048 |  26.023 us | 0.2955 us | 0.2468 us |  25.961 us |  2.62 |  1.1902 |   4.93 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom |  4096 |  19.744 us | 0.1323 us | 0.1173 us |  19.725 us |  1.00 |  2.9297 |  12.09 KB |
|  StreamFrom |  4096 |  49.950 us | 0.3091 us | 0.2581 us |  49.889 us |  2.53 |  2.1973 |   9.24 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom |  8192 |  39.130 us | 0.2462 us | 0.2056 us |  39.103 us |  1.00 |  5.8594 |   24.1 KB |
|  StreamFrom |  8192 |  96.814 us | 0.6712 us | 0.5950 us |  96.665 us |  2.47 |  4.2725 |  17.53 KB |
|             |       |            |           |           |            |       |         |           |
| ConvertFrom | 16384 |  79.182 us | 1.5642 us | 1.9209 us |  77.915 us |  1.00 | 11.5967 |   48.1 KB |
|  StreamFrom | 16384 | 193.451 us | 3.8137 us | 4.5399 us | 191.188 us |  2.45 |  8.3008 |  34.39 KB |


Implemented flush temp for partial blocks

|      Method | count |       Mean |     Error |    StdDev | Ratio |   Gen 0 | Allocated |
|------------ |------ |-----------:|----------:|----------:|------:|--------:|----------:|
| ConvertFrom |  1024 |   5.052 us | 0.0160 us | 0.0141 us |  1.00 |  0.7477 |   3.06 KB |
|  StreamFrom |  1024 |  15.207 us | 0.2926 us | 0.4101 us |  3.00 |  0.7019 |   2.92 KB |
|             |       |            |           |           |       |         |           |
| ConvertFrom |  2048 |   9.996 us | 0.1129 us | 0.1000 us |  1.00 |  1.4801 |   6.07 KB |
|  StreamFrom |  2048 |  26.224 us | 0.3467 us | 0.2895 us |  2.62 |  1.1902 |   4.93 KB |
|             |       |            |           |           |       |         |           |
| ConvertFrom |  4096 |  19.799 us | 0.1699 us | 0.1419 us |  1.00 |  2.9297 |  12.09 KB |
|  StreamFrom |  4096 |  50.387 us | 0.7452 us | 0.6606 us |  2.54 |  2.1973 |   9.24 KB |
|             |       |            |           |           |       |         |           |
| ConvertFrom |  8192 |  39.213 us | 0.1977 us | 0.1849 us |  1.00 |  5.8594 |   24.1 KB |
|  StreamFrom |  8192 |  99.054 us | 1.9538 us | 2.6082 us |  2.55 |  4.2725 |  17.53 KB |
|             |       |            |           |           |       |         |           |
| ConvertFrom | 16384 |  78.401 us | 0.3258 us | 0.2888 us |  1.00 | 11.5967 |   48.1 KB |
|  StreamFrom | 16384 | 190.822 us | 0.9164 us | 0.7155 us |  2.44 |  8.3008 |  34.39 KB |


Pooled buffer, back to UTF8 encoding

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
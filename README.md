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

https://github.com/aklomp/base64
https://devblogs.microsoft.com/dotnet/the-jit-finally-proposed-jit-and-simd-are-getting-married/

## Benches

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
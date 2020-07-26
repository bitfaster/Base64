# Base64

Extension methods to enable conversion to and from Base64 strings with reduced memory allocations.

###TODO:

- Functional tests for stream version of FromBase64
	- space (leading, trailing, inner)
	- padding (leading, trailing, inner)
	- 1, 2 and 3 blocks
	- incorrect length
	- https://commons.apache.org/proper/commons-codec/xref-test/org/apache/commons/codec/binary/Base64Test.html
- Perf tests for stream version of FromBase64
- Fully optimized FromUtf8Base64String, based on char end to end. Profiler shows that 20% time spent on 
ASCII string => bytes, 20% on bytes => ASCII char. Can just convert to char* from string instantly.


### References

https://github.com/aklomp/base64

### Benches


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
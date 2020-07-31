using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Base64.UnitTests
{
    [TestClass]
    public class BufferedStreamWriterTests
    {

        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        private EncodingBuffer GetUtf8Buffer(int size = 1024)
        {
            return new EncodingBuffer(NoBomEncoding.UTF8, size);
        }

        private EncodingBuffer GetAsciiBuffer(int size = 1024)
        {
            return new EncodingBuffer(NoBomEncoding.ASCII, size);
        }

        [TestMethod]
        public void WriteChars()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Write a wide variety of characters and read them back

            Stream ms = CreateStream();
            var sw = new BufferedStreamWriter(ms, GetUtf8Buffer());
            StreamReader sr;

            for (int i = 0; i < chArr.Length; i++)
                sw.Write(chArr[i]);

            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                sr.Read().Should().Be((int)chArr[i]);
            }
        }

        [TestMethod]
        public void NullArray()
        {
            // [] Exception for null array
            Stream ms = CreateStream();
            var sw = new BufferedStreamWriter(ms, GetUtf8Buffer());

            sw.Invoking(s => s.Write(null, 0, 0)).Should().Throw<ArgumentNullException>();
            sw.Dispose();
        }

        [TestMethod]
        public void NegativeOffset()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Exception if offset is negative
            Stream ms = CreateStream();
            var sw = new BufferedStreamWriter(ms, GetUtf8Buffer());

            sw.Invoking(s => s.Write(chArr, -1, 0)).Should().Throw<ArgumentOutOfRangeException>();
            sw.Dispose();
        }

        [TestMethod]
        public void NegativeCount()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Exception if count is negative
            Stream ms = CreateStream();
            var sw = new BufferedStreamWriter(ms, GetUtf8Buffer());

            sw.Invoking(s => s.Write(chArr, 0, -1)).Should().Throw<ArgumentOutOfRangeException>();
            sw.Dispose();
        }

        [TestMethod]
        public void WriteCustomLenghtStrings()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Write some custom length strings
            Stream ms = CreateStream();
            var sw = new BufferedStreamWriter(ms, GetUtf8Buffer());
            StreamReader sr;

            sw.Write(chArr, 2, 5);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 2; i < 7; i++)
            {
                sr.Read().Should().Be((int)chArr[i]);
            }
            ms.Dispose();
        }

        [TestMethod]
        public void WriteToStreamWriter()
        {
            char[] chArr = TestDataProvider.CharData;
            // [] Just construct a streamwriter and write to it
            //-------------------------------------------------
            Stream ms = CreateStream();
            var sw = new BufferedStreamWriter(ms, GetUtf8Buffer());
            StreamReader sr;

            sw.Write(chArr, 0, chArr.Length);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                sr.Read().Should().Be((int)chArr[i]);
            }
            ms.Dispose();
        }

        [TestMethod]
        public void TestWritingPastEndOfArray()
        {
            char[] chArr = TestDataProvider.CharData;
            Stream ms = CreateStream();
            var sw = new BufferedStreamWriter(ms, GetUtf8Buffer());

            sw.Invoking(s => s.Write(chArr, 1, chArr.Length)).Should().Throw<ArgumentException>();
            sw.Dispose();
        }

        [TestMethod]
        public void VerifyWrittenString()
        {
            char[] chArr = TestDataProvider.CharData;
            // [] Write string with wide selection of characters and read it back

            StringBuilder sb = new StringBuilder(40);
            Stream ms = CreateStream();
            var sw = new BufferedStreamWriter(ms, GetUtf8Buffer());
            StreamReader sr;

            for (int i = 0; i < chArr.Length; i++)
                sb.Append(chArr[i]);

            sw.Write(sb.ToString());
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                sr.Read().Should().Be((int)chArr[i]);
            }
        }

        [TestMethod]
        public void NullStringWritesNothing()
        {
            // [] Null string should write nothing

            Stream ms = CreateStream();
            var sw = new BufferedStreamWriter(ms, GetUtf8Buffer());
            sw.Write((string)null);
            sw.Flush();
            ms.Length.Should().Be(0);
        }

        [TestMethod]
        public void Write_CharArray_WritesExpectedData()
        {
            Write_CharArray_WritesExpectedData(1, 1, 1, false);
            Write_CharArray_WritesExpectedData(100, 1, 100, false);
            Write_CharArray_WritesExpectedData(100, 10, 3, false);
            Write_CharArray_WritesExpectedData(1, 1, 1, true);
            Write_CharArray_WritesExpectedData(100, 1, 100, true);
            Write_CharArray_WritesExpectedData(100, 10, 3, true);
        }

        public void Write_CharArray_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new BufferedStreamWriter(s, GetAsciiBuffer(writerBufferSize), autoFlush))
            {
                var data = new char[length];
                var rand = new Random(42);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (char)(rand.Next(0, 26) + 'a');
                }

                int index = 0;

                while (index < data.Length)
                {
                    int n = Math.Min(data.Length, writeSize);
                    writer.Write(data, index, n);
                    index += n;
                }

                writer.Flush();

                s.ToArray().Select(b => (char)b).Should().BeEquivalentTo(data);
            }
        }

        [TestMethod]
        public void WriteLine_CharArray_WritesExpectedData()
        {
            WriteLine_CharArray_WritesExpectedData(1, 1, 1, false);
            WriteLine_CharArray_WritesExpectedData(100, 1, 100, false);
            WriteLine_CharArray_WritesExpectedData(100, 10, 3, false);
            WriteLine_CharArray_WritesExpectedData(1, 1, 1, true);
            WriteLine_CharArray_WritesExpectedData(100, 1, 100, true);
            WriteLine_CharArray_WritesExpectedData(100, 10, 3, true);
        }

        public void WriteLine_CharArray_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, Encoding.ASCII, writerBufferSize) { AutoFlush = autoFlush })
            {
                var data = new char[length];
                var rand = new Random(42);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (char)(rand.Next(0, 26) + 'a');
                }

                int index = 0;

                while (index < data.Length)
                {
                    int n = Math.Min(data.Length, writeSize);
                    writer.WriteLine(data, index, n);
                    index += n;
                }

                writer.Flush();

                s.Length.Should().Be(length + (Environment.NewLine.Length * (length / writeSize)));
            }
        }

        [TestMethod]
        public void AfterDisposeThrows()
        {
            // [] Calling methods after disposing the stream should throw
            //-----------------------------------------------------------------
            var sw2 = new BufferedStreamWriter(new MemoryStream(), GetUtf8Buffer());
            sw2.Dispose();
            ValidateDisposedExceptions(sw2);
        }

        [TestMethod]
        public void AfterCloseThrows()
        {
            // [] Calling methods after closing the stream should throw
            //-----------------------------------------------------------------
            var sw2 = new BufferedStreamWriter(new MemoryStream(), GetUtf8Buffer());
            sw2.Close();
            ValidateDisposedExceptions(sw2);
        }

        private void ValidateDisposedExceptions(BufferedStreamWriter sw)
        {
            sw.Invoking(w => w.Write('A')).Should().Throw<ObjectDisposedException>();
            sw.Invoking(w => w.Write("hello")).Should().Throw<ObjectDisposedException>();
            sw.Invoking(w => w.Flush()).Should().Throw<ObjectDisposedException>();
        }

        [TestMethod]
        public void CloseCausesFlush()
        {
            Stream memstr2;

            // [] Check that flush updates the underlying stream
            //-----------------------------------------------------------------
            memstr2 = CreateStream();
            var sw2 = new BufferedStreamWriter(memstr2, GetUtf8Buffer());

            var strTemp = "HelloWorld";
            sw2.Write(strTemp);

            memstr2.Length.Should().Be(0);

            sw2.Flush();
            memstr2.Length.Should().Be(strTemp.Length);

        }

        [TestMethod]
        public void CantFlushAfterDispose()
        {
            // [] Flushing disposed writer should throw
            //-----------------------------------------------------------------

            Stream memstr2 = CreateStream();
            var sw2 = new BufferedStreamWriter(memstr2, GetUtf8Buffer());

            sw2.Dispose();
            sw2.Invoking(w => w.Flush()).Should().Throw<ObjectDisposedException>();
        }

        [TestMethod]
        public void CantFlushAfterClose()
        {
            // [] Flushing closed writer should throw
            //-----------------------------------------------------------------

            Stream memstr2 = CreateStream();
            var sw2 = new BufferedStreamWriter(memstr2, GetUtf8Buffer());

            sw2.Close();
            sw2.Invoking(w => w.Flush()).Should().Throw<ObjectDisposedException>();
        }
    }

    public static class TestDataProvider
    {
        private static readonly char[] s_charData;
        private static readonly char[] s_smallData;
        private static readonly char[] s_largeData;

        public static object FirstObject { get; } = (object)1;
        public static object SecondObject { get; } = (object)"[second object]";
        public static object ThirdObject { get; } = (object)"<third object>";
        public static object[] MultipleObjects { get; } = new object[] { FirstObject, SecondObject, ThirdObject };

        public static string FormatStringOneObject { get; } = "Object is {0}";
        public static string FormatStringTwoObjects { get; } = $"Object are '{0}', {SecondObject}";
        public static string FormatStringThreeObjects { get; } = $"Objects are {0}, {SecondObject}, {ThirdObject}";
        public static string FormatStringMultipleObjects { get; } = "Multiple Objects are: {0}, {1}, {2}";

        static TestDataProvider()
        {
            s_charData = new char[]
            {
                char.MinValue,
                char.MaxValue,
                '\t',
                ' ',
                '$',
                '@',
                '#',
                '\0',
                '\v',
                '\'',
                '\u3190',
                '\uC3A0',
                'A',
                '5',
                '\r',
                '\uFE70',
                '-',
                ';',
                '\r',
                '\n',
                'T',
                '3',
                '\n',
                'K',
                '\u00E6'
            };

            s_smallData = "HELLO".ToCharArray();

            var data = new List<char>();
            for (int count = 0; count < 1000; ++count)
            {
                data.AddRange(s_smallData);
            }
            s_largeData = data.ToArray();
        }

        public static char[] CharData => s_charData;

        public static char[] SmallData => s_smallData;

        public static char[] LargeData => s_largeData;
    }

    internal sealed class DelegateStream : Stream
    {
        private readonly Func<bool> _canReadFunc;
        private readonly Func<bool> _canSeekFunc;
        private readonly Func<bool> _canWriteFunc;
        private readonly Action _flushFunc = null;
        private readonly Func<CancellationToken, Task> _flushAsyncFunc = null;
        private readonly Func<long> _lengthFunc;
        private readonly Func<long> _positionGetFunc;
        private readonly Action<long> _positionSetFunc;
        private readonly Func<byte[], int, int, int> _readFunc;
        private readonly Func<byte[], int, int, CancellationToken, Task<int>> _readAsyncFunc;
        private readonly Func<long, SeekOrigin, long> _seekFunc;
        private readonly Action<long> _setLengthFunc;
        private readonly Action<byte[], int, int> _writeFunc;
        private readonly Func<byte[], int, int, CancellationToken, Task> _writeAsyncFunc;
        private readonly Action<bool> _disposeFunc;

        public DelegateStream(
            Func<bool> canReadFunc = null,
            Func<bool> canSeekFunc = null,
            Func<bool> canWriteFunc = null,
            Action flushFunc = null,
            Func<CancellationToken, Task> flushAsyncFunc = null,
            Func<long> lengthFunc = null,
            Func<long> positionGetFunc = null,
            Action<long> positionSetFunc = null,
            Func<byte[], int, int, int> readFunc = null,
            Func<byte[], int, int, CancellationToken, Task<int>> readAsyncFunc = null,
            Func<long, SeekOrigin, long> seekFunc = null,
            Action<long> setLengthFunc = null,
            Action<byte[], int, int> writeFunc = null,
            Func<byte[], int, int, CancellationToken, Task> writeAsyncFunc = null,
            Action<bool> disposeFunc = null)
        {
            _canReadFunc = canReadFunc ?? (() => false);
            _canSeekFunc = canSeekFunc ?? (() => false);
            _canWriteFunc = canWriteFunc ?? (() => false);

            _flushFunc = flushFunc ?? (() => { });
            _flushAsyncFunc = flushAsyncFunc ?? (token => base.FlushAsync(token));

            _lengthFunc = lengthFunc ?? (() => { throw new NotSupportedException(); });
            _positionSetFunc = positionSetFunc ?? (_ => { throw new NotSupportedException(); });
            _positionGetFunc = positionGetFunc ?? (() => { throw new NotSupportedException(); });

            _readAsyncFunc = readAsyncFunc ?? ((buffer, offset, count, token) => base.ReadAsync(buffer, offset, count, token));
            _readFunc = readFunc ?? ((buffer, offset, count) => readAsyncFunc(buffer, offset, count, default).GetAwaiter().GetResult());

            _seekFunc = seekFunc ?? ((_, __) => { throw new NotSupportedException(); });
            _setLengthFunc = setLengthFunc ?? (_ => { throw new NotSupportedException(); });

            _writeAsyncFunc = writeAsyncFunc ?? ((buffer, offset, count, token) => base.WriteAsync(buffer, offset, count, token));
            _writeFunc = writeFunc ?? ((buffer, offset, count) => writeAsyncFunc(buffer, offset, count, default).GetAwaiter().GetResult());

            _disposeFunc = disposeFunc;
        }

        public override bool CanRead { get { return _canReadFunc(); } }
        public override bool CanSeek { get { return _canSeekFunc(); } }
        public override bool CanWrite { get { return _canWriteFunc(); } }

        public override void Flush() { _flushFunc(); }
        public override Task FlushAsync(CancellationToken cancellationToken) { return _flushAsyncFunc(cancellationToken); }

        public override long Length { get { return _lengthFunc(); } }
        public override long Position { get { return _positionGetFunc(); } set { _positionSetFunc(value); } }

        public override int Read(byte[] buffer, int offset, int count) { return _readFunc(buffer, offset, count); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { return _readAsyncFunc(buffer, offset, count, cancellationToken); }

        public override long Seek(long offset, SeekOrigin origin) { return _seekFunc(offset, origin); }
        public override void SetLength(long value) { _setLengthFunc(value); }

        public override void Write(byte[] buffer, int offset, int count) { _writeFunc(buffer, offset, count); }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { return _writeAsyncFunc(buffer, offset, count, cancellationToken); }

        protected override void Dispose(bool disposing) { _disposeFunc?.Invoke(disposing); }
    }
}

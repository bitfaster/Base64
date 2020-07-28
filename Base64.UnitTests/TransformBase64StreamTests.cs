using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64.UnitTests
{
    [TestClass]
    public class TransformBase64StreamTests
    {
        private readonly MemoryStream stream = new MemoryStream();
        private readonly EncodingBuffer encodingBuffer = new EncodingBuffer(NoBomEncoding.ASCII, 4096);
        private readonly Base64Buffer base64Buffer = new Base64Buffer();

        private int length = 4096 * 3;

        private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" + "abcdefghijklmnopqrstuvwxyz" + "0123456789+/";

        [TestMethod]
        public void StringMultiChunkWhiteSpace1()
        {
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append(" a");
            }

            ValidateString(sb.ToString());
        }

        [TestMethod]
        public void StringMultiChunkWhiteSpace2()
        {
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append("a ");
            }

            ValidateString(sb.ToString());
        }

        [TestMethod]
        public void StringMultiChunkWhiteSpace3()
        {
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append(" ");
                sb.Append(alphabet[i % alphabet.Length]);
                sb.Append(" ");
            }

            ValidateString(sb.ToString());
        }

        [TestMethod]
        public void StringMultiChunkWhiteSpace4()
        {
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append("  aa");
            }

            ValidateString(sb.ToString());
        }

        [TestMethod]
        public void StringMultiChunkWhiteSpace5()
        {
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append("   a");
            }

            ValidateString(sb.ToString());
        }


        private void ValidateString(string input)
        {
            var reference = Convert.FromBase64String(input);

            using (var base64Stream = new TransformBase64Stream(stream, base64Buffer, true))
            using (var w = new BufferedStreamWriter(base64Stream, encodingBuffer, true))
            {
                w.Write(input);
            }

            stream.TryGetBuffer(out var buffer);

            Console.WriteLine($"Exp: {Encoding.ASCII.GetString(reference)}");
            Console.WriteLine($"Act: {Encoding.ASCII.GetString(buffer.Array, buffer.Offset, buffer.Count)}");

            for (int i = 0; i < reference.Length; i++)
            {
                buffer.Array[i].Should().Be(reference[i], $"Difference at {i}");
            }

            buffer.Count.Should().Be(reference.Length);
        }
    }
}

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Base64.UnitTests
{
    [TestClass]
    public class TransformBlockTests
    {
        // a     YQ==
        // aa    YWE=
        // aaa   YWFh
        // aaaa  YWFhYQ==

        private readonly IBase64Transform t = new FromBase64TransformWithWhiteSpace();

        private static byte[] outputBuffer = new byte[4096];

        [TestMethod]
        public void Test_a()
        {
            Validate("YQ==");
        }

        [TestMethod]
        public void Test_aa()
        {
            Validate("YWE=");
        }

        [TestMethod]
        public void Test_aaa()
        {
            Validate("YWFh");
        }

        [TestMethod]
        public void Test_aaaa()
        {
            Validate("YWFhYQ==");
        }

        [TestMethod]
        public void Test__aaaa()
        {
            Validate(" YWFhYQ==");
        }

        [TestMethod]
        public void Test_aaaa_()
        {
            Validate("YWFhYQ== ");
        }

        [TestMethod]
        public void Test_aa_aa()
        {
            Validate("YWFh YQ==");
        }

        [TestMethod]
        public void Test_a_aaa()
        {
            Validate("YW FhYQ==");
        }

        [TestMethod]
        public void Test_aaa_a()
        {
            Validate("YWFhYQ ==");
        }

        [TestMethod]
        public void InnerSpace()
        {
            string base64string = new string('a', 2016) + "Zm 9v";
            Validate(base64string);
        }

        [TestMethod]
        public void InnerSpaceChunked()
        {
            // there should be 3 bytes in temp in first block, but there is only 2.
            string base64string = new string('a', 4) + "Zm 9v";
            ValidateChunked(base64string, 4096);
        }

        private void Validate(string input)
        {
            var reference = Convert.FromBase64String(input);

            var bytes = Encoding.ASCII.GetBytes(input);
            int len = t.TransformBlock(bytes, 0, bytes.Length, outputBuffer, 0);

            len.Should().Be(reference.Length);

            for (int i = 0; i < reference.Length; i++)
            {
                outputBuffer[i].Should().Be(reference[i], $"Difference at {i}");
            }
        }

        private void ValidateChunked(string input, int maxChunkSize)
        {
            var reference = Convert.FromBase64String(input);

            int inputBlockSize = 4;

            var bytes = Encoding.ASCII.GetBytes(input);

            int bytesToWrite = bytes.Length;
            int inputIndex = 0;
            int bytesWritten = 0;


            int chunk = Math.Min(maxChunkSize, bytesToWrite);

            // only write whole blocks
            int numWholeBlocks = chunk / inputBlockSize;
            int numWholeBlocksInBytes = numWholeBlocks * inputBlockSize;

            bytesWritten += t.TransformBlock(bytes, inputIndex, numWholeBlocksInBytes, outputBuffer, bytesWritten);
            inputIndex += numWholeBlocksInBytes;

            int remainder = bytes.Length - inputIndex;
            bytesWritten += t.TransformFinalBlock(bytes, inputIndex, remainder, outputBuffer, bytesWritten);



            
            bytesWritten.Should().Be(reference.Length);

            for (int i = 0; i < reference.Length; i++)
            {
                outputBuffer[i].Should().Be(reference[i], $"Difference at {i}");
            }
        }
    }
}

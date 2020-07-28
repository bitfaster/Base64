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
        private readonly FromBase64TransformWithWhiteSpace t = new FromBase64TransformWithWhiteSpace();

        private readonly Block block = new Block(0, 4096);

        // Tests RFC 4648 section 10 test vectors.
        // @see <a href="http://tools.ietf.org/html/rfc4648">http://tools.ietf.org/
        [TestMethod]
        public void RFC4648()
        {
            Validate("");
            Validate("Zg==");
            Validate("Zm8=");
            Validate("Zm9v");
            Validate("Zm9vYg==");
            Validate("Zm9vYmE=");
            Validate("Zm9vYmFy");
        }

        [TestMethod]
        public void InvalidFormatFirstBlock()
        {
            InvalidFormatFirstBlock("====");
            InvalidFormatFirstBlock("aaa!");
            InvalidFormatFirstBlock("=aaa");
            InvalidFormatFirstBlock("a=aa");
        }

        [TestMethod]
        public void InvalidFormatFinalBlock()
        {
            InvalidFormatFinalBlock("=");
            InvalidFormatFinalBlock("==");
            InvalidFormatFinalBlock("===");

            InvalidFormatFinalBlock("a");
            InvalidFormatFinalBlock("!");
            InvalidFormatFinalBlock("aa!");
        }

        [TestMethod]
        public void InvalidLength()
        {
            //InvalidFormatFinalBlock("aaaaa", "");
            //InvalidFormatFinalBlock("aaaa", "a"); // up to here works
            InvalidFormatFinalBlock("aaa", "aa");
        }

        [TestMethod]
        public void LeadingSpace()
        {
            Validate(" YWFhYQ==");
        }

        [TestMethod]
        public void TrailingSpace()
        {
            Validate("YWFhYQ== ");
        }

        [TestMethod]
        public void SingleBlockInnerSpace()
        {
            Validate("YWFh YQ==");
            Validate("YW FhYQ==");
            Validate("YWFhYQ ==");
        }

        [TestMethod]
        public void SingleBlockWithMultipleSpace()
        {
            Validate(" a  a  a  a ");
        }

        [TestMethod]
        public void SingleBlockInnerSpaceLong()
        {
            string base64string = new string('a', 2016) + "Zm 9v";
            Validate(base64string);
        }

        [TestMethod]
        public void TwoBlocksContinuous()
        {
            ValidateMultiblock("aa", "aa");
        }

        [TestMethod]
        public void TwoBlocksWithSpace()
        {
            ValidateMultiblock(" a  a ", " a  a ");
            ValidateMultiblock(" a  a", "  a  a ");
            ValidateMultiblock(" a  a ", " a  a ");
            ValidateMultiblock(" a  a  ", "a  a ");
        }

        [TestMethod]
        public void ThreeBlocksContinuous()
        {
            ValidateMultiblock("aaaa", "aaa", "a");
        }

        [TestMethod]
        public void ThreeBlocksWithSpace()
        {
            ValidateMultiblock(" a  a  a ", " a  a  a ", " a  a");
            ValidateMultiblock(" a  a  a", "  a  a  a ", " a  a");
            ValidateMultiblock(" a  a  a  ", "a  a  a ", " a  a");
            ValidateMultiblock(" a  a  a ", " a  a  a", "  a  a");
            ValidateMultiblock(" a  a  a ", " a  a  a  ", "a  a");
            ValidateMultiblock(" a  a  a", "  a  a  a", "  a  a");
            ValidateMultiblock(" a  a  a", "  a  a  a  ", "a  a");
            ValidateMultiblock(" a  a  a  ", "a  a  a", "  a  a");
            ValidateMultiblock(" a  a  a  ", "a  a  a  ", "a  a");
        }

        private void Validate(string input)
        {
            var reference = Convert.FromBase64String(input);

            var bytes = Encoding.ASCII.GetBytes(input);

            this.block.input = bytes;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes.Length;
            this.block.outputOffset = 0;

            int len = t.TransformBlock(block);

            len.Should().Be(reference.Length);

            for (int i = 0; i < reference.Length; i++)
            {
                block.outputBuffer[i].Should().Be(reference[i], $"Difference at {i}");
            }
        }

        private void InvalidFormatFirstBlock(string input)
        {
            this.block.Reset();
            t.Reset();

            var bytes = Encoding.ASCII.GetBytes(input);

            this.block.input = bytes;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes.Length;
            this.block.outputOffset = 0;

            t.Invoking(x => x.TransformBlock(block)).Should().Throw<FormatException>();
        }

        private void InvalidFormatFinalBlock(string input)
        {
            this.block.Reset();
            t.Reset();

            var bytes = Encoding.ASCII.GetBytes(input);

            this.block.input = bytes;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes.Length;
            this.block.outputOffset = 0;

            t.TransformBlock(block);

            t.Invoking(x => x.TransformFinalBlock(block)).Should().Throw<FormatException>();
        }

        private void InvalidFormatFinalBlock(string first, string final)
        {
            this.block.Reset();
            t.Reset();

            var bytes = Encoding.ASCII.GetBytes(first);

            this.block.input = bytes;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes.Length;
            this.block.outputOffset = 0;

            this.block.outputOffset += t.TransformBlock(block);

            bytes = Encoding.ASCII.GetBytes(final);

            this.block.input = bytes;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes.Length;

            t.Invoking(x => x.TransformFinalBlock(block)).Should().Throw<FormatException>();
        }

        private void ValidateMultiblock(string block1, string block2)
        {
            var reference = Convert.FromBase64String(block1 + block2);

            var bytes1 = Encoding.ASCII.GetBytes(block1);

            this.block.input = bytes1;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes1.Length;
            this.block.outputOffset = 0;

            int bytesWritten = t.TransformBlock(block);

            var bytes2 = Encoding.ASCII.GetBytes(block2);

            this.block.input = bytes2;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes2.Length;
            this.block.outputOffset = bytesWritten;

            bytesWritten += t.TransformFinalBlock(block);

            for (int i = 0; i < reference.Length; i++)
            {
                block.outputBuffer[i].Should().Be(reference[i], $"Difference at {i}");
            }

            bytesWritten.Should().Be(reference.Length);
        }

        private void ValidateMultiblock(string block1, string block2, string block3)
        {
            var reference = Convert.FromBase64String(block1 + block2 + block3);

            var bytes1 = Encoding.ASCII.GetBytes(block1);

            this.block.input = bytes1;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes1.Length;
            this.block.outputOffset = 0;

            int bytesWritten = t.TransformBlock(block);

            var bytes2 = Encoding.ASCII.GetBytes(block2);

            this.block.input = bytes2;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes2.Length;
            this.block.outputOffset = bytesWritten;

            bytesWritten += t.TransformBlock(block);

            var bytes3 = Encoding.ASCII.GetBytes(block3);

            this.block.input = bytes3;
            this.block.inputOffset = 0;
            this.block.inputCount = bytes3.Length;
            this.block.outputOffset = bytesWritten;

            bytesWritten += t.TransformFinalBlock(block);

            for (int i = 0; i < reference.Length; i++)
            {
                block.outputBuffer[i].Should().Be(reference[i], $"Difference at {i}");
            }

            bytesWritten.Should().Be(reference.Length);
        }
    }
}

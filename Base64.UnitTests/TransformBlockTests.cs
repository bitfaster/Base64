using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private void Validate(string input)
        {
            var reference = Convert.FromBase64String(input);

            var bytes = Encoding.UTF8.GetBytes(input);
            int len = t.TransformBlock(bytes, 0, bytes.Length, outputBuffer, 0);

            len.Should().Be(reference.Length);

            for (int i = 0; i < reference.Length; i++)
            {
                outputBuffer[i].Should().Be(reference[i], $"Difference at {i}");
            }
        }
    }
}

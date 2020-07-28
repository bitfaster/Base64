using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base64.UnitTests
{
	[TestClass]
	public class MemoryStreamExtensionsTests
	{
		[TestMethod]
		public void WriteUtf8AndSetStartMatchesUtf8EncodingGetBytes()
		{
			string test = "foo";
			var referenceBytes = Encoding.UTF8.GetBytes(test);

			using (var s = new MemoryStream())
			{
				s.WriteUtf8AndSetStart(test);

				var writtenBytes = s.ToArray();

				writtenBytes.Should().BeEquivalentTo(referenceBytes);
			}
		}

		[TestMethod]
		public void ReadToBase64MatchesConvertToBase64String()
		{
			string test = "foo";

			// we are trying to replace this code, so verify we produce the same result:
			var rawBytes = Encoding.UTF8.GetBytes(test);
			var convertString = Convert.ToBase64String(rawBytes);

			using (var s = new MemoryStream())
			{
				s.WriteUtf8AndSetStart(test);

				var streamString = s.ReadToBase64();

				streamString.Should().Be(convertString);
			}
		}

		[TestMethod]
		public void WriteFromBase64MatchesConvertFromBase64()
		{
			string input = "foo";
			var utf8Base64String = input.ToUtf8Base64String();

			using (var s = new MemoryStream())
			{
				s.WriteFromBase64(utf8Base64String);
				
				s.Position = 0;

				var streamBase64Bytes = s.ToArray();
				var convertBase64Bytes = Convert.FromBase64String(utf8Base64String);

				streamBase64Bytes.Should().BeEquivalentTo(convertBase64Bytes);

				// also check we can actually decode the bytes back to the original string
				s.TryGetBuffer(out var buffer);
				var decoded = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);

				decoded.Should().Be(input);
			}
		}

		// if we fail and the internal structures are not reset before subsequent calls, we can be in
		// a corrupt state (e.g. temp buffers contain partial data). This test is to check that everything
		// is correctly reset
		[TestMethod]
		public void WriteFromBase64FailThenPass()
		{
			string input = "foo";
			var utf8Base64String = input.ToUtf8Base64String();

			using (var stream = new MemoryStream())
			{
				stream.Invoking(s => s.WriteFromBase64("aaaaa")).Should().Throw<FormatException>();
			}

			using (var s = new MemoryStream())
			{
				s.WriteFromBase64(utf8Base64String);
				
				s.Position = 0;

				var streamBase64Bytes = s.ToArray();
				var convertBase64Bytes = Convert.FromBase64String(utf8Base64String);

				streamBase64Bytes.Should().BeEquivalentTo(convertBase64Bytes);
			}
		}

		[TestMethod]
		public void RFC4648ExamplesFromBase64ProduceExpectedOutput()
		{
			VerifyWriteFromBase64("", "");
			VerifyWriteFromBase64("Zg==", "f");
			VerifyWriteFromBase64("Zm8=", "fo");
			VerifyWriteFromBase64("Zm9v", "foo");
			VerifyWriteFromBase64("Zm9vYg==", "foob");
			VerifyWriteFromBase64("Zm9vYmE=", "fooba");
			VerifyWriteFromBase64("Zm9vYmFy", "foobar");
		}

		[TestMethod]
		public void FromBase64InvalidInputThrows()
		{
			// invalid length
			ValidateInvalidFrom64("a");
			ValidateInvalidFrom64("aa");
			ValidateInvalidFrom64("aaa");
			ValidateInvalidFrom64("aaaaa");
			ValidateInvalidFrom64("aaaaaa");
			ValidateInvalidFrom64("aaaaaaa");

			// invalid char
			ValidateInvalidFrom64("!aaa");

			// invalid padding
			ValidateInvalidFrom64("=aaa");
			ValidateInvalidFrom64("==aa");
			ValidateInvalidFrom64("a===");
			ValidateInvalidFrom64("====");
		}

		private static void VerifyWriteFromBase64(string base64, string expectedAscii)
		{
			using (var s = new MemoryStream())
			{
				s.WriteFromBase64(base64);

				s.Position = 0;

				var streamBase64Bytes = s.ToArray();
				var actual = Encoding.ASCII.GetString(streamBase64Bytes);

				actual.Should().Be(expectedAscii);
			}
		}

		private static void ValidateInvalidFrom64(string input)
		{
			using (var s = new MemoryStream())
			{
				s.Invoking(i => i.WriteFromBase64(input)).Should().Throw<FormatException>();
			}
		}
	}
}

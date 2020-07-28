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
	public class StringExtensionsTests
	{
		[TestMethod]
		public void ToBase64()
		{
			string test = "foo";

			var referenceBytes = Encoding.UTF8.GetBytes(test);
			var convertString = Convert.ToBase64String(referenceBytes);

			var streamString = test.ToUtf8Base64String();

			streamString.Should().Be(convertString);
		}

		[TestMethod]
		public void ToBase64Long()
		{
			// short string cut off is < 160
			string test = new string('a', 160);

			var referenceBytes = Encoding.UTF8.GetBytes(test);
			var convertString = Convert.ToBase64String(referenceBytes);

			var streamString = test.ToUtf8Base64String();

			streamString.Should().Be(convertString);
		}

		[TestMethod]
		public void FromBase64()
		{
			string test = "foo";
			var base64string = test.ToUtf8Base64String();

			var resultBytes = Convert.FromBase64String(base64string);
			var convertString = Encoding.UTF8.GetString(resultBytes);

			var streamString = base64string.FromUtf8Base64String();

			streamString.Should().Be(convertString);
		}

		[TestMethod]
		public void FromBase64Long()
		{
			string test = new string('a', 2048);
			var base64string = test.ToUtf8Base64String();

			var resultBytes = Convert.FromBase64String(base64string);
			var convertString = Encoding.UTF8.GetString(resultBytes);

			var streamString = base64string.FromUtf8Base64String();

			streamString.Should().Be(convertString);
		}

		[TestMethod]
		public void FromBase64InternalSpace()
		{
			string base64string = new string('a', 2016) + "Zm 9v";

			var resultBytes = Convert.FromBase64String(base64string);
			var convertString = Encoding.UTF8.GetString(resultBytes);

			var streamString = base64string.FromUtf8Base64String();

			streamString.Should().Be(convertString);
		}

		[TestMethod]
		public void FromBase64TrailingSpace()
		{
			string base64string = new string('a', 2016) + "Zm9v ";

			var resultBytes = Convert.FromBase64String(base64string);
			var convertString = Encoding.UTF8.GetString(resultBytes);

			var streamString = base64string.FromUtf8Base64String();

			streamString.Should().Be(convertString);
		}

		[TestMethod]
		public void FromBase64LeadingSpace()
		{
			string base64string = " " + new string('a', 2016) + "Zm9v";

			var resultBytes = Convert.FromBase64String(base64string);
			var convertString = Encoding.UTF8.GetString(resultBytes);

			var streamString = base64string.FromUtf8Base64String();

			streamString.Should().Be(convertString);
		}

		[TestMethod]
		[ExpectedException(typeof(System.FormatException))]
		public void FromBase64WrongLength()
		{
			string base64string = new string('a', 2017);

			// The length of a base64 encoded string is always a multiple of 4
			var streamString = base64string.FromUtf8Base64String();

			Console.WriteLine(streamString);
		}

		[TestMethod]
		public void CompareFromUtf8Base64String()
		{
			const int size = 64 * 64;
			char[] alphabet = ("abcdefghijklmnopqrstuvwxyz" + "abcdefghijklmnopqrstuvwxyz".ToUpper() + "0123456789").ToCharArray();

			StringBuilder sb = new StringBuilder(size);

			for (int i = 0; i < size; i++)
			{
				sb.Append(alphabet[i % alphabet.Length]);

				var s2 = sb.ToString().ToUtf8Base64String();

				var resultBytes = Convert.FromBase64String(s2);
				var convertString = Encoding.UTF8.GetString(resultBytes);

				var xStr = s2.FromUtf8Base64String();

				xStr.Should().Be(convertString);
			}
		}

		// if we fail and the internal structures are not reset before subsequent calls, we can be in
		// a corrupt state (e.g. temp buffers contain partial data). This test is to check that everything
		// is correctly reset
		[TestMethod]
		public void FromBase64FailThenPass()
		{
			string test = "foo";
			var base64string = test.ToUtf8Base64String();

			var resultBytes = Convert.FromBase64String(base64string);
			var convertString = Encoding.UTF8.GetString(resultBytes);

			var invalidLength = "aaaaa";

			invalidLength.Invoking(i => i.FromUtf8Base64String()).Should().Throw<FormatException>();

			var streamString = base64string.FromUtf8Base64String();

			streamString.Should().Be(convertString);
		}

		[TestMethod]
		public void RFC4648ExamplesFromBase64ProduceExpectedOutput()
		{
			"".FromUtf8Base64String().Should().Be("");
			"Zg==".FromUtf8Base64String().Should().Be("f");
			"Zm8=".FromUtf8Base64String().Should().Be("fo");
			"Zm9v".FromUtf8Base64String().Should().Be("foo");
			"Zm9vYg==".FromUtf8Base64String().Should().Be("foob");
			"Zm9vYmE=".FromUtf8Base64String().Should().Be("fooba");
			"Zm9vYmFy".FromUtf8Base64String().Should().Be("foobar");
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

		private static void ValidateInvalidFrom64(string input)
		{ 
			input.Invoking(i => i.FromUtf8Base64String()).Should().Throw<FormatException>();
		}
	}
}

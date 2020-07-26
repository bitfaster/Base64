using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64.UnitTests
{
	// special cases
	// space: leading, trailing, inner
	// padding: leading, trailing, inner

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
			// base64string.Length = 2021
			string base64string = " " + new string('a', 2016) + "Zm9v";

			var resultBytes = Convert.FromBase64String(base64string);
			var convertString = Encoding.UTF8.GetString(resultBytes);

			// resultBytes.Length = 1515
			// convertString.Length = 1515

			// when we enter transform block, input count is 2020, not 2021 - why is this?\
			// its because we % input len in buffered writer.

			var streamString = base64string.FromUtf8Base64String();

			streamString.Should().Be(convertString);
		}

		[TestMethod]
		[ExpectedException(typeof(System.FormatException))]
		public void FromBase64WrongLength()
		{
			string base64string = new string('a', 2017);

			// The length of a base64 encoded string is always a multiple of 4

			// throws System.FormatException: Invalid length for a Base-64 char array or string.
			//var resultBytes = Convert.FromBase64String(base64string);
			//var convertString = Encoding.UTF8.GetString(resultBytes);

			var streamString = base64string.FromUtf8Base64String();

			Console.WriteLine(streamString);

			//streamString.Should().Be(convertString);
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
	}
}

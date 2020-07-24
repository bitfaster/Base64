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

				// ReadToBase64, ReadAsBase64
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
				using (s.WriteFromBase64(utf8Base64String))
				{
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
		}
	}
}

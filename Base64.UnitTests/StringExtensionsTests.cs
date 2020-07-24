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
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    public static class StringExtensions
    {
		/// <summary>
		/// Convert large strings to UTF8 base 64 without allocating an intermediate byte array.
		/// </summary>
		/// <param name="input">The input string</param>
		/// <returns>The input string encoded as base64</returns>
		public static string ToUtf8Base64String(this string input)
		{
			// default method is faster for small strings
			if (input.Length < 160)
			{
				var b = Encoding.UTF8.GetBytes(input);
				return Convert.ToBase64String(b);
			}

			var buffer = PooledUtf8EncodingBuffer.GetInstance();

			try
			{
				// write a stream containing the string (now the stream is a byte[] equivalent to Encoding.X.GetBytes(test))
				// Note that if the encoding has BOM, streamwriter will not match Encoding.X.GetBytes(test)
				using (var s = MemoryStreamFactory.Create(nameof(ToUtf8Base64String)))
				{
					using (var w = new BufferedStreamWriter(s, buffer, true))
					{
						w.Write(input);
						w.Flush();
					}

					s.Position = 0;
					return s.ReadToBase64();
				}
			}
			finally
			{
				PooledUtf8EncodingBuffer.Free(buffer);
			}
		}

		// TODO: object pool for buffer
		private static readonly Base64Buffer base64Buffer = new Base64Buffer();

		public static string FromUtf8Base64String(this string base64)
		{
			if (base64.Length < 1024)
			{
				var bytes = Convert.FromBase64String(base64);
				return Encoding.UTF8.GetString(bytes);
			}

			var encodingBuffer = PooledAsciiEncodingBuffer.GetInstance();

			// in actual fact, FromBase64String(String s) does this:
			//unsafe
			//{
			//	fixed (Char* sPtr = s)
			//	{
			//		return FromBase64CharPtr(sPtr, s.Length);
			//	}
			//}
			// so there is no encoding, it is taking the bytes directly, without ASCII encoding
			// can probably make the same thing with an 'encoding buffer' that does this

			try
			{
				using (var s = MemoryStreamFactory.Create(nameof(FromUtf8Base64String)))
				{
					using (var base64Stream = new TransformBase64Stream(s, base64Buffer, true))
					{
						// This should be an ASCII encoded buffer stream.
						// chars = ASCII.GetChars(bytes), then base64
						// This could also explain speed difference - ASCII is faster
						using (var writer = new BufferedStreamWriter(base64Stream, encodingBuffer, true))
						{
							writer.Write(base64);
							writer.Flush();
						}
					}

					s.TryGetBuffer(out var buffer);
					return Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
				}
			}
			finally
			{
				PooledAsciiEncodingBuffer.Free(encodingBuffer);
			}
		}
	}
}

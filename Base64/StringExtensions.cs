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

			var utf8Buffer = PooledUtf8EncodingBuffer.GetInstance();

			try
			{
				// write a stream containing the string (now the stream is a byte[] equivalent to Encoding.X.GetBytes(test))
				// Note that if the encoding has BOM, streamwriter will not match Encoding.X.GetBytes(test)
				using (var s = MemoryStreamFactory.Create(nameof(ToUtf8Base64String)))
				{
					using (var w = new BufferedStreamWriter(s, utf8Buffer, true))
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
				PooledUtf8EncodingBuffer.Free(utf8Buffer);
			}
		}

		public static string FromUtf8Base64String(this string base64)
		{
			if (base64.Length < 1024)
			{
				var bytes = Convert.FromBase64String(base64);
				return Encoding.UTF8.GetString(bytes);
			}

			// this should actually be ASCII, but UTF8 is faster.
			var utf8Buffer = PooledUtf8EncodingBuffer.GetInstance();
			var base64Buffer = PooledBase64Buffer.GetInstance();

			try
			{
				using (var s = MemoryStreamFactory.Create(nameof(FromUtf8Base64String)))
				{
					using (var base64Stream = new TransformBase64Stream(s, base64Buffer, true))
					{
						using (var writer = new BufferedStreamWriter(base64Stream, utf8Buffer, true))
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
				PooledUtf8EncodingBuffer.Free(utf8Buffer);
				PooledBase64Buffer.Free(base64Buffer);
			}
		}
	}
}

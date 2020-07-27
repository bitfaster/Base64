using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
	public static class MemoryStreamExtensions
	{
		/// <summary>
		/// Utility method to replace new MemoryStream(Encoding.UTF8.GetBytes(str)) with a direct stream write.
		/// </summary>
		/// <param name="stream">The stream to write to</param>
		/// <param name="str">The string to write</param>
		/// <returns>The stream writer used</returns>
		public static StreamWriter WriteUtf8AndSetStart(this MemoryStream stream, string str)
		{
			var writer = new StreamWriter(stream, NoBomEncoding.UTF8);
			writer.Write(str);
			writer.Flush();
			stream.Position = 0;

			return writer;
		}

		public static string ReadUtf8(this MemoryStream stream)
		{
			stream.TryGetBuffer(out var buffer);
			return Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
		}

		public static void WriteFromBase64(this Stream stream, string base64)
		{
			var asciiBuffer = PooledAsciiEncodingBuffer.GetInstance();
			var base64Buffer = PooledBase64Buffer.GetInstance();

			try
			{
				using (var base64Stream = new TransformBase64Stream(stream, base64Buffer, true))
				{
					using (var writer = new BufferedStreamWriter(base64Stream, asciiBuffer, true))
					{
						writer.Write(base64);
						writer.Flush();
					}
				}
			}
			finally
			{
				PooledAsciiEncodingBuffer.Free(asciiBuffer);
				PooledBase64Buffer.Free(base64Buffer);
			}
		}

		public static string ReadToBase64(this MemoryStream stream)
		{
			stream.TryGetBuffer(out var buffer);
			return Convert.ToBase64String(buffer.Array, buffer.Offset, buffer.Count);
		}
	}

	public static class NoBomEncoding
	{
		public static readonly Encoding ASCII = new ASCIIEncoding();
		public static readonly Encoding UTF8 = new UTF8Encoding(false);
		public static readonly Encoding Unicode = new UnicodeEncoding(false, false);
	}
}

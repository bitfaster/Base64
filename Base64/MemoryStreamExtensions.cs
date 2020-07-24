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

		public static void WriteFromBase64(this Stream stream, string str)
		{
			var buffer = PooledUtf8EncodingBuffer.GetInstance();

			try
			{
				// Need to be on Framework 4.72+ to have the leaveopen param in CryptoStream
				// until then, return the crypto stream, so that the caller can manage the lifetime
				CryptoStream base64Stream = new CryptoStream(stream, new System.Security.Cryptography.FromBase64Transform(), CryptoStreamMode.Write);

				using (var writer = new BufferedStreamWriter(base64Stream, buffer, true))
				{
					writer.Write(str);
					writer.Flush();
				}

				base64Stream.FlushFinalBlock();
			}
			finally
			{
				PooledUtf8EncodingBuffer.Free(buffer);
			}
		}

		public static string ReadToBase64(this MemoryStream stream)
		{
			stream.TryGetBuffer(out var buffer);
			return Convert.ToBase64String(buffer.Array, buffer.Offset, buffer.Count);
		}

		/// <summary>
		/// Convert large strings to UTF8 base 64 without allocating an intermediate byte array.
		/// </summary>
		/// <param name="input">The input string</param>
		/// <returns>The input string encoded as base64</returns>
		//public static string ToUtf8Base64String(this string input)
		//{
		//	return ToBase64String(input, NoBomEncoding.UTF8);
		//}

		/// <summary>
		/// Convert large strings to UTF8 base 64 without allocating an intermediate byte array.
		/// </summary>
		/// <param name="input">The input string</param>
		/// <returns>The input string encoded as base64</returns>
		//public static string ToUnicodeBase64String(this string input)
		//{
		//	return ToBase64String(input, NoBomEncoding.Unicode);
		//}

		//private static string ToBase64String(string input, Encoding encoding)
		//{
		//	// default method is faster for small strings
		//	if (input.Length < 160)
		//	{
		//		var b = Encoding.UTF8.GetBytes(input);
		//		return Convert.ToBase64String(b);
		//	}

		//	var buffer = PooledUtf8EncodingBuffer.GetInstance();

		//	try
		//	{
		//		// write a stream containing the string (now the stream is a byte[] equivalent to Encoding.X.GetBytes(test))
		//		// Note that if the encoding has BOM, streamwriter will not match Encoding.X.GetBytes(test)
		//		using (var s = MemoryStreamFactory.Create(nameof(ToBase64String)))
		//		{
		//			using (var w = new BufferedStreamWriter(s, buffer, true))
		//			{
		//				w.Write(input);
		//				w.Flush();
		//			}

		//			s.Position = 0;

		//			return s.ReadToBase64();
		//		}
		//	}
		//	finally
		//	{
		//		PooledUtf8EncodingBuffer.Free(buffer);
		//	}
		//}

		//public static string FromUtf8Base64String(this string base64)
		//{
		//	var encodingBuffer = PooledUtf8EncodingBuffer.GetInstance();

		//	try
		//	{
		//		using (var s = MemoryStreamFactory.Create(nameof(FromUtf8Base64String)))
		//		using (CryptoStream base64Stream = new CryptoStream(s, new System.Security.Cryptography.FromBase64Transform(), CryptoStreamMode.Write))
		//		{
		//			using (var writer = new BufferedStreamWriter(base64Stream, encodingBuffer, true))
		//			{
		//				writer.Write(base64);
		//				writer.Flush();
		//			}

		//			base64Stream.FlushFinalBlock();

		//			s.TryGetBuffer(out var buffer);
		//			return encodingBuffer.Encoding.GetString(buffer.Array, buffer.Offset, buffer.Count);
		//		}
		//	}
		//	finally
		//	{
		//		PooledUtf8EncodingBuffer.Free(encodingBuffer);
		//	}
		//}

		// TODO: object pool for buffer
		//private static readonly Base64Buffer base64Buffer = new Base64Buffer();

		//public static string FromUtf8Base64String2(this string base64)
		//{
		//	if (base64.Length < 1024)
		//	{
		//		var bytes = Convert.FromBase64String(base64);
		//		return Encoding.UTF8.GetString(bytes);
		//	}

		//	var encodingBuffer = PooledUtf8EncodingBuffer.GetInstance();

		//	try
		//	{
		//		using (var s = MemoryStreamFactory.Create(nameof(FromUtf8Base64String)))
		//		{
		//			using (var base64Stream = new TransformBase64Stream(s, base64Buffer, true))
		//			{
		//				using (var writer = new BufferedStreamWriter(base64Stream, encodingBuffer, true))
		//				{
		//					writer.Write(base64);
		//					writer.Flush();
		//				}
		//			}

		//			s.TryGetBuffer(out var buffer);
		//			return encodingBuffer.Encoding.GetString(buffer.Array, buffer.Offset, buffer.Count);
		//		}
		//	}
		//	finally
		//	{
		//		PooledUtf8EncodingBuffer.Free(encodingBuffer);
		//	}
		//}

		//private static int EstimateBufferSize(Encoding encoding, string input)
		//{
		//	// don't allocate large buffers for small strings
		//	return Math.Min(encoding.GetMaxByteCount(input.Length), 512);
		//}
	}

	public static class NoBomEncoding
	{
		public static readonly Encoding UTF8 = new UTF8Encoding(false);
		public static readonly Encoding Unicode = new UnicodeEncoding(false, false);
	}
}

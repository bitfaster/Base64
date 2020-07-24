using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
	public class PooledUtf8EncodingBuffer
	{
		private static readonly ObjectPool<EncodingBuffer> PoolInstance = CreatePool();

		public const int BufferSize = 4096;

		private static ObjectPool<EncodingBuffer> CreatePool()
		{
			return new ObjectPool<EncodingBuffer>(() => new EncodingBuffer(NoBomEncoding.UTF8, BufferSize));
		}

		public static EncodingBuffer GetInstance()
		{
			return PoolInstance.Allocate();
		}

		public static void Free(EncodingBuffer builder)
		{
			builder.Encoder.Reset();

			PoolInstance.Free(builder);
		}
	}
}

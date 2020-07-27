using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    public class PooledBase64Buffer
    {
		private static readonly ObjectPool<Base64Buffer> PoolInstance = CreatePool();

		public const int BufferSize = 4096;

		private static ObjectPool<Base64Buffer> CreatePool()
		{
			return new ObjectPool<Base64Buffer>(() => new Base64Buffer());
		}

		public static Base64Buffer GetInstance()
		{
			return PoolInstance.Allocate();
		}

		public static void Free(Base64Buffer buffer)
		{
			buffer.Reset();

			PoolInstance.Free(buffer);
		}
	}
}

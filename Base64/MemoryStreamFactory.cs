using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.Remoting.Messaging;
	using System.Text;
	using System.Threading.Tasks;
	using System.Web;
	using Microsoft.IO;
	using static Microsoft.IO.RecyclableMemoryStreamManager;

	public static class MemoryStreamFactory
	{
		private const int DefaultBlockSize = RecyclableMemoryStreamManager.DefaultBlockSize;
		private const int DefaultLargeBufferMultiple = 1 << 20;
		private const int DefaultMaximumBufferSize = 16 * (1 << 20); // 16mb

		private static RecyclableMemoryStreamManager streamManager = CreateStreamManager();

		private const int reportFrequency = 10000;
		private static long sampleCount = 0;

		public static MemoryStream Create(string tag)
		{
			if (UseRecycledMemoryStream)
			{
				return streamManager.GetStream(tag);
			}

			return new MemoryStream();
		}

		private static RecyclableMemoryStreamManager CreateStreamManager()
		{
			var rmsm = new RecyclableMemoryStreamManager(
				DefaultBlockSize,
				DefaultLargeBufferMultiple,
				DefaultMaximumBufferSize,
				useExponentialLargeBuffer: true)
			{
				// Calling stream.GetBuffer/TryGetBuffer can switch from multiple small buffers to 1 large buffer.
				// This setting forces the small buffers to be returned to the pool immediately once a stream switches
				// to large buffer mode.
				AggressiveBufferReturn = true,
			};


			return rmsm;
		}

		private static bool UseRecycledMemoryStream = true;
	}
}

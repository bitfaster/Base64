using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    public class EncodingBuffer
    {
        public EncodingBuffer(Encoding encoding, int bufferSize)
        {
            this.Encoding = encoding;
            this.Encoder = encoding.GetEncoder();
            this.CharBuffer = new char[bufferSize];
            this.ByteBuffer = new byte[encoding.GetMaxByteCount(bufferSize)];
        }

        public Encoding Encoding { get; }

        public Encoder Encoder { get; }

        public byte[] ByteBuffer { get; }

        public char[] CharBuffer { get; }
    }
}

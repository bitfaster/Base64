using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    public class Base64Buffer
    {
        public Base64Buffer()
        {
            this.FromBase64Transform = new FromBase64TransformWithWhiteSpace();
            this.InputBlock = new byte[this.FromBase64Transform.InputBlockSize];
            this.OutputBlock = new byte[this.FromBase64Transform.OutputBlockSize];
            this.OutputBuffer = new byte[this.FromBase64Transform.ChunkSize];
        }

        public IBase64Transform FromBase64Transform { get; }

        public byte[] InputBlock { get; }

        public byte[] OutputBlock { get; }

        public byte[] OutputBuffer { get; }

        public void Reset()
        {
            Array.Clear(this.InputBlock, 0, this.InputBlock.Length);
            Array.Clear(this.OutputBlock, 0, this.OutputBlock.Length);
        }
    }
}

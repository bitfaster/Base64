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
            this.FinalInputBlock = new byte[this.FromBase64Transform.InputBlockSize];
            this.Block = new Block(this.FromBase64Transform.InputBlockSize, this.FromBase64Transform.ChunkSize);
        }

        public IBase64Transform FromBase64Transform { get; }

        public byte[] FinalInputBlock { get; }

        public Block Block { get; }

        public void Reset()
        {
            this.FromBase64Transform.Reset();
            this.Block.Reset();
        }
    }
}

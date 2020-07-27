using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    public interface IBase64Transform
    {
        int ChunkSize { get; }

        int InputBlockSize { get; }

        int OutputBlockSize { get; }

        int TransformBlock(Block block);

        int TransformFinalBlock(Block block);

        void Reset();
    }
}
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
    }

    public class Block
    {
        public byte[] input;
        public int inputOffset;
        public int inputCount;

        public byte[] outputBuffer;
        public int outputOffset;

        public Block(int inputBlockSize, int chunkSize)
        {
            this.input = new byte[inputBlockSize];
            this.outputBuffer = new byte[chunkSize];
        }

        public void Validate()
        {
            return;
            if (input == null) throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", "ArgumentOutOfRange_NeedNonNegNum");
            if (inputCount < 0 || (inputCount > input.Length)) throw new ArgumentException("Argument_InvalidValue");
            if ((input.Length - inputCount) < inputOffset) throw new ArgumentException("Argument_InvalidOffLen");
        }

        public void Reset()
        {
            this.inputOffset = this.inputCount = this.outputOffset = 0;
        }
    }
}
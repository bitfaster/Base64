using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
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
            if (input == null) throw new ArgumentNullException("input");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", "inputOffset must be positive");
            if (inputCount < 0 || (inputCount > input.Length)) throw new ArgumentException("inputCount cannot be greater than input length");
            if ((input.Length - inputCount) < inputOffset) throw new ArgumentException("Invalid offset. offset + inputCount is beyond the input array bounds.");
        }

        public void Reset()
        {
            this.inputOffset = this.inputCount = this.outputOffset = 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    public class FasterFromBase64Transform : IBase64Transform
    {
        // property access adds 0.13% according to profiler, every little helps :)
        private static readonly Encoding ASCII = Encoding.ASCII;

        const int bufferSize = 4096;

        // this cannot be on the stack. Can be eliminated if we do not need to trim whitespace
        private byte[] inputBuffer = new byte[bufferSize];

        private byte[] tempByteBuffer = new byte[bufferSize];
        private int inputIndex;

        // this can be stackalloc
        private char[] tempCharBuffer = new char[bufferSize];

        // Constructors
        public FasterFromBase64Transform()
        {
            inputIndex = 0;
        }

        public int ChunkSize => bufferSize;

        // converting from Base64 generates 3 bytes output from each 4 bytes input block
        public int InputBlockSize
        {
            get { return (4); }
        }

        public int OutputBlockSize
        {
            get { return (3); }
        }

        public bool CanTransformMultipleBlocks
        {
            get { return (false); }
        }

        public virtual bool CanReuseTransform
        {
            get { return (true); }
        }

        // Buffered stream writer guaratees that we can only write with full buffer
        // then call write transform final block to flush
        public unsafe int TransformBlock(Block block)
        {
            // Do some validation
            block.Validate();

            if (block.inputCount + inputIndex < 4)
            {
                // copy input to _inputBuffer for final block
                Buffer.BlockCopy(this.tempByteBuffer, 0, this.inputBuffer, inputIndex, block.inputCount);
                inputIndex += block.inputCount;
                return 0;
            }

            // Get the number of 4 bytes blocks to transform
            int numBlocks = (block.inputCount + inputIndex) / 4;

            // copy remainder to _inputBuffer for final block
            inputIndex = (block.inputCount + inputIndex) % 4;
            Buffer.BlockCopy(block.input, block.inputCount - inputIndex, this.inputBuffer, 0, inputIndex);

            int nWritten = 0;

            fixed (char* cp = tempCharBuffer)
            fixed (byte* ip = block.input)
            {
                nWritten = ASCII.GetChars(ip, 4 * numBlocks, cp, bufferSize);
            }

            // write chars directly to outputBuffer
            fixed (byte* op = block.outputBuffer)
            {
                return UnsafeConvert.FromBase64CharArray(tempCharBuffer, 0, nWritten, op + block.outputOffset);
            }
        }

        public unsafe int TransformFinalBlock(Block block)
        {
            // Do some validation
            block.Validate();

            int effectiveCount;

            Buffer.BlockCopy(block.input, block.inputOffset, tempByteBuffer, 0, block.inputCount);
            effectiveCount = block.inputCount;

            if (effectiveCount + inputIndex < 4)
            {
                Reset();
                return 0;
            }

            // Get the number of 4 byte blocks to transform
            int numBlocks = (effectiveCount + inputIndex) / 4;

            byte* transformBuffer = stackalloc byte[inputIndex + effectiveCount];

            fixed (byte* ip = this.inputBuffer, tp = tempByteBuffer)
            {
                Buffer.MemoryCopy(ip, transformBuffer, inputIndex, inputIndex);
                Buffer.MemoryCopy(tp, transformBuffer + inputIndex, effectiveCount, effectiveCount);
            }

            inputIndex = (effectiveCount + inputIndex) % 4;
            Buffer.BlockCopy(tempByteBuffer, effectiveCount - inputIndex, this.inputBuffer, 0, inputIndex);

            int nWritten = 0;

            fixed (char* cp = tempCharBuffer)
            {
                nWritten = ASCII.GetChars(transformBuffer, 4 * numBlocks, cp, 16);
            }

            // reinitialize the transform
            Reset();

            // write chars directly to outputBuffer
            fixed (byte* op = block.outputBuffer)
            {
                return UnsafeConvert.FromBase64CharArray(tempCharBuffer, 0, nWritten, op + block.outputOffset);
            }
        }

        private int DiscardWhiteSpaces2(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            int effectiveCount = inputCount;
            int i, iCount = 0;

            for (i = 0; i < inputCount; i++)
                if (Char.IsWhiteSpace((char)inputBuffer[inputOffset + i]))
                    effectiveCount--;
                else
                    tempByteBuffer[iCount++] = inputBuffer[inputOffset + i];

            return effectiveCount;
        }

        // Reset the state of the transform so it can be used again
        private void Reset()
        {
            inputIndex = 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    public class FromBase64Transform : IBase64Transform
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

        private FromBase64TransformMode _whitespaces;

        // Constructors
        public FromBase64Transform()
            : this(FromBase64TransformMode.IgnoreWhiteSpaces)
        {
        }

        public FromBase64Transform(FromBase64TransformMode whitespaces)
        {
            _whitespaces = whitespaces;
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

        // Crux of this is to take the input buffer, make it into a multiple of block size, ASCII endcode the bytes, then call FromBase64CharArray
        // which does the meaty work.

        // There still a lot of data copying here for:
        // 1. Remove intermediate white space - do we need it? If we don't, we can skip some buffer juggling.
        // 2. Partially written input (input is not a multiple of block size)
        // - is this needed? Can we constrain so that you must write a whole block, and remainder must come from WriteFinalBlock?
        //   That way, we do not need to copy inputBuffer => _inputBuffer => transformBuffer


        public unsafe int TransformBlock(Block block)
        {
            // if (inputCount != bufferSize)
            //     throw new ArgumentOutOfRangeException("inputCount", "ArgumentOutOfRange_NeedNonNegNum");

            // Do some validation
            block.Validate();

            int effectiveCount;

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            // TODO it looks like the convert function we call into ignores whitespace, so this could be skipped
            // test what happens if input contains whitespace. Only works for trailing whitespace.
            //if (_whitespaces == FromBase64TransformMode.IgnoreWhiteSpaces) 
            //{
            //    effectiveCount = DiscardWhiteSpaces2(inputBuffer, inputOffset, inputCount);
            //} 
            //else 
            //{
            Buffer.BlockCopy(block.input, block.inputOffset, this.tempByteBuffer, 0, block.inputCount);
            effectiveCount = block.inputCount;
            //}            

            if (effectiveCount + inputIndex < 4)
            {
                // copy input to _inputBuffer for next pass
                Buffer.BlockCopy(this.tempByteBuffer, 0, this.inputBuffer, inputIndex, effectiveCount);
                inputIndex += effectiveCount;
                return 0;
            }

            // Get the number of 4 bytes blocks to transform
            int numBlocks = (effectiveCount + inputIndex) / 4;

            // combine previous input with new input in transformBuffer
            // this is potentially a 4096 element array on the stack
            byte* transformBuffer = stackalloc byte[inputIndex + effectiveCount];

            // Can we skip copying from input buffer to transform buffer?
            // if the input buffer is empty, we can just use tempByteBuffer directly.

            fixed (byte* ip = this.inputBuffer, tp = this.tempByteBuffer)
            {
                Buffer.MemoryCopy(ip, transformBuffer, inputIndex, inputIndex);
                Buffer.MemoryCopy(tp, transformBuffer + inputIndex, effectiveCount, effectiveCount);
            }

            // copy remainder to _inputBuffer for later/final block
            inputIndex = (effectiveCount + inputIndex) % 4;
            Buffer.BlockCopy(this.tempByteBuffer, effectiveCount - inputIndex, this.inputBuffer, 0, inputIndex);

            int nWritten = 0;

            fixed (char* cp = tempCharBuffer)
            {
                nWritten = ASCII.GetChars(transformBuffer, 4 * numBlocks, cp, bufferSize);
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

            if (_whitespaces == FromBase64TransformMode.IgnoreWhiteSpaces)
            {
                effectiveCount = DiscardWhiteSpaces2(block.input, block.inputOffset, block.inputCount);
            }
            else
            {
                Buffer.BlockCopy(block.input, block.inputOffset, tempByteBuffer, 0, block.inputCount);
                effectiveCount = block.inputCount;
            }

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
                nWritten = Encoding.ASCII.GetChars(transformBuffer, 4 * numBlocks, cp, 16);
            }

            //byte[] tempBytes = Convert.FromBase64CharArray(tempCharBuffer, 0, nWritten);

            //// reinitialize the transform
            //Reset();
            //return tempBytes;

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

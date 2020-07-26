using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{

    public class FromBase64TransformWithWhiteSpace : IBase64Transform
    {
        // property access adds 0.13% according to profiler, every little helps :)
        private static readonly Encoding ASCII = Encoding.ASCII;

        const int bufferSize = 4096;

        // this cannot be on the stack. Can be eliminated if we do not need to trim whitespace
        private byte[] inputBuffer = new byte[bufferSize];

        private byte[] tempByteBuffer = new byte[4];
        private int tempCount;
        private int inputIndex;

        // this can be stackalloc
        private char[] tempCharBuffer = new char[bufferSize];

        // Constructors
        public FromBase64TransformWithWhiteSpace()
        {
            inputIndex = 0;
        }

        public int ChunkSize => bufferSize;

        // converting from Base64 generates 3 bytes output from each 4 bytes input block
        public int InputBlockSize
        {
            get { return 4; }
        }

        public int OutputBlockSize
        {
            get { return 3; }
        }

        // This will result in us allowing some invalid non-whitespace chars, but it is fast.
        // The invalid chars will be blocked later by UnsafeConvert.FromBase64CharArray
        const uint intSpace = (uint)' ';

        // This is optimized for the case where there are either no whitespace chars, or very few.
        // Try to process continuous arrays of data. If there is a whitespace, process everything before in a continuous array.
        // Then try to find a valid 4 byte section and process it. Then continue to find and process continuous arrays.

        public unsafe int TransformBlock(byte[] input, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (inputOffset < 0) throw new ArgumentOutOfRangeException(nameof(inputOffset), "ArgumentOutOfRange_NeedNonNegNum");
            if (inputCount < 0 || (inputCount > input.Length)) throw new ArgumentException("Argument_InvalidValue");
            if ((input.Length - inputCount) < inputOffset) throw new ArgumentException("Argument_InvalidOffLen");

            int effectiveCount;
            int totalOutputBytes = 0;

            fixed (byte* inputPtr = input) 
            {
                byte* startPtr = (byte*)(inputPtr + inputOffset);
                byte* endMarkerPtr = (byte*)(inputPtr + inputOffset + inputCount);
                byte* endPtr = startPtr;

                while (endPtr < endMarkerPtr)
                {
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // find ptrs to a section of the array that doesn't contain spaces

                    startPtr = SkipSpaces(startPtr, endMarkerPtr);

                    // we examined a section containing only whitespace, exit
                    if (startPtr >= endMarkerPtr)
                    {
                        break;
                    }

                    endPtr = NextSpaceOrEnd(startPtr, endMarkerPtr);

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Get the number of 4 bytes blocks to transform
                    effectiveCount = (int)(endPtr - startPtr);
                    int numBlocks2 = (effectiveCount) / 4;

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Transform the bytes to chars and write to output

                    int nWritten = 0;
                    fixed (char* cp = tempCharBuffer)
                    {
                        nWritten = ASCII.GetChars(startPtr, 4 * numBlocks2, cp, bufferSize);
                    }

                    fixed (byte* op = outputBuffer)
                    {
                        int outputBytes = UnsafeConvert.FromBase64CharArray(tempCharBuffer, 0, nWritten, op + outputOffset);

                        outputOffset += outputBytes;
                        totalOutputBytes += outputBytes;
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////
                    // Try to take 4 bytes starting from the last space.

                    int remainder = (effectiveCount) % 4;

                    if (remainder != 0)
                    {
                        // We need 4 bytes total, try to find remainder bytes after the whitespace
                        byte* tmpPtr = startPtr + effectiveCount - remainder;
                        var temp = stackalloc byte[4];

                        int bytesFound = 0;
                        while (tmpPtr < endMarkerPtr && bytesFound < 4)
                        {
                            uint c = (uint)(*tmpPtr);

                            if (c > intSpace)
                                temp[bytesFound++] = *tmpPtr;

                            tmpPtr++;
                        }

                        if (bytesFound == 4)
                        {
                            // we have 4 bytes, write it to output
                            fixed (char* cp = tempCharBuffer)
                            {
                                nWritten = ASCII.GetChars(temp, 4, cp, bufferSize);
                            }

                            fixed (byte* op = outputBuffer)
                            {
                                int outputBytes = UnsafeConvert.FromBase64CharArray(tempCharBuffer, 0, nWritten, op + outputOffset);

                                outputOffset += outputBytes;
                                totalOutputBytes += outputBytes;
                            }

                            // advance startPtr past the block just written
                            startPtr = tmpPtr;
                        }
                        else
                        {
                            // Must be the final block, store remaining bytes in the temp buffer
                            fixed (byte* tp = this.tempByteBuffer)
                            {
                                Buffer.MemoryCopy(temp, tp, 4, bytesFound);
                            }

                            tempCount = bytesFound;
                            break;
                        }
                    }
                    else
                    {
                        // inputIndex = remainder;
                        startPtr = endPtr;
                    }
                }

                return totalOutputBytes;
            }
        }

        private static unsafe byte* NextSpaceOrEnd(byte* startPtr, byte* endMarkerPtr)
        {
            byte* endPtr = startPtr;
            while (endPtr < endMarkerPtr)
            {
                uint c = (uint)(*endPtr);

                if (c <= intSpace)
                    break;

                endPtr++;
            }

            return endPtr;
        }

        public unsafe int TransformFinalBlock(byte[] input, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (input == null) throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", "ArgumentOutOfRange_NeedNonNegNum");
            if (inputCount < 0 || (inputCount > input.Length)) throw new ArgumentException("Argument_InvalidValue");
            if ((input.Length - inputCount) < inputOffset) throw new ArgumentException("Argument_InvalidOffLen");

            int totalOutputBytes = 0;
            int nWritten = 0;

            // probably this becomes a sub method that can be run at the start of the other block method
            if (this.tempCount > 0)
            {
                // try to write a full block starting with remainder of last write
                byte* tempBytes = stackalloc byte[4];

                fixed (byte* tp = this.tempByteBuffer)
                {
                    Buffer.MemoryCopy(tp, tempBytes, 4, this.tempCount);
                }

                fixed (byte* inputPtr = input)
                {
                    byte* startPtr = (byte*)(inputPtr + inputOffset);
                    byte* endMarkerPtr = (byte*)(inputPtr + inputOffset + inputCount);
                    byte* endPtr = startPtr;

                    startPtr = SkipSpaces(startPtr, endMarkerPtr);

                    int bytesNeeded = 4 - this.tempCount;

                    while (bytesNeeded > 0 && startPtr < endMarkerPtr)
                    {
                        uint c = (uint)(*startPtr);

                        if (c > intSpace)
                        {
                            tempBytes[this.tempCount++] = *startPtr;
                            bytesNeeded--;
                        }

                        startPtr++;
                    }

                    if (bytesNeeded != 0)
                    {
                        throw new FormatException("Invalid base64 data length");
                    }

                    // we have 4 bytes, write it to output
                    fixed (char* cp = tempCharBuffer)
                    {
                        nWritten = ASCII.GetChars(tempBytes, 4, cp, bufferSize);
                    }

                    fixed (byte* op = outputBuffer)
                    {
                        int outputBytes = UnsafeConvert.FromBase64CharArray(tempCharBuffer, 0, nWritten, op + outputOffset);

                        outputOffset += outputBytes;
                        totalOutputBytes += outputBytes;
                    }

                    // advance offset
                    int count = (int)(startPtr - (byte*)(inputPtr + inputOffset));
                    inputOffset += count;
                    inputCount -= count;
                }
            }

            int effectiveCount;

            Buffer.BlockCopy(input, inputOffset, tempByteBuffer, 0, inputCount);
            effectiveCount = inputCount;

            if (effectiveCount + inputIndex < 4)
            {
                Reset();
                return totalOutputBytes;
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


            fixed (char* cp = tempCharBuffer)
            {
                nWritten = ASCII.GetChars(transformBuffer, 4 * numBlocks, cp, 16);
            }

            Reset();

            // write chars directly to outputBuffer
            fixed (byte* op = outputBuffer)
            {
                return totalOutputBytes + UnsafeConvert.FromBase64CharArray(tempCharBuffer, 0, nWritten, op + outputOffset);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe byte* SkipSpaces(byte* startPtr, byte* endMarkerPtr)
        {
            while (startPtr < endMarkerPtr)
            {
                uint c = (uint)(*startPtr);

                if (c > intSpace)
                    break;

                startPtr++;
            }

            return startPtr;
        }

        // Reset the state of the transform so it can be used again
        private void Reset()
        {
            this.tempCount = 0;
            inputIndex = 0;
        }
    }
}

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
    public class FromBase64Transform
    {
        // property access adds 0.13% according to profiler, every little helps :)
        private static readonly Encoding ASCII = Encoding.ASCII;
        const int bufferSize = 4096;
        private int tempCount;

        private byte[] tempByteBuffer = new byte[4];
        private char[] tempCharBuffer = new char[bufferSize];

        public FromBase64Transform()
        {
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
        public unsafe int TransformBlock(Block block)
        {
            block.Validate();

            int effectiveCount;
            int totalOutputBytes = 0;

            if (this.tempCount > 0)
            {
                totalOutputBytes += this.FlushTempBuffer(block);
            }

            fixed (byte* inputPtr = block.input) 
            {
                byte* startPtr = (byte*)(inputPtr + block.inputOffset);
                byte* endMarkerPtr = (byte*)(inputPtr + block.inputOffset + block.inputCount);
                byte* endPtr = startPtr;

                while (endPtr < endMarkerPtr)
                {
                    // find ptrs to a section of the array that doesn't contain spaces
                    startPtr = SkipSpaces(startPtr, endMarkerPtr);

                    // if we examined a section containing only whitespace, exit
                    if (startPtr >= endMarkerPtr)
                    {
                        break;
                    }

                    endPtr = NextSpaceOrEnd(startPtr, endMarkerPtr);

                    // Get the number of 4 bytes blocks to transform
                    effectiveCount = (int)(endPtr - startPtr);
                    int numBlocks = (effectiveCount) / 4;

                    // Transform the bytes to chars and write to output
                    int nWritten = 0;
                    fixed (char* cp = tempCharBuffer)
                    {
                        nWritten = ASCII.GetChars(startPtr, 4 * numBlocks, cp, bufferSize);
                    }

                    fixed (byte* op = block.outputBuffer)
                    {
                        int outputBytes = UnsafeConvert.FromBase64CharArray(tempCharBuffer, 0, nWritten, op + block.outputOffset);

                        block.outputOffset += outputBytes;
                        totalOutputBytes += outputBytes;
                    }

                    // If we couldn't write a complete block, try to find 4 bytes starting from the last space.
                    int remainder = effectiveCount % 4;

                    if (remainder != 0)
                    {
                        // We need 4 bytes total, try to find remainder bytes after the whitespace
                        byte* tmpPtr = startPtr + effectiveCount - remainder;

                        int bytesFound = 0;
                        while (tmpPtr < endMarkerPtr && bytesFound < 4)
                        {
                            uint c = (uint)(*tmpPtr);

                            if (c > intSpace)
                                this.tempByteBuffer[bytesFound++] = *tmpPtr;

                            tmpPtr++;
                        }

                        if (bytesFound == 4)
                        {
                            // we have 4 bytes, write it to output
                            fixed (char* cp = tempCharBuffer)
                            fixed (byte* tp = this.tempByteBuffer)
                            {
                                nWritten = ASCII.GetChars(tp, 4, cp, bufferSize);
                            }

                            fixed (byte* op = block.outputBuffer)
                            {
                                int outputBytes = UnsafeConvert.FromBase64CharArray(tempCharBuffer, 0, nWritten, op + block.outputOffset);

                                block.outputOffset += outputBytes;
                                totalOutputBytes += outputBytes;
                            }

                            // advance startPtr past the block just written
                            startPtr = tmpPtr;
                        }
                        else
                        {
                            // Must be the final block, mark remaining bytes in the temp buffer
                            tempCount = bytesFound;
                            break;
                        }
                    }
                    else
                    {
                        startPtr = endPtr;
                    }
                }

                return totalOutputBytes;
            }
        }

        public unsafe int TransformFinalBlock(Block block)
        {
            block.Validate();

            int totalOutputBytes = 0;

            // probably this becomes a sub method that can be run at the start of the other block method
            if (this.tempCount > 0)
            {
                totalOutputBytes = this.FlushTempBuffer(block);
            }

            if (block.inputCount == 0)
            {
                return totalOutputBytes;
            }

            if (block.inputCount < 4)
            {
                // handle trailing whitespace
                for (int i = block.inputOffset; i < block.inputOffset + block.inputCount; i++)
                {
                    if (block.input[i] != (int)' ' && block.input[i] != (int)'\n' && block.input[i] != (int)'\r' && block.input[i] != (int)'\t' && block.input[i] != (int)'\0')
                    {
                        throw new FormatException("Invalid length for a Base-64 char array or string");
                    }  
                }

                return totalOutputBytes;
            }

            throw new InvalidOperationException("Too much data written in final block");
        }

        private unsafe int FlushTempBuffer(Block block)
        {
            int totalOutputBytes = 0;

            // try to write a full block starting with remainder of last write
            fixed (byte* inputPtr = block.input)
            {
                byte* startPtr = (byte*)(inputPtr + block.inputOffset);
                byte* endMarkerPtr = (byte*)(inputPtr + block.inputOffset + block.inputCount);

                startPtr = SkipSpaces(startPtr, endMarkerPtr);

                int bytesNeeded = 4 - this.tempCount;

                while (bytesNeeded > 0 && startPtr < endMarkerPtr)
                {
                    uint c = (uint)(*startPtr);

                    if (c > intSpace)
                    {
                        this.tempByteBuffer[this.tempCount++] = *startPtr;
                        bytesNeeded--;
                    }

                    startPtr++;
                }

                if (bytesNeeded != 0)
                {
                    throw new FormatException("Invalid base64 data length");
                }

                int nWritten = 0;
                this.tempCount = 0;

                // we have 4 bytes, write it to output
                fixed (char* cp = tempCharBuffer)
                fixed (byte* tp = this.tempByteBuffer)
                {
                    nWritten = ASCII.GetChars(tp, 4, cp, bufferSize);
                }

                fixed (byte* op = block.outputBuffer)
                {
                    int outputBytes = UnsafeConvert.FromBase64CharArray(tempCharBuffer, 0, nWritten, op + block.outputOffset);

                    block.outputOffset += outputBytes;
                    totalOutputBytes += outputBytes;
                }

                // advance offset
                int count = (int)(startPtr - (byte*)(inputPtr + block.inputOffset));
                block.inputOffset += count;
                block.inputCount -= count;
            }

            return totalOutputBytes;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public void Reset()
        {
            this.tempCount = 0;
        }
    }
}

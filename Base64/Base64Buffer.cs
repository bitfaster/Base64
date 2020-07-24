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
            this.FromBase64Transform = new FromBase64Transform();
            this.InputBlock = new byte[this.FromBase64Transform.InputBlockSize];
            this.OutputBlock = new byte[this.FromBase64Transform.OutputBlockSize];
            this.OutputBuffer = new byte[this.FromBase64Transform.ChunkSize];
        }

        public FromBase64Transform FromBase64Transform { get; }

        public byte[] InputBlock { get; }

        public byte[] OutputBlock { get; }

        public byte[] OutputBuffer { get; }

        public void Reset()
        {
            Array.Clear(this.InputBlock, 0, this.InputBlock.Length);
            Array.Clear(this.OutputBlock, 0, this.OutputBlock.Length);
        }
    }

    // https://referencesource.microsoft.com/#mscorlib/system/security/cryptography/cryptostream.cs,aeb692cbbaf22679
    public class TransformBase64Stream : Stream
    {
        private readonly Stream stream;
        private readonly FromBase64Transform transform;

        private byte[] inputBlock;
        private int inputBlockIndex = 0;
        private int inputBlockSize;
        private byte[] outputBlock;
        private byte[] outputBuffer;
        private int outputBlockIndex = 0;
        private int outputBlockSize;
        private bool finalBlockTransformed = false;
        private bool leaveOpen;

        private const string StreamNotSeekable = "Stream is not seekable";

        public TransformBase64Stream(Stream stream, Base64Buffer base64Buffer, bool leaveOpen = false)
        {
            this.stream = stream;
            this.transform = base64Buffer.FromBase64Transform;

            this.inputBlockSize = base64Buffer.InputBlock.Length;
            this.outputBlockSize = base64Buffer.OutputBlock.Length;
            this.inputBlock = base64Buffer.InputBlock;
            this.outputBlock = base64Buffer.OutputBlock;
            this.outputBuffer = base64Buffer.OutputBuffer;

            this.leaveOpen = leaveOpen;
        }

        public bool HasFlushedFinalBlock
        {
            get { return this.finalBlockTransformed; }
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException(StreamNotSeekable);

        public override long Position { get => throw new NotSupportedException(StreamNotSeekable); set => throw new NotSupportedException(StreamNotSeekable); }

        public void FlushFinalBlock()
        {
            if (this.finalBlockTransformed)
                throw new NotSupportedException("Final block already flushed");
            // We have to process the last block here.  First, we have the final block in _InputBuffer, so transform it

            byte[] finalBytes = this.transform.TransformFinalBlock(this.inputBlock, 0, this.inputBlockIndex);

            this.finalBlockTransformed = true;

            // Now, write out anything sitting in the _OutputBuffer...
            if (this.outputBlockIndex > 0)
            {
                this.stream.Write(this.outputBlock, 0, this.outputBlockIndex);
                this.outputBlockIndex = 0;
            }

            // Write out finalBytes
            this.stream.Write(finalBytes, 0, finalBytes.Length);

            this.stream.Flush();

            return;
        }

        public override void Flush()
        {
            return;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Stream is readonly");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(StreamNotSeekable);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(StreamNotSeekable);
        }

        // It seems like first 3 if statements are never reached, can they be eliminated?
        // in other words, does the while loop always write all the data? While loop says yes.

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Argument_InvalidOffLen");

            // write <= count bytes to the output stream, transforming as we go.
            // Basic idea: using bytes in the inputBlock first, make whole blocks,
            // transform them, and write them out. Cache any remaining bytes in the inputBlock.
            int bytesToWrite = count;
            int currentInputIndex = offset;

            // if we have some bytes in the inputBlock, we have to deal with those first,
            // so let's try to make an entire block out of it
            if (this.inputBlockIndex > 0)
            {
                if (count >= this.inputBlockSize - this.inputBlockIndex)
                {
                    // we have enough to transform at least a block, so fill the input block
                    Buffer.BlockCopy(buffer, offset, this.inputBlock, this.inputBlockIndex, this.inputBlockSize - this.inputBlockIndex);
                    currentInputIndex += (this.inputBlockSize - this.inputBlockIndex);
                    bytesToWrite -= (this.inputBlockSize - this.inputBlockIndex);
                    this.inputBlockIndex = this.inputBlockSize;
                    // Transform the block and write it out
                }
                else
                {
                    // not enough to transform a block, so just copy the bytes into the inputBlock
                    // and return
                    Buffer.BlockCopy(buffer, offset, this.inputBlock, this.inputBlockIndex, count);
                    this.inputBlockIndex += count;
                    return;
                }
            }

            // If the OutputBuffer has anything in it, write it out
            if (this.outputBlockIndex > 0)
            {
                this.stream.Write(this.outputBlock, 0, this.outputBlockIndex);
                this.outputBlockIndex = 0;
            }

            // At this point, either the inputBlock is full, empty, or we've already returned.
            // If full, let's process it -- we now know the outputBlock is empty
            int numOutputBytes;
            if (this.inputBlockIndex == this.inputBlockSize)
            {
                numOutputBytes = this.transform.TransformBlock(this.inputBlock, 0, this.inputBlockSize, this.outputBlock, 0);
                // write out the bytes we just got 
                this.stream.Write(this.outputBlock, 0, numOutputBytes);
                // reset the _InputBuffer
                this.inputBlockIndex = 0;
            }

            while (bytesToWrite > 0)
            {
                if (bytesToWrite >= this.inputBlockSize)
                {
                    // take chunks of outputBuffer.Length
                    int chunk = Math.Min(this.outputBuffer.Length, bytesToWrite);

                    // only write whole blocks
                    int numWholeBlocks = chunk / inputBlockSize;
                    int numWholeBlocksInBytes = numWholeBlocks * inputBlockSize;

                    numOutputBytes = this.transform.TransformBlock(buffer, currentInputIndex, numWholeBlocksInBytes, this.outputBuffer, 0);
                    this.stream.Write(this.outputBuffer, 0, numOutputBytes);
                    currentInputIndex += numWholeBlocksInBytes;
                    bytesToWrite -= numWholeBlocksInBytes;
                }
                else
                {
                    // In this case, we don't have an entire block's worth left, so store it up in the 
                    // input buffer, which by now must be empty.
                    Buffer.BlockCopy(buffer, currentInputIndex, inputBlock, 0, bytesToWrite);
                    inputBlockIndex += bytesToWrite;

                    return;
                }
            }

            return;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (!this.finalBlockTransformed)
                    {
                        this.FlushFinalBlock();
                    }

                    if (!leaveOpen)
                    {
                        this.stream.Close();
                    }
                }
            }
            finally
            {
                try
                {
                    // Ensure we don't try to transform the final block again if we get disposed twice
                    // since it's null after this
                    this.finalBlockTransformed = true;

                    this.inputBlock = null;
                    this.outputBlock = null;
                    this.outputBuffer = null;
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
    }

    public class FromBase64Transform
    {
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


        public unsafe int TransformBlock(byte[] input, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            // if (inputCount != bufferSize)
            //     throw new ArgumentOutOfRangeException("inputCount", "ArgumentOutOfRange_NeedNonNegNum");

            // Do some validation
            if (input == null) throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", "ArgumentOutOfRange_NeedNonNegNum");
            if (inputCount < 0 || (inputCount > input.Length)) throw new ArgumentException("Argument_InvalidValue");
            if ((input.Length - inputCount) < inputOffset) throw new ArgumentException("Argument_InvalidOffLen");

            int effectiveCount;

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            // TODO it looks like the convert function we call into ignores whitespace, so this could be skipped
            // test what happens if input contains whitespace
            //if (_whitespaces == FromBase64TransformMode.IgnoreWhiteSpaces) 
            //{
            //    effectiveCount = DiscardWhiteSpaces2(inputBuffer, inputOffset, inputCount);
            //} 
            //else 
            //{
            Buffer.BlockCopy(input, inputOffset, this.tempByteBuffer, 0, inputCount);
            effectiveCount = inputCount;
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
                nWritten = Encoding.ASCII.GetChars(transformBuffer, 4 * numBlocks, cp, bufferSize);
            }

            // write chars directly to outputBuffer
            fixed (byte* op = outputBuffer)
            {
                return FromBase64CharArray(tempCharBuffer, 0, nWritten, op + outputOffset);
            }
        }

        public unsafe byte[] TransformFinalBlock(byte[] input, int inputOffset, int inputCount)
        {
            // Do some validation
            if (input == null) throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", "ArgumentOutOfRange_NeedNonNegNum");
            if (inputCount < 0 || (inputCount > input.Length)) throw new ArgumentException("Argument_InvalidValue");
            if ((input.Length - inputCount) < inputOffset) throw new ArgumentException("Argument_InvalidOffLen");

            int effectiveCount;

            if (_whitespaces == FromBase64TransformMode.IgnoreWhiteSpaces)
            {
                effectiveCount = DiscardWhiteSpaces2(input, inputOffset, inputCount);
            }
            else
            {
                Buffer.BlockCopy(input, inputOffset, tempByteBuffer, 0, inputCount);
                effectiveCount = inputCount;
            }

            if (effectiveCount + inputIndex < 4)
            {
                Reset();
                return (Array.Empty<Byte>());
            }

            // Get the number of 4 bytes blocks to transform
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

            byte[] tempBytes = Convert.FromBase64CharArray(tempCharBuffer, 0, nWritten);

            // reinitialize the transform
            Reset();
            return tempBytes;
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


        /// <summary>
        /// Converts the specified range of a Char array, which encodes binary data as Base64 digits, to the equivalent byte array.     
        /// </summary>
        /// <param name="inArray">Chars representing Base64 encoding characters</param>
        /// <param name="offset">A position within the input array.</param>
        /// <param name="length">Number of element to convert.</param>
        /// <returns>The array of bytes represented by the specified Base64 encoding characters.</returns>
        [SecuritySafeCritical]
        private static unsafe int FromBase64CharArray(char[] inArray, int offset, int length, byte* destination)
        {

            if (inArray == null)
                throw new ArgumentNullException("inArray");

            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "ArgumentOutOfRange_Index");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_GenericPositive");

            if (offset > inArray.Length - length)
                throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_OffsetLength");

            unsafe
            {
                fixed (char* inArrayPtr = inArray)
                {

                    return FromBase64CharPtr(inArrayPtr + offset, length, destination);
                }
            }
        }

        // This method has been modified to accept pointer input buffers
        /// <summary>
        /// Convert Base64 encoding characters to bytes:
        ///  - Compute result length exactly by actually walking the input;
        ///  - Allocate new result array based on computation;
        ///  - Decode input into the new array;
        /// </summary>
        /// <param name="inputPtr">Pointer to the first input char</param>
        /// <param name="inputLength">Number of input chars</param>
        /// <returns></returns>
        [SecurityCritical]
        private static unsafe int FromBase64CharPtr(char* inputPtr, int inputLength, byte* decodedBytesPtr)
        {

            // The validity of parameters much be checked by callers, thus we are Critical here.

            // We need to get rid of any trailing white spaces.
            // Otherwise we would be rejecting input such as "abc= ":
            while (inputLength > 0)
            {
                int lastChar = inputPtr[inputLength - 1];

                if (lastChar != (int)' ' && lastChar != (int)'\n' && lastChar != (int)'\r' && lastChar != (int)'\t')
                {
                    break;
                }
                inputLength--;
            }

            // Compute the output length:
            Int32 resultLength = FromBase64_ComputeResultLength(inputPtr, inputLength);

            // resultLength can be zero. We will still enter FromBase64_Decode and process the input.
            // It may either simply write no bytes (e.g. input = " ") or throw (e.g. input = "ab").

            // Convert Base64 chars into bytes:
            return FromBase64_Decode(inputPtr, inputLength, decodedBytesPtr, resultLength);

            // Note that actualResultLength can differ from resultLength if the caller is modifying the array
            // as it is being converted. Silently ignore the failure.
            // Consider throwing exception in an non in-place release.
        }

        // From here on might be able to hook up via Reflection to avoid copy paste.

        /// <summary>
        /// Decode characters representing a Base64 encoding into bytes:
        /// Walk the input. Every time 4 chars are read, convert them to the 3 corresponding output bytes.
        /// This method is a bit lengthy on purpose. We are trying to avoid jumps to helpers in the loop
        /// to aid performance.
        /// </summary>
        /// <param name="inputPtr">Pointer to first input char</param>
        /// <param name="inputLength">Number of input chars</param>
        /// <param name="destPtr">Pointer to location for teh first result byte</param>
        /// <param name="destLength">Max length of the preallocated result buffer</param>
        /// <returns>If the result buffer was not large enough to write all result bytes, return -1;
        /// Otherwise return the number of result bytes actually produced.</returns>
        [SecurityCritical]
        private static unsafe Int32 FromBase64_Decode(Char* startInputPtr, Int32 inputLength, Byte* startDestPtr, Int32 destLength)
        {

            // You may find this method weird to look at. It’s written for performance, not aesthetics.
            // You will find unrolled loops label jumps and bit manipulations.

            const UInt32 intA = (UInt32)'A';
            const UInt32 inta = (UInt32)'a';
            const UInt32 int0 = (UInt32)'0';
            const UInt32 intEq = (UInt32)'=';
            const UInt32 intPlus = (UInt32)'+';
            const UInt32 intSlash = (UInt32)'/';
            const UInt32 intSpace = (UInt32)' ';
            const UInt32 intTab = (UInt32)'\t';
            const UInt32 intNLn = (UInt32)'\n';
            const UInt32 intCRt = (UInt32)'\r';
            const UInt32 intAtoZ = (UInt32)('Z' - 'A');  // = ('z' - 'a')
            const UInt32 int0to9 = (UInt32)('9' - '0');

            Char* inputPtr = startInputPtr;
            Byte* destPtr = startDestPtr;

            // Pointers to the end of input and output:
            Char* endInputPtr = inputPtr + inputLength;
            Byte* endDestPtr = destPtr + destLength;

            // Current char code/value:
            UInt32 currCode;

            // This 4-byte integer will contain the 4 codes of the current 4-char group.
            // Eeach char codes for 6 bits = 24 bits.
            // The remaining byte will be FF, we use it as a marker when 4 chars have been processed.            
            UInt32 currBlockCodes = 0x000000FFu;

            unchecked
            {
                while (true)
                {

                    // break when done:
                    if (inputPtr >= endInputPtr)
                        goto _AllInputConsumed;

                    // Get current char:
                    currCode = (UInt32)(*inputPtr);
                    inputPtr++;

                    // Determine current char code:

                    if (currCode - intA <= intAtoZ)
                        currCode -= intA;

                    else if (currCode - inta <= intAtoZ)
                        currCode -= (inta - 26u);

                    else if (currCode - int0 <= int0to9)
                        currCode -= (int0 - 52u);

                    else
                    {
                        // Use the slower switch for less common cases:
                        switch (currCode)
                        {

                            // Significant chars:
                            case intPlus:
                                currCode = 62u;
                                break;

                            case intSlash:
                                currCode = 63u;
                                break;

                            // Legal no-value chars (we ignore these):
                            case intCRt:
                            case intNLn:
                            case intSpace:
                            case intTab:
                                continue;

                            // The equality char is only legal at the end of the input.
                            // Jump after the loop to make it easier for the JIT register predictor to do a good job for the loop itself:
                            case intEq:
                                goto _EqualityCharEncountered;

                            // Other chars are illegal:
                            default:
                                throw new FormatException("Format_BadBase64Char");
                        }
                    }

                    // Ok, we got the code. Save it:
                    currBlockCodes = (currBlockCodes << 6) | currCode;

                    // Last bit in currBlockCodes will be on after in shifted right 4 times:
                    if ((currBlockCodes & 0x80000000u) != 0u)
                    {

                        if ((Int32)(endDestPtr - destPtr) < 3)
                            return -1;

                        *(destPtr) = (Byte)(currBlockCodes >> 16);
                        *(destPtr + 1) = (Byte)(currBlockCodes >> 8);
                        *(destPtr + 2) = (Byte)(currBlockCodes);
                        destPtr += 3;

                        currBlockCodes = 0x000000FFu;
                    }

                }
            }  // unchecked while

            // 'd be nice to have an assert that we never get here, but CS0162: Unreachable code detected.
            // Contract.Assert(false, "We only leave the above loop by jumping; should never get here.");

            // We jump here out of the loop if we hit an '=':
            _EqualityCharEncountered:

            //Contract.Assert(currCode == intEq);

            // Recall that inputPtr is now one position past where '=' was read.
            // '=' can only be at the last input pos:
            if (inputPtr == endInputPtr)
            {

                // Code is zero for trailing '=':
                currBlockCodes <<= 6;

                // The '=' did not complete a 4-group. The input must be bad:
                if ((currBlockCodes & 0x80000000u) == 0u)
                    throw new FormatException("Format_BadBase64CharArrayLength");

                if ((int)(endDestPtr - destPtr) < 2)  // Autch! We underestimated the output length!
                    return -1;

                // We are good, store bytes form this past group. We had a single "=", so we take two bytes:
                *(destPtr++) = (Byte)(currBlockCodes >> 16);
                *(destPtr++) = (Byte)(currBlockCodes >> 8);

                currBlockCodes = 0x000000FFu;

            }
            else
            { // '=' can also be at the pre-last position iff the last is also a '=' excluding the white spaces:

                // We need to get rid of any intermediate white spaces.
                // Otherwise we would be rejecting input such as "abc= =":
                while (inputPtr < (endInputPtr - 1))
                {
                    Int32 lastChar = *(inputPtr);
                    if (lastChar != (Int32)' ' && lastChar != (Int32)'\n' && lastChar != (Int32)'\r' && lastChar != (Int32)'\t')
                        break;
                    inputPtr++;
                }

                if (inputPtr == (endInputPtr - 1) && *(inputPtr) == '=')
                {

                    // Code is zero for each of the two '=':
                    currBlockCodes <<= 12;

                    // The '=' did not complete a 4-group. The input must be bad:
                    if ((currBlockCodes & 0x80000000u) == 0u)
                        throw new FormatException("Format_BadBase64CharArrayLength");

                    if ((Int32)(endDestPtr - destPtr) < 1)  // Autch! We underestimated the output length!
                        return -1;

                    // We are good, store bytes form this past group. We had a "==", so we take only one byte:
                    *(destPtr++) = (Byte)(currBlockCodes >> 16);

                    currBlockCodes = 0x000000FFu;

                }
                else  // '=' is not ok at places other than the end:
                    throw new FormatException("Format_BadBase64Char");

            }

            // We get here either from above or by jumping out of the loop:
            _AllInputConsumed:

            // The last block of chars has less than 4 items
            if (currBlockCodes != 0x000000FFu)
                throw new FormatException("Format_BadBase64CharArrayLength");

            // Return how many bytes were actually recovered:
            return (Int32)(destPtr - startDestPtr);

        } // Int32 FromBase64_Decode(...)


        /// <summary>
        /// Compute the number of bytes encoded in the specified Base 64 char array:
        /// Walk the entire input counting white spaces and padding chars, then compute result length
        /// based on 3 bytes per 4 chars.
        /// </summary>
        [SecurityCritical]
        private static unsafe Int32 FromBase64_ComputeResultLength(Char* inputPtr, Int32 inputLength)
        {

            const UInt32 intEq = (UInt32)'=';
            const UInt32 intSpace = (UInt32)' ';

            //Contract.Assert(0 <= inputLength);

            Char* inputEndPtr = inputPtr + inputLength;
            Int32 usefulInputLength = inputLength;
            Int32 padding = 0;

            while (inputPtr < inputEndPtr)
            {

                UInt32 c = (UInt32)(*inputPtr);
                inputPtr++;

                // We want to be as fast as possible and filter out spaces with as few comparisons as possible.
                // We end up accepting a number of illegal chars as legal white-space chars.
                // This is ok: as soon as we hit them during actual decode we will recognise them as illegal and throw.
                if (c <= intSpace)
                    usefulInputLength--;

                else if (c == intEq)
                {
                    usefulInputLength--;
                    padding++;
                }
            }

            //Contract.Assert(0 <= usefulInputLength);

            // For legal input, we can assume that 0 <= padding < 3. But it may be more for illegal input.
            // We will notice it at decode when we see a '=' at the wrong place.
            //Contract.Assert(0 <= padding);

            // Perf: reuse the variable that stored the number of '=' to store the number of bytes encoded by the
            // last group that contains the '=':
            if (padding != 0)
            {

                if (padding == 1)
                    padding = 2;
                else if (padding == 2)
                    padding = 1;
                else
                    throw new FormatException("Format_BadBase64Char");
            }

            // Done:
            return (usefulInputLength / 4) * 3 + padding;
        }
    }
}

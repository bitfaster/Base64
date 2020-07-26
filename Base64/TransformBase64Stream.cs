using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    // https://referencesource.microsoft.com/#mscorlib/system/security/cryptography/cryptostream.cs,aeb692cbbaf22679
    public class TransformBase64Stream : Stream
    {
        private readonly Stream stream;
        private readonly IBase64Transform transform;

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

            //byte[] finalBytes = this.transform.TransformFinalBlock(this.inputBlock, 0, this.inputBlockIndex, this.outputBuffer, 0);
            int len = this.transform.TransformFinalBlock(this.inputBlock, 0, this.inputBlockIndex, this.outputBuffer, 0);

            this.finalBlockTransformed = true;

            // Now, write out anything sitting in the _OutputBuffer...
            if (this.outputBlockIndex > 0)
            {
                this.stream.Write(this.outputBlock, 0, this.outputBlockIndex);
                this.outputBlockIndex = 0;
            }

            // Write out finalBytes
            this.stream.Write(this.outputBuffer, 0, len);

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
}

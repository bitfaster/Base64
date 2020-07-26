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

        private byte[] finalInputBlock;
        private int finalInputBlockCount = 0;
        private int finalInputBlockSize;
        private bool finalBlockTransformed = false;
        private bool leaveOpen;

        private Block block;

        private const string StreamNotSeekable = "Stream is not seekable";

        public TransformBase64Stream(Stream stream, Base64Buffer base64Buffer, bool leaveOpen = false)
        {
            this.stream = stream;
            this.transform = base64Buffer.FromBase64Transform;

            this.finalInputBlockSize = base64Buffer.FinalInputBlock.Length;
            this.finalInputBlock = base64Buffer.FinalInputBlock;

            this.block = base64Buffer.Block;

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

            // We have to process the last block here.  First, we have the final block in tempInputBlock, so transform it
            this.block.input = this.finalInputBlock;
            this.block.inputOffset = 0;
            this.block.inputCount = this.finalInputBlockCount;
            this.block.outputOffset = 0;

            int len = this.transform.TransformFinalBlock(this.block);
            this.block.Reset();

            this.finalBlockTransformed = true;

            this.stream.Write(this.block.outputBuffer, 0, len);

            this.stream.Flush();

            return;
        }

        public override void Flush()
        {
            return;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Stream is write only");
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

            if (this.finalInputBlockCount != 0)
                throw new InvalidOperationException("An incomplete block was written prior to calling write.");

            // write <= count bytes to the output stream, transforming as we go.
            // Basic idea: using bytes in the inputBlock first, make whole blocks,
            // transform them, and write them out. Cache any remaining bytes in the finalInputBlock.
            int bytesToWrite = count;
            int currentInputIndex = offset;

            int numOutputBytes;

            while (bytesToWrite > 0)
            {
                if (bytesToWrite >= this.finalInputBlockSize)
                {
                    // take chunks of outputBuffer.Length
                    int chunk = Math.Min(this.block.outputBuffer.Length, bytesToWrite);

                    // only write whole blocks
                    int numWholeBlocks = chunk / finalInputBlockSize;
                    int numWholeBlocksInBytes = numWholeBlocks * finalInputBlockSize;

                    this.block.input = buffer;
                    this.block.inputOffset = currentInputIndex;
                    this.block.inputCount = numWholeBlocksInBytes;
                    this.block.outputOffset = 0;

                    numOutputBytes = this.transform.TransformBlock(block);
                    this.stream.Write(this.block.outputBuffer, 0, numOutputBytes);
                    currentInputIndex += numWholeBlocksInBytes;
                    bytesToWrite -= numWholeBlocksInBytes;
                }
                else
                {
                    // In this case, we don't have an entire block's worth left, so store it up in the 
                    // input buffer, which by now must be empty.
                    Buffer.BlockCopy(buffer, currentInputIndex, this.finalInputBlock, 0, bytesToWrite);
                    this.finalInputBlockCount += bytesToWrite;

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

                    this.block = null;
                    this.finalInputBlock = null;
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
    }
}

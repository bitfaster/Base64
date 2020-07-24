using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    // https://referencesource.microsoft.com/#mscorlib/system/io/streamwriter.cs
    // TODO: throw not supported on async methods
    public class BufferedStreamWriter : TextWriter
    {
        private Encoding encoding;
        private Stream stream;
        private Encoder encoder;

        private byte[] byteBuffer;
        private char[] charBuffer;

        private int charLen;
        private int charPos;
        private bool closable;
        private bool autoFlush;
        private bool haveWrittenPreamble;

        public override Encoding Encoding => this.encoding;

        private bool LeaveOpen => !closable;

        public BufferedStreamWriter(Stream stream, EncodingBuffer encodingBuffer, bool leaveOpen = false)
        {
            this.stream = stream;
            this.encoding = encodingBuffer.Encoding;
            this.encoder = encodingBuffer.Encoder;

            this.charBuffer = encodingBuffer.CharBuffer;
            this.charLen = this.charBuffer.Length;

            this.byteBuffer = encodingBuffer.ByteBuffer;

            this.closable = !leaveOpen;
        }

        public override void Write(char value)
        {
            if (charPos == charLen) Flush(false, false);
            charBuffer[charPos] = value;
            charPos++;
            if (autoFlush) Flush(true, false);
        }

        public override void Write(char[] buffer)
        {
            // This may be faster than the one with the index & count since it
            // has to do less argument checking.
            if (buffer == null)
                return;

            int index = 0;
            int count = buffer.Length;
            while (count > 0)
            {
                if (charPos == charLen) Flush(false, false);
                int n = charLen - charPos;
                if (n > count) n = count;
                //Contract.Assert(n > 0, "StreamWriter::Write(char[]) isn't making progress!  This is most likely a ---- in user code.");
                Buffer.BlockCopy(buffer, index * sizeof(char), charBuffer, charPos * sizeof(char), n * sizeof(char));
                charPos += n;
                index += n;
                count -= n;
            }
            if (autoFlush) Flush(true, false);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (buffer.Length - index < count)
                throw new ArgumentException("Invalid offset length");
            // Contract.EndContractBlock();

            while (count > 0)
            {
                if (charPos == charLen) Flush(false, false);
                int n = charLen - charPos;
                if (n > count) n = count;
                //Contract.Assert(n > 0, "StreamWriter::Write(char[], int, int) isn't making progress!  This is most likely a race condition in user code.");
                Buffer.BlockCopy(buffer, index * sizeof(char), charBuffer, charPos * sizeof(char), n * sizeof(char));
                charPos += n;
                index += n;
                count -= n;
            }
            if (autoFlush) Flush(true, false);
        }

        public override void Write(String value)
        {
            if (value != null)
            {
                int count = value.Length;
                int index = 0;
                while (count > 0)
                {
                    if (charPos == charLen) Flush(false, false);
                    int n = charLen - charPos;
                    if (n > count) n = count;
                    //Contract.Assert(n > 0, "StreamWriter::Write(String) isn't making progress!  This is most likely a race condition in user code.");
                    value.CopyTo(index, charBuffer, charPos, n);
                    charPos += n;
                    index += n;
                    count -= n;
                }
                if (autoFlush) Flush(true, false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                // We need to flush any buffered data if we are being closed/disposed.
                // Also, we never close the handles for stdout & friends.  So we can safely 
                // write any buffered data to those streams even during finalization, which 
                // is generally the right thing to do.
                if (stream != null)
                {
                    // Note: flush on the underlying stream can throw (ex., low disk space)
                    if (disposing)
                    {
                        Flush(true, true);
                    }
                }
            }
            finally
            {
                // Dispose of our resources if this StreamWriter is closable. 
                // Note: Console.Out and other such non closable streamwriters should be left alone 
                if (!LeaveOpen && stream != null)
                {
                    try
                    {
                        // Attempt to close the stream even if there was an IO error from Flushing.
                        // Note that Stream.Close() can potentially throw here (may or may not be
                        // due to the same Flush error). In this case, we still need to ensure 
                        // cleaning up internal resources, hence the finally block.  
                        if (disposing)
                            stream.Close();
                    }
                    finally
                    {
                        stream = null;
                        byteBuffer = null;
                        charBuffer = null;
                        encoding = null;
                        encoder = null;
                        charLen = 0;
                        base.Dispose(disposing);
                    }
                }
            }
        }

        private void Flush(bool flushStream, bool flushEncoder)
        {
            // flushEncoder should be true at the end of the file and if
            // the user explicitly calls Flush (though not if AutoFlush is true).
            // This is required to flush any dangling characters from our UTF-7 
            // and UTF-8 encoders.  
            if (stream == null)
                throw new ObjectDisposedException("Object disposed, writer closed");

            // Perf boost for Flush on non-dirty writers.
            if (charPos == 0 && ((!flushStream && !flushEncoder)))
                return;

            if (!haveWrittenPreamble)
            {
                haveWrittenPreamble = true;
                byte[] preamble = encoding.GetPreamble();
                if (preamble.Length > 0)
                    stream.Write(preamble, 0, preamble.Length);
            }

            int count = encoder.GetBytes(charBuffer, 0, charPos, byteBuffer, 0, flushEncoder);
            charPos = 0;
            if (count > 0)
                stream.Write(byteBuffer, 0, count);
            // By definition, calling Flush should flush the stream, but this is
            // only necessary if we passed in true for flushStream.  The Web
            // Services guys have some perf tests where flushing needlessly hurts.
            if (flushStream)
                stream.Flush();
        }
    }

    //internal static class BufferBlock
    //{
    //    // A very simple and efficient memmove that assumes all of the
    //    // parameter validation has already been done.  The count and offset
    //    // parameters here are in bytes.  If you want to use traditional
    //    // array element indices and counts, use Array.Copy.
    //    [System.Security.SecuritySafeCritical]  // auto-generated
    //    [ResourceExposure(ResourceScope.None)]
    //    [MethodImplAttribute(MethodImplOptions.InternalCall)] // throws System.Security.SecurityException: ECall methods must be packaged into a system module.
    //    internal static extern void InternalBlockCopy(Array src, int srcOffsetBytes, Array dst, int dstOffsetBytes, int byteCount);
    //}

    public class EncodingBuffer
    {
        public EncodingBuffer(Encoding encoding, int bufferSize)
        {
            this.Encoding = encoding;
            this.Encoder = encoding.GetEncoder();
            this.CharBuffer = new char[bufferSize];
            this.ByteBuffer = new byte[encoding.GetMaxByteCount(bufferSize)];
        }

        public Encoding Encoding { get; }

        public Encoder Encoder { get; }

        public byte[] ByteBuffer { get; }

        public char[] CharBuffer { get; }
    }
}

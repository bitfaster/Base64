using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Base64
{
    public static class UnsafeConvert
    {
        /// <summary>
        /// Converts the specified range of a Char array, which encodes binary data as Base64 digits, to the equivalent byte array.     
        /// </summary>
        /// <param name="inArray">Chars representing Base64 encoding characters</param>
        /// <param name="offset">A position within the input array.</param>
        /// <param name="length">Number of element to convert.</param>
        /// <returns>The array of bytes represented by the specified Base64 encoding characters.</returns>
        [SecuritySafeCritical]
        public static unsafe int FromBase64CharArray(char[] inArray, int offset, int length, byte* destination)
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

        // Try direct char*
        [SecuritySafeCritical]
        public static unsafe int FromBase64CharArray(char* inArray, int offset, int length, byte* destination)
        {

            if (inArray == null)
                throw new ArgumentNullException("inArray");

            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "ArgumentOutOfRange_Index");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_GenericPositive");

            //if (offset > inArray.Length - length)
            //    throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_OffsetLength");

            return FromBase64CharPtr(inArray + offset, length, destination);
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
            return FromBase64_DecodeReflect(inputPtr, inputLength, decodedBytesPtr, resultLength);

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

        [SecurityCritical]
        private static unsafe int FromBase64_Decode2(
          char* startInputPtr,
          int inputLength,
          byte* startDestPtr,
          int destLength)
        {
            char* chPtr1 = startInputPtr;
            byte* numPtr1 = startDestPtr;
            char* chPtr2 = chPtr1 + inputLength;
            byte* numPtr2 = numPtr1 + destLength;
            uint num1 = (uint)byte.MaxValue;
            while (chPtr1 < chPtr2)
            {
                uint num2 = (uint)*chPtr1;
                ++chPtr1;
                uint num3;
                if (num2 - 65U <= 25U)
                    num3 = num2 - 65U;
                else if (num2 - 97U <= 25U)
                {
                    num3 = num2 - 71U;
                }
                else
                {
                    switch (num2)
                    {
                        case 9:
                        case 10:
                        case 13:
                        case 32:
                            continue;
                        case 43:
                            num3 = 62U;
                            break;
                        case 47:
                            num3 = 63U;
                            break;
                        case 48:
                        case 49:
                        case 50:
                        case 51:
                        case 52:
                        case 53:
                        case 54:
                        case 55:
                        case 56:
                        case 57:
                            num3 = num2 - 4294967292U;
                            break;
                        case 61:
                            if (chPtr1 == chPtr2)
                            {
                                uint num4 = num1 << 6;
                                if (((int)num4 & int.MinValue) == 0)
                                    throw new FormatException("Format_BadBase64CharArrayLength");
                                if ((int)(numPtr2 - numPtr1) < 2)
                                    return -1;
                                byte* numPtr3 = numPtr1;
                                byte* numPtr4 = numPtr3 + 1;
                                int num5 = (int)(byte)(num4 >> 16);
                                *numPtr3 = (byte)num5;
                                byte* numPtr5 = numPtr4;
                                numPtr1 = numPtr5 + 1;
                                int num6 = (int)(byte)(num4 >> 8);
                                *numPtr5 = (byte)num6;
                                num1 = (uint)byte.MaxValue;
                                goto label_31;
                            }
                            else
                            {
                                for (; chPtr1 < chPtr2 - 1; ++chPtr1)
                                {
                                    switch (*chPtr1)
                                    {
                                        case '\t':
                                        case '\n':
                                        case '\r':
                                        case ' ':
                                            continue;
                                        default:
                                            goto label_24;
                                    }
                                }
                                label_24:
                                if (chPtr1 != chPtr2 - 1 || *chPtr1 != '=')
                                    throw new FormatException("Format_BadBase64Char");
                                uint num4 = num1 << 12;
                                if (((int)num4 & int.MinValue) == 0)
                                    throw new FormatException("Format_BadBase64CharArrayLength");
                                if ((int)(numPtr2 - numPtr1) < 1)
                                    return -1;
                                *numPtr1++ = (byte)(num4 >> 16);
                                num1 = (uint)byte.MaxValue;
                                goto label_31;
                            }
                        default:
                            throw new FormatException("Format_BadBase64Char");
                    }
                }
                num1 = num1 << 6 | num3;
                if (((int)num1 & int.MinValue) != 0)
                {
                    if ((int)(numPtr2 - numPtr1) < 3)
                        return -1;
                    *numPtr1 = (byte)(num1 >> 16);
                    numPtr1[1] = (byte)(num1 >> 8);
                    numPtr1[2] = (byte)num1;
                    numPtr1 += 3;
                    num1 = (uint)byte.MaxValue;
                }
            }
            label_31:
            if (num1 != (uint)byte.MaxValue)
                throw new FormatException("Format_BadBase64CharArrayLength");
            return (int)(numPtr1 - startDestPtr);
        }

        private static readonly MethodInfo FromBase64_DecodeMethodInfo = typeof(Convert).GetMethod("FromBase64_Decode", BindingFlags.Static | BindingFlags.NonPublic);

        // no measurable overhead from this - it's not in a tight loop.
        public static unsafe int FromBase64_DecodeReflect(
          char* startInputPtr,
          int inputLength,
          byte* startDestPtr,
          int destLength)
        {
            var inputPtr = Pointer.Box(startInputPtr, typeof(char*));
            var destPtr = Pointer.Box(startDestPtr, typeof(byte*));

            var args = new object[] { inputPtr, inputLength,  destPtr, destLength };

            try
            {
                return (int)FromBase64_DecodeMethodInfo.Invoke(null, args);
            }
            catch (TargetInvocationException ex)
            {
                // unwrap
                throw ex.InnerException ?? ex;
            }
        }

        //public static unsafe int FromBase64_DecodeReflectDel(
        //  char* startInputPtr,
        //  int inputLength,
        //  byte* startDestPtr,
        //  int destLength)
        //{
        //    var type = typeof(Convert);
        //    var method = type.GetMethod("FromBase64_Decode", BindingFlags.Static | BindingFlags.NonPublic);

        //    var inputPtr = Pointer.Box(startInputPtr, typeof(char*));
        //    var destPtr = Pointer.Box(startDestPtr, typeof(byte*));

        //    return FromBase64_DecodeDel(inputPtr, inputLength, destPtr, destLength);
        //}

        //private static readonly Func<object, int, object, int, int> FromBase64_DecodeDel = CreateMethod();

        //private static Func<object, int, object, int, int>  CreateMethod()
        //{
        //    // this fails because the method is security critical, but the delegate type is not. Create delegate doesn't appear to support this.
        //    var method = typeof(Convert).GetMethod("FromBase64_Decode", BindingFlags.Static | BindingFlags.NonPublic);
        //    return (Func<object, int, object, int, int>)method.CreateDelegate(typeof(Func<object, int, object, int, int>));
        //}
    }
}

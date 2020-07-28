using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;

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

            fixed (char* inArrayPtr = inArray)
            {
                return FromBase64CharPtr(inArrayPtr + offset, length, destination);
            }
        }

        /// <summary>
        /// Convert Base64 encoding characters to bytes:
        ///  - Compute result length
        ///  - Decode input into the specified array;
        /// </summary>
        /// <param name="inputPtr">Pointer to the first input char</param>
        /// <param name="inputLength">Number of input chars</param>
        /// <returns></returns>
        [SecurityCritical]
        private static unsafe int FromBase64CharPtr(char* inputPtr, int inputLength, byte* decodedBytesPtr)
        {
            int resultLength = (inputLength / 4) * 3;

            // resultLength can be zero. We will still enter FromBase64_Decode and process the input.
            // It may either simply write no bytes (e.g. input = " ") or throw (e.g. input = "ab").

            // Convert Base64 chars into bytes:
            return FromBase64_DecodeReflect(inputPtr, inputLength, decodedBytesPtr, resultLength);

            // Note that actualResultLength can differ from resultLength if the caller is modifying the array
            // as it is being converted. Silently ignore the failure.
            // Consider throwing exception in an non in-place release.
        }

        private static readonly MethodInfo FromBase64_DecodeMethodInfo = typeof(Convert).GetMethod("FromBase64_Decode", BindingFlags.Static | BindingFlags.NonPublic);

        // no measurable overhead from reflection here - it's not in a tight loop.
        // Can we turn this into a func to avoid allocating the args array? Pointers still need to be boxed.
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
    }
}

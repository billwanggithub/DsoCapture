using System;

namespace DsoCapture.Utility
{
    class DataHelper
    {
        public static byte[] CombineByteArrays(byte[] array1, byte[] array2)
        {
            byte[] combinedArray = new byte[array1.Length + array2.Length];
            Array.Copy(array1, combinedArray, array1.Length);
            Array.Copy(array2, 0, combinedArray, array1.Length, array2.Length);
            return combinedArray;
        }
    }
}

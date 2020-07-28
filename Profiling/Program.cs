using Base64;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiling
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Options:");
            Console.WriteLine("1. ToUtf8Base64String");
            Console.WriteLine("2. FromUtf8Base64String");
            Console.WriteLine("3. FromUtf8Base64String (LOH)");
            Console.WriteLine("4. CompareFromUtf8Base64String");

            var input = Console.ReadLine();

            if (!int.TryParse(input, out var selectedOption))
            {
                throw new Exception("not a valid int");
            }

            Console.WriteLine("Running...");

            switch (selectedOption)
            {
                case 1:
                    ToUtf8Base64String();
                    break;
                case 2:
                    FromUtf8Base64String();
                    break;
                case 3:
                    LohTest();
                    break;
                case 4:
                    CompareFromUtf8Base64String();
                    break;
                default:
                    throw new Exception("invalid option");
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void ToUtf8Base64String()
        {
            var s = new string('a', 2048);

            for (int i = 0; i < 640000; i++)
            {
                s.ToUtf8Base64String();
            }
        }

        private static void FromUtf8Base64String()
        {
            var s = new string('a', 8000).ToUtf8Base64String();

            //var resultBytes = Convert.FromBase64String(s); 
            //var convertString = Encoding.UTF8.GetString(resultBytes);

            for (int i = 0; i < 640000; i++)
            {
                s.FromUtf8Base64String();
            }
        }

        private static void LohTest()
        {
            string lohString = new string('a', 500000).ToUtf8Base64String();

            for (int i = 0; i < 640000; i++)
            {
                lohString.FromUtf8Base64String();
            }

        }

        private static void CompareFromUtf8Base64String()
        {
            char[] alphabet = ("abcdefghijklmnopqrstuvwxyz" + "abcdefghijklmnopqrstuvwxyz".ToUpper() + "0123456789").ToCharArray();

            StringBuilder sb = new StringBuilder(128 * 128);

            for (int i = 0; i < 128; i++)
            {
                for (int j = 0; j < 128; j++)
                {
                    sb.Append(alphabet[(i + j) % alphabet.Length]);

                    var s2 = sb.ToString().ToUtf8Base64String();

                    var resultBytes = Convert.FromBase64String(s2);
                    var convertString = Encoding.UTF8.GetString(resultBytes);

                    var xStr = s2.FromUtf8Base64String();

                    if (xStr != convertString)
                    {
                        Console.WriteLine($"conv: {convertString}");
                        Console.WriteLine($"xStr: {xStr}");
                    }
                }
            }


            var s = new string('a', 8000).ToUtf8Base64String();

            //var resultBytes = Convert.FromBase64String(s); 
            //var convertString = Encoding.UTF8.GetString(resultBytes);

            for (int i = 0; i < 640000; i++)
            {
                s.FromUtf8Base64String();
            }
        }
    }
}

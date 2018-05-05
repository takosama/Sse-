using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;

namespace ConsoleApp67

{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            test.Run();
            BenchmarkDotNet.Running.BenchmarkRunner.Run<test>();
        }
    }

    public class test
    {
        [Benchmark]
        unsafe public static void Run()
        {
            byte[] img = new byte[1920 * 1080 * 4];
            byte[] canvus = new byte[1920 * 1080 * 4];

            fixed (byte* ptr = &img[0])
            fixed (byte* p = &canvus[0])
            {
                //ばぐとりようの数字
                for (int i = 0; i < 255; i++)
                {

                    img[i] = (byte)i;
                }
                var pprt = ptr;

                var pp = p;
                int h = 1080;
                int w = 1920;
                Vector128<float> r = Sse.SetVector128(.333f, .333f, .333f, .333f);
                Vector128<float> g = Sse.SetVector128(.666f, .666f, .666f, .666f);
                Vector128<float> b = Sse.SetVector128(.112f, .112f, .112f, .112f);
                Vector128<sbyte> maskr = Sse2.SetVector128(-1, -1, -1, 12, -1, -1, -1, 8, -1, -1, -1, 4, -1, -1, -1, 0);
                Vector128<sbyte> maskg = Sse2.SetVector128(-1, -1, -1, 13, -1, -1, -1, 9, -1, -1, -1, 5, -1, -1, -1, 1);
                Vector128<sbyte> maskb = Sse2.SetVector128(-1, -1, -1, 14, -1, -1, -1, 10, -1, -1, -1, 6, -1, -1, -1, 2);
                Vector128<sbyte> maskrtn = Sse2.SetVector128(-1, 12, 12, 12, -1, 8, 8, 8, -1, 4, 4, 4, -1, 0, 0, 0);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x += 4)
                    {
                        var tmp0 = Sse.StaticCast<byte, sbyte>(Sse2.LoadVector128(pprt));

                        var t0 = Ssse3.Shuffle(tmp0, maskr);
                        var t1 = Ssse3.Shuffle(tmp0, maskg);
                        var t2 = Ssse3.Shuffle(tmp0, maskb);

                        var tmp6 = Sse2.ConvertToVector128Single(Sse.StaticCast<sbyte, int>(t0));
                        var tmp7 = Sse2.ConvertToVector128Single(Sse.StaticCast<sbyte, int>(t1));
                        var tmp8 = Sse2.ConvertToVector128Single(Sse.StaticCast<sbyte, int>(t2));


                        var tmp13 = Sse.Add(Sse.Add(Sse.Multiply(tmp6, r), Sse.Multiply(tmp7, g)), Sse.Multiply(tmp8, b));

                        var tmp14 = Sse.StaticCast<int, sbyte>(Sse2.ConvertToVector128Int32(tmp13));

                        var tmp18 = Ssse3.Shuffle(tmp14, maskrtn);

                        Sse2.Store(pp, Sse.StaticCast<sbyte, byte>(tmp18));
                        pp += 16;
                        pprt += 16;
                    }
                }
            }
        }
    }
}
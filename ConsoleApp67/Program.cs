using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;

/*
 * BenchmarkDotNet=v0.10.14, OS=Windows 10.0.16299.371 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-4710MQ CPU 2.50GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
Frequency=2435767 Hz, Resolution=410.5483 ns, Timer=TSC
.NET Core SDK=2.2.100-preview1-008633
  [Host]     : .NET Core 2.1.0-preview3-26411-06 (CoreCLR 4.6.26411.07, CoreFX 4.6.26411.06), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.0-preview3-26411-06 (CoreCLR 4.6.26411.07, CoreFX 4.6.26411.06), 64bit RyuJIT


        Method |        Mean |     Error |    StdDev |      Median |
-------------- |------------:|----------:|----------:|------------:|
         Nomal | 10,354.6 us | 132.75 us | 117.68 us | 10,300.9 us |
 NomalParallel |  1,556.6 us |  32.39 us |  73.78 us |  1,524.0 us |
  SIMDParallel |    661.9 us |  21.73 us |  64.08 us |    639.2 us |
          SIMD |  7,318.6 us |  22.85 us |  20.26 us |  7,317.7 us |
          */
namespace ConsoleApp67

{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<test>();
        }
    }

    public class test
    {
        [Benchmark]
        unsafe public static void Nomal()
        {
            byte[] img = new byte[1920 * 1080 * 4];
            byte[] canvus = new byte[1920 * 1080 * 4];
            int h = 1080;
            int w = 1920;
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

                for(int y=0; y< h; y++)
                { 
                    for (int x = 0; x < w; x++)
                    {
                        byte l = (byte)(0.2 * pprt[0] + 0.6 * pprt[0] + 0.1 * pprt[0]);
                        pp[0] = l;
                        pp[1] = l;
                        pp[2] = l;
                        pp += 4;
                        pprt += 4;
                    }
                }
            }
        }

        [Benchmark]
        unsafe public static void NomalParallel()
        {
            byte[] img = new byte[1920 * 1080 * 4];
            byte[] canvus = new byte[1920 * 1080 * 4];
            int h = 1080 ;
            int w = 1920;
            fixed (byte* ptr = &img[0])
            fixed (byte* p = &canvus[0])
            {
                //ばぐとりようの数字
                for (int i = 0; i < 255; i++)
                {

                    img[i] = (byte)i;
                }
                var ppprt = ptr;

                var ppp = p;
                
                Parallel.For(0, h, y =>
                {
                    var pprt = ppprt + 4 * y + w;
                    var pp = ppp + 4 * y + w;
                    for (int x = 0; x < w; x ++)
                    {
                        byte l = (byte)(0.2*pprt[0] +0.6* pprt[0] +0.1* pprt[0]);
                        pp[0] = l;
                        pp[1] = l;
                        pp[2] = l;
                        pp += 4;
                        pprt += 4;
                    }
                });
            }
        }

        [Benchmark]
        unsafe public static void SIMDParallel()
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
                var ppprt = ptr;

                var ppp = p;
                int h = 1080;
                int w = 1920;
                Vector128<float> r = Sse.SetVector128(.333f, .333f, .333f, .333f);
                Vector128<float> g = Sse.SetVector128(.666f, .666f, .666f, .666f);
                Vector128<float> b = Sse.SetVector128(.112f, .112f, .112f, .112f);
                Vector128<sbyte> maskr = Sse2.SetVector128(-1, -1, -1, 12, -1, -1, -1, 8, -1, -1, -1, 4, -1, -1, -1, 0);
                Vector128<sbyte> maskg = Sse2.SetVector128(-1, -1, -1, 13, -1, -1, -1, 9, -1, -1, -1, 5, -1, -1, -1, 1);
                Vector128<sbyte> maskb = Sse2.SetVector128(-1, -1, -1, 14, -1, -1, -1, 10, -1, -1, -1, 6, -1, -1, -1, 2);
                Vector128<sbyte> maskrtn = Sse2.SetVector128(-1, 12, 12, 12, -1, 8, 8, 8, -1, 4, 4, 4, -1, 0, 0, 0);

                Parallel.For(0, h, y =>
                   {
                       var pprt = ppprt + 4 * y + w;
                       var pp = ppp + 4 * y + w;
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
                   });
            }
        }


        [Benchmark]
        unsafe public static void SIMD()
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

               for(int y=0;y< h; y++)
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

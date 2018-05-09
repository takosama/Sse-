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
    public class Program
    {

        static void Main(string[] args)
        {
            test.AlignmentedSIMDAVXParallell();
            BenchmarkDotNet.Running.BenchmarkRunner.Run<test>();
       //     Console.WriteLine(ArrayTest.aa[0]);
        }
    }

    public class ArrayTest
    {

        [Benchmark]
        static public void TestNomalArray()
        {
            int[] a = new int[1000000];
            int[] b = new int[1000000];
            Random r = new Random();
            for (int i = 0; i < 1000000; i++)
            {
                b[i] = r.Next(0, 1000000);
            }

            for (int i = 0; i < 1000000; i++)
            {
                a[b[i]] = a[b[i]] + 1;
            }
        }

        [Benchmark]
        static public void TestAlignmentedArray()
        {
            AlignmentedArray<int> aa = new AlignmentedArray<int>(1000000, 128);
            AlignmentedArray<int> ab = new AlignmentedArray<int>(1000000, 128);
            Random r = new Random();

            for (int i = 0; i < 1000000; i++)
            {
                ab[i] = r.Next(0, 1000000);
            }   

            for (int i = 0; i < 1000000; i++)
            {
               aa[ ab[i]] = aa[ ab[i]] + 1;
            }
        }
    }

    public unsafe class AlignmentedArray<T> where T : unmanaged
    {
        T[] TArray = null;
        T* ArrayZero = null;
        GCHandle handle;
        public AlignmentedArray(int Lng, int Alignment)
        {
            int n = (int)(Math.Ceiling(((double)(Alignment)) / Marshal.SizeOf<T>()));
             this.TArray = new T[n + Lng];
           var Handle= GCHandle.Alloc(this.TArray, GCHandleType.Pinned);
            this.handle = Handle;

            long lngPtr = this.handle.AddrOfPinnedObject().ToInt64();
            lngPtr = lngPtr % Alignment == 0 ? lngPtr : lngPtr + Alignment - lngPtr % Alignment;
            var StartPtr = new IntPtr(lngPtr);
            this.ArrayZero = (T*)StartPtr.ToPointer();
        }
        public T this[int n]
        {
            set
            {
                *(ArrayZero + n) = value;
            }
            get
            {
                return *(this.ArrayZero + n);
            }
        }

       public T* Pointer
        {
            get
            {
                return this.ArrayZero;
            }
        }
    
       public void Dispose()
       {
            this.handle.Free();
       }
      
       ~AlignmentedArray()
       {
           Dispose();
       }
    }

    unsafe public class test
    {
        static int _x = 1920;
        static int _y = 1080;
        static int ArraySize = _x * _y * 4;
        volatile static sbyte Fetch = 0;
        [Benchmark]

        unsafe public static void Nomal()
        {
            byte[] img = new byte[ArraySize];
            byte[] canvus = new byte[ArraySize];
            int h = _y;
            int w = _x;
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

                for (int y = 0; y < h; y++)
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

        unsafe public static void NomalParallel()
        {
            byte[] img = new byte[ArraySize];
            byte[] canvus = new byte[ArraySize];
            int h = _y;
            int w = _x;
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
                    var pprt = ppprt + 4 * y * w;
                    var pp = ppp + 4 * y * w;
                    for (int x = 0; x < w; x++)
                    {
                        byte l = (byte)(0.2 * pprt[0] + 0.6 * pprt[0] + 0.1 * pprt[0]);
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
            byte[] img = new byte[ArraySize];
            byte[] canvus = new byte[ArraySize];

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
                int h = _y;
                int w = _x;
                Vector128<float> r = Sse.SetVector128(.333f, .333f, .333f, .333f);
                Vector128<float> g = Sse.SetVector128(.666f, .666f, .666f, .666f);
                Vector128<float> b = Sse.SetVector128(.112f, .112f, .112f, .112f);
                Vector128<sbyte> maskr = Sse2.SetVector128(-1, -1, -1, 12, -1, -1, -1, 8, -1, -1, -1, 4, -1, -1, -1, 0);
                Vector128<sbyte> maskg = Sse2.SetVector128(-1, -1, -1, 13, -1, -1, -1, 9, -1, -1, -1, 5, -1, -1, -1, 1);
                Vector128<sbyte> maskb = Sse2.SetVector128(-1, -1, -1, 14, -1, -1, -1, 10, -1, -1, -1, 6, -1, -1, -1, 2);
                Vector128<sbyte> maskrtn = Sse2.SetVector128(-1, 12, 12, 12, -1, 8, 8, 8, -1, 4, 4, 4, -1, 0, 0, 0);

                Parallel.For(0, h, y =>
                   {
                       var pprt = ppprt + 4 * y * w;
                       var pp = ppp + 4 * y * w;
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


        unsafe public static void AlignmentedSIMDParallel()
        {
            AlignmentedArray<byte> img = new AlignmentedArray<byte>(ArraySize, 256);
            AlignmentedArray<byte> canvus = new AlignmentedArray<byte>(ArraySize, 256);

            var ptr = img.Pointer;
            var p = canvus.Pointer;
            {
                var ppprt = ptr;

                var ppp = p;
                int h = _y;
                int w = _x;
                Vector128<float> r = Sse.SetVector128(.333f, .333f, .333f, .333f);
                Vector128<float> g = Sse.SetVector128(.666f, .666f, .666f, .666f);
                Vector128<float> b = Sse.SetVector128(.112f, .112f, .112f, .112f);
                Vector128<sbyte> maskr = Sse2.SetVector128(-1, -1, -1, 12, -1, -1, -1, 8, -1, -1, -1, 4, -1, -1, -1, 0);
                Vector128<sbyte> maskg = Sse2.SetVector128(-1, -1, -1, 13, -1, -1, -1, 9, -1, -1, -1, 5, -1, -1, -1, 1);
                Vector128<sbyte> maskb = Sse2.SetVector128(-1, -1, -1, 14, -1, -1, -1, 10, -1, -1, -1, 6, -1, -1, -1, 2);
                Vector128<sbyte> maskrtn = Sse2.SetVector128(-1, 12, 12, 12, -1, 8, 8, 8, -1, 4, 4, 4, -1, 0, 0, 0);

                Parallel.For(0, h, y =>
                {
                    var pprt = ppprt + 4 * y * w;
                    var pp = ppp + 4 * y * w;
                    for (int x = 0; x < w; x += 4)
                    {
                        var tmp0 = Sse.StaticCast<byte, sbyte>(Sse2.LoadAlignedVector128(pprt));

                        var t0 = Ssse3.Shuffle(tmp0, maskr);
                        var t1 = Ssse3.Shuffle(tmp0, maskg);
                        var t2 = Ssse3.Shuffle(tmp0, maskb);

                        var tmp6 = Sse2.ConvertToVector128Single(Sse.StaticCast<sbyte, int>(t0));
                        var tmp7 = Sse2.ConvertToVector128Single(Sse.StaticCast<sbyte, int>(t1));
                        var tmp8 = Sse2.ConvertToVector128Single(Sse.StaticCast<sbyte, int>(t2));


                        var tmp13 = Sse.Add(Sse.Add(Sse.Multiply(tmp6, r), Sse.Multiply(tmp7, g)), Sse.Multiply(tmp8, b));

                        var tmp14 = Sse.StaticCast<int, sbyte>(Sse2.ConvertToVector128Int32(tmp13));

                        var tmp18 = Ssse3.Shuffle(tmp14, maskrtn);

                        Sse2.StoreAligned(pp, Sse.StaticCast<sbyte, byte>(tmp18));
                        pp += 16;
                        pprt += 16;
                    }
                });
            }
        }
       
        [Benchmark]
        unsafe public static void AlignmentedNotmpSIMDParallel()
        {

            AlignmentedArray<byte> img = new AlignmentedArray<byte>(ArraySize, 32);
            AlignmentedArray<byte> canvus = new AlignmentedArray<byte>(ArraySize, 32);

            var ptr = img.Pointer;
            var p = canvus.Pointer;
            {
                var ppprt = ptr;

                var ppp = p;
                int h = _y;
                int w = _x;
                Vector128<float> r = Sse.SetVector128(.333f, .333f, .333f, .333f);
                Vector128<float> g = Sse.SetVector128(.666f, .666f, .666f, .666f);
                Vector128<float> b = Sse.SetVector128(.112f, .112f, .112f, .112f);
                Vector128<sbyte> maskr = Sse2.SetVector128(-1, -1, -1, 12, -1, -1, -1, 8, -1, -1, -1, 4, -1, -1, -1, 0);
                Vector128<sbyte> maskg = Sse2.SetVector128(-1, -1, -1, 13, -1, -1, -1, 9, -1, -1, -1, 5, -1, -1, -1, 1);
                Vector128<sbyte> maskb = Sse2.SetVector128(-1, -1, -1, 14, -1, -1, -1, 10, -1, -1, -1, 6, -1, -1, -1, 2);
                Vector128<sbyte> maskrtn = Sse2.SetVector128(-1, 12, 12, 12, -1, 8, 8, 8, -1, 4, 4, 4, -1, 0, 0, 0);
                Parallel.For(0, 40, i =>
                 {
                     sbyte* pprt = (sbyte*)ppprt + 4 * _y / 40 * i * w;
                     sbyte* pp = (sbyte*)ppp + 4 * _y / 40 * i * w;
                     int last = _y / 40 * i + _y / 40;

                     for (int y = _y / 40 * i; y < last; y++)
                     {
                         for (int x = 0; x < w; x += 4)
                         {
                          var   tmp0 = Sse2.LoadAlignedVector128(pprt);
                          var   t0 = Ssse3.Shuffle(tmp0, maskr);
                          var   t1 = Ssse3.Shuffle(tmp0, maskg);
                          var   t2 = Ssse3.Shuffle(tmp0, maskb);
                          var   tmp6 = Sse2.ConvertToVector128Single(Sse.StaticCast<sbyte, int>(t0));
                          var   tmp7 = Sse2.ConvertToVector128Single(Sse.StaticCast<sbyte, int>(t1));
                          var   tmp8 = Sse2.ConvertToVector128Single(Sse.StaticCast<sbyte, int>(t2));
                          var   tmp13 = Sse.Add(Sse.Add(Sse.Multiply(tmp6, r), Sse.Multiply(tmp7, g)), Sse.Multiply(tmp8, b));
                          var   tmp14 = Sse.StaticCast<int, sbyte>(Sse2.ConvertToVector128Int32(tmp13));


                             Sse2.StoreAlignedNonTemporal(pp, Ssse3.Shuffle(tmp14, maskrtn));
                             pp += 16;
                             pprt += 16;
                         }
                     }
                 });
            }
        }
    
        unsafe public static void SIMDAVXParallell()
        {
            byte[] img = new byte[ArraySize];
            byte[] canvus = new byte[ArraySize];
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
                int h =_y ;
                int w = _x;
                Vector256<float> r = Avx.SetVector256(.333f, .333f, .333f, .333f, .333f, .333f, .333f, .333f);
                Vector256<float> g = Avx.SetVector256(.666f, .666f, .666f, .666f, .666f, .666f, .666f, .666f);
                Vector256<float> b = Avx.SetVector256(.112f, .112f, .112f, .112f, .112f, .112f, .112f, .112f);
                Vector128<sbyte> maskr = Sse2.SetVector128(-1, -1, -1, 12, -1, -1, -1, 8, -1, -1, -1, 4, -1, -1, -1, 0);
                Vector128<sbyte> maskg = Sse2.SetVector128(-1, -1, -1, 13, -1, -1, -1, 9, -1, -1, -1, 5, -1, -1, -1, 1);
                Vector128<sbyte> maskb = Sse2.SetVector128(-1, -1, -1, 14, -1, -1, -1, 10, -1, -1, -1, 6, -1, -1, -1, 2);
                Vector128<sbyte> maskrtn = Sse2.SetVector128(-1, 12, 12, 12, -1, 8, 8, 8, -1, 4, 4, 4, -1, 0, 0, 0);
                Parallel.For(0, h, y =>
                {
                    Vector256<sbyte> datr = Avx.SetZeroVector256<sbyte>();
                    Vector256<sbyte> datg = Avx.SetZeroVector256<sbyte>();
                    Vector256<sbyte> datb = Avx.SetZeroVector256<sbyte>();
                    Vector256<sbyte> rtn = Avx.SetZeroVector256<sbyte>();
              
                    var pprt = ppprt + 4 * y * w;
                    var pp = ppp + 4 * y * w;
                    for (int x = 0; x < w; x += 8)
                    {
                        var tmp00 = Avx.StaticCast<byte, sbyte>(Avx.LoadVector256(pprt));
                        pprt += 32;
                        var tmp0 = Avx2.ExtractVector128(tmp00, 0);
                        var tmp1 = Avx2.ExtractVector128(tmp00, 1);

                        var t00 = Ssse3.Shuffle(tmp0, maskr);
                        var t01 = Ssse3.Shuffle(tmp0, maskg);
                        var t02 = Ssse3.Shuffle(tmp0, maskb);

                        var t10 = Ssse3.Shuffle(tmp1, maskr);
                        var t11 = Ssse3.Shuffle(tmp1, maskg);
                        var t12 = Ssse3.Shuffle(tmp1, maskb);
                        Avx.InsertVector128(datr,t00, 0);
                        Avx.InsertVector128(datr,t10, 1);
                        Avx.InsertVector128(datg,t01, 0);
                        Avx.InsertVector128(datg,t11, 1);
                        Avx.InsertVector128(datb,t02, 0);
                        Avx.InsertVector128(datb,t12, 1);
                       
                        var tmp8 = Avx.ConvertToVector256Single(Avx.StaticCast<sbyte, int>(datr));
                        var tmp9 = Avx.ConvertToVector256Single(Avx.StaticCast<sbyte, int>(datg));
                        var tmp10 = Avx.ConvertToVector256Single(Avx.StaticCast<sbyte, int>(datb));


                        var tmp13 = Avx.Add(Avx.Add(Avx.Multiply(tmp8, r), Avx.Multiply(tmp9, g)), Avx.Multiply(tmp10, b));

                        var tmp14 = Avx.StaticCast<int, sbyte>(Avx.ConvertToVector256Int32(tmp13));

                        var tmp15 = Avx2.ExtractVector128(tmp14, 0);
                        var tmp16 = Avx2.ExtractVector128(tmp14, 1);
                        var tmp18 = Ssse3.Shuffle(tmp15, maskrtn);
                        var tmp19 = Ssse3.Shuffle(tmp16, maskrtn);

                        Avx.InsertVector128(rtn, tmp18, 0);
                        Avx.InsertVector128(rtn, tmp19, 1);


                      Avx.Store(pp, Avx.StaticCast<sbyte, byte>(rtn));
                        pp += 32;
                    }
                });
            }
        }

         unsafe public static void AlignmentedSIMDAVXParallell()
        {
            AlignmentedArray<byte> img = new AlignmentedArray<byte>(ArraySize, 256);
            AlignmentedArray<byte> canvus = new AlignmentedArray<byte>(ArraySize, 256);
            var ptr = img.Pointer;
            var p = canvus.Pointer;
            {
                 var ppprt = ptr;

                var ppp = p;
                int h =_y ;
                int w = _x;
                Vector256<float> r = Avx.SetVector256(.333f, .333f, .333f, .333f, .333f, .333f, .333f, .333f);
                Vector256<float> g = Avx.SetVector256(.666f, .666f, .666f, .666f, .666f, .666f, .666f, .666f);
                Vector256<float> b = Avx.SetVector256(.112f, .112f, .112f, .112f, .112f, .112f, .112f, .112f);
                Vector128<sbyte> maskr = Sse2.SetVector128(-1, -1, -1, 12, -1, -1, -1, 8, -1, -1, -1, 4, -1, -1, -1, 0);
                Vector128<sbyte> maskg = Sse2.SetVector128(-1, -1, -1, 13, -1, -1, -1, 9, -1, -1, -1, 5, -1, -1, -1, 1);
                Vector128<sbyte> maskb = Sse2.SetVector128(-1, -1, -1, 14, -1, -1, -1, 10, -1, -1, -1, 6, -1, -1, -1, 2);
                Vector128<sbyte> maskrtn = Sse2.SetVector128(-1, 12, 12, 12, -1, 8, 8, 8, -1, 4, 4, 4, -1, 0, 0, 0);
                Parallel.For(0, h, y =>
                //  for (int y = 0; y < h; y++)
                {
                    Vector256<sbyte> datr = Avx.SetZeroVector256<sbyte>();
                    Vector256<sbyte> datg = Avx.SetZeroVector256<sbyte>();
                    Vector256<sbyte> datb = Avx.SetZeroVector256<sbyte>();
                    Vector256<sbyte> rtn = Avx.SetZeroVector256<sbyte>();

                    var pprt = ppprt + 4 * y * w;
                    var pp = ppp + 4  * y *w;
                    for (int x = 0; x < w; x += 8)
                    {
                        var tmp00 = Avx.StaticCast<byte, sbyte>(Avx.LoadAlignedVector256(pprt));
                        pprt += 32;
                        var tmp0 = Avx2.ExtractVector128(tmp00, 0);
                        var tmp1 = Avx2.ExtractVector128(tmp00, 1);

                        var t00 = Ssse3.Shuffle(tmp0, maskr);
                        var t01 = Ssse3.Shuffle(tmp0, maskg);
                        var t02 = Ssse3.Shuffle(tmp0, maskb);

                        var t10 = Ssse3.Shuffle(tmp1, maskr);
                        var t11 = Ssse3.Shuffle(tmp1, maskg);
                        var t12 = Ssse3.Shuffle(tmp1, maskb);
                        Avx.InsertVector128(datr, t00, 0);
                        Avx.InsertVector128(datr, t10, 1);
                        Avx.InsertVector128(datg, t01, 0);
                        Avx.InsertVector128(datg, t11, 1);
                        Avx.InsertVector128(datb, t02, 0);
                        Avx.InsertVector128(datb, t12, 1);

                        var tmp8 = Avx.ConvertToVector256Single(Avx.StaticCast<sbyte, int>(datr));
                        var tmp9 = Avx.ConvertToVector256Single(Avx.StaticCast<sbyte, int>(datg));
                        var tmp10 = Avx.ConvertToVector256Single(Avx.StaticCast<sbyte, int>(datb));


                        var tmp13 = Avx.Add(Avx.Add(Avx.Multiply(tmp8, r), Avx.Multiply(tmp9, g)), Avx.Multiply(tmp10, b));

                        var tmp14 = Avx.StaticCast<int, sbyte>(Avx.ConvertToVector256Int32(tmp13));

                        var tmp15 = Avx2.ExtractVector128(tmp14, 0);
                        var tmp16 = Avx2.ExtractVector128(tmp14, 1);
                        var tmp18 = Ssse3.Shuffle(tmp15, maskrtn);
                        var tmp19 = Ssse3.Shuffle(tmp16, maskrtn);

                        Avx.InsertVector128(rtn, tmp18, 0);
                        Avx.InsertVector128(rtn, tmp19, 1);


                        Avx.StoreAligned(pp, Avx.StaticCast<sbyte, byte>(rtn));
                        pp += 32;
                    }
                });
                
            }
        }

     
        unsafe public static void SIMD()
        {
            byte[] img = new byte[ArraySize];
            byte[] canvus = new byte[ArraySize];

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
                int h =_y ;
                int w = _x;
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

(*
[SimpleJob(RuntimeMoniker.Net472, baseline: true)]
[SimpleJob(RuntimeMoniker.NetCoreApp30)]
[SimpleJob(RuntimeMoniker.NativeAot70)]
[SimpleJob(RuntimeMoniker.Mono)]
[RPlotExporter]
public class Md5VsSha256
{
    private SHA256 sha256 = SHA256.Create();
    private MD5 md5 = MD5.Create();
    private byte[] data;

    [Params(1000, 10000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        data = new byte[N];
        new Random(42).NextBytes(data);
    }

    [Benchmark]
    public byte[] Sha256() => sha256.ComputeHash(data);

    [Benchmark]
    public byte[] Md5() => md5.ComputeHash(data);
}
*)
//F#
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Jobs
open System.Security.Cryptography
open System
open BenchmarkDotNet.Running
open System.Runtime.CompilerServices
//[<SimpleJob(RuntimeMoniker.Net472, baseline=true)>]
//[<SimpleJob(RuntimeMoniker.NetCoreApp30)>]
//[<SimpleJob(RuntimeMoniker.NativeAot70)>]

module Candidates = 

    //let inline private retype (x : 'a) : 'b = (# "" x : 'b #)
    let inline nthroot n (A:double) : double =
        let rec f x =
            let m = n - 1
            let m' = x * double m
            let x' = (m' + A / (pown x m))
            let x'' = LanguagePrimitives.DivideByInt< double > x' n
            if float (abs(x'' - x))  < 1e-9 then 
                x''
            else
                f x''
        f (LanguagePrimitives.DivideByInt< double > A n)
    let inline multByInt32_typecheck_inline (x: 'T) (n: int) : 'T = 
        if LanguagePrimitives.PhysicalEquality typeof<'T> typeof<float32> then
            unbox(box x) * (float32 n) |> box |> unbox
        elif LanguagePrimitives.PhysicalEquality typeof<'T> typeof<float> then
            unbox(box x) * (double n) |> box |> unbox
        elif LanguagePrimitives.PhysicalEquality typeof<'T> typeof<decimal> then
            unbox(box x) * (decimal n) |> box |> unbox
        else
            failwith "Type mismatch"
    let inline nthroot_typecheck_inline n (A:'T) : 'T =
        let rec f x =
            let m = n - 1
            let m' = multByInt32_typecheck_inline x m
            let x' = (m' + A / (pown x m))
            let x'' = LanguagePrimitives.DivideByInt< 'T > x' n
            if float (abs(x'' - x))  < 1e-9 then 
                x''
            else
                f x''
        f (LanguagePrimitives.DivideByInt< 'T > A n)
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let multByInt32_typecheck (x: 'T) (n: int)  : 'T = 
        if LanguagePrimitives.PhysicalEquality typeof<'T> typeof<float32> then
            unbox(box x) * (float32 n) |> box |> unbox
        elif LanguagePrimitives.PhysicalEquality typeof<'T> typeof<float> then
            unbox(box x) * (double n) |> box |> unbox
        elif LanguagePrimitives.PhysicalEquality typeof<'T> typeof<decimal> then
            unbox(box x) * (decimal n) |> box |> unbox
        else
            failwith "Type mismatch"
    let inline nthroot_typecheck n (A:'T) : 'T =
        let rec f x =
            let m = n - 1
            let m' = multByInt32_typecheck x m
            let x' = (m' + A / (pown x m))
            let x'' = LanguagePrimitives.DivideByInt< 'T > x' n
            if float (abs(x'' - x))  < 1e-9 then 
                x''
            else
                f x''
        f (LanguagePrimitives.DivideByInt< 'T > A n)

open Candidates

[<RPlotExporter>]
type NthrootDouble() =
    let rnd = Random 42
    let data = Array.init 10000 (fun _ -> rnd.NextDouble())
    let sink = Array.zeroCreate<double> 10000

    [<Params(2, 3, 5, 10)>]
    member val n = 0 with get, set

    [<Benchmark>]
    member this.nthroot() = 
        for i = 0 to data.Length - 1 do
            sink.[i] <- Candidates.nthroot this.n data.[i]

    [<Benchmark>]
    member this.nthroot_typecheck() = 
        for i = 0 to data.Length - 1 do
            sink.[i] <- nthroot_typecheck this.n data.[i]

    [<Benchmark>]
    member this.nthroot_typecheck_inline() = 
        for i = 0 to data.Length - 1 do
            sink.[i] <- nthroot_typecheck_inline this.n data.[i]


[<EntryPoint>]
let main argv =
    let summary = BenchmarkRunner.Run<NthrootDouble>()
    0 // return an integer exit code
namespace EnumerableTest.Runner.Console

open System
open System.IO
open System.Reflection
open Argu
open Basis.Core
open EnumerableTest.Runner

module Assembly =
  let tryLoadFile (file: FileInfo) =
    try
      let assemblyName = AssemblyName.GetAssemblyName(file.FullName)
      Assembly.Load(assemblyName) |> Some
    with
    | _ -> None

module Program =
  let run isVerbose timeout (assemblyFiles: seq<FileInfo>) =
    let (assemblies, unloadedFiles) =
      assemblyFiles
      |> Seq.paritionMap Assembly.tryLoadFile
    let results =
      assemblies
      |> Seq.collect (TestSuite.ofAssemblyAsync timeout)
      |> Observable.ofParallel
    let printer =
      TestPrinter(Console.Out, Console.BufferWidth - 1, isVerbose)
      |> tap (fun p -> p.PrintUnloadedFiles(unloadedFiles))
    let counter = AssertionCounter()
    results.Subscribe(printer) |> ignore<IDisposable>
    results.Subscribe(counter) |> ignore<IDisposable>
    results.Connect()
    results |> Observable.wait
    printer.PrintSummaryAsync(counter.Current) |> Async.RunSynchronously
    counter.IsPassed

  [<EntryPoint>]
  let main _ =
    let thisFile = FileInfo(Assembly.GetExecutingAssembly().Location)
    let assemblyFiles =
      FileSystemInfo.getTestAssemblies thisFile
      |> Seq.append AppArgument.files
      |> Seq.distinctBy (fun file -> file.FullName)
    MarshalValue.Recursion <- AppArgument.recursion
    let isPassed =
      run AppArgument.isVerbose AppArgument.timeout assemblyFiles
    let exitCode =
      if isPassed
        then 0
        else -1
    exitCode

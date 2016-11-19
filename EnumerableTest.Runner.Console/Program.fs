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
    if isVerbose then
      printfn "assemblies:"
      for file in assemblyFiles do
        printfn "  - %s" file.FullName
    let results =
      assemblyFiles
      |> Seq.choose Assembly.tryLoadFile
      |> Seq.collect (TestSuite.ofAssemblyAsync timeout)
      |> Observable.ofParallel
    let printer = TestPrinter(Console.Out, Console.BufferWidth - 1, isVerbose)
    let counter = AssertionCounter()
    results.Subscribe(printer) |> ignore<IDisposable>
    results.Subscribe(counter) |> ignore<IDisposable>
    results.Connect()
    results |> Observable.wait
    printer.PrintSummaryAsync(counter.Current) |> Async.RunSynchronously
    let exitCode =
      if counter.IsAllGreen
        then 0
        else -1
    exitCode

  [<EntryPoint>]
  let main _ =
    let thisFile = FileInfo(Assembly.GetExecutingAssembly().Location)
    let assemblyFiles =
      FileSystemInfo.getTestAssemblies thisFile
      |> Seq.append AppArgument.files
    run AppArgument.isVerbose AppArgument.timeout assemblyFiles

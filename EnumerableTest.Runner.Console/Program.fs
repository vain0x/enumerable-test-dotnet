namespace EnumerableTest.Runner.Console

open System
open System.IO
open System.Reflection
open EnumerableTest.Runner

module Program =
  [<EntryPoint>]
  let main argv =
    let thisFile = FileInfo(Assembly.GetExecutingAssembly().Location)
    let assemblyFiles =
      FileSystemInfo.getTestAssemblies thisFile
    let results =
      assemblyFiles
      |> Seq.map (fun file -> Assembly.LoadFile(file.FullName))
      |> Seq.collect TestSuite.ofAssemblyLazy
      |> Seq.map Async.run
      |> Observable.ofParallel
    let printer = TestPrinter(Console.Out, Console.BufferWidth - 1)
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

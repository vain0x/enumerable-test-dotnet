namespace EnumerableTest.Runner.Console

open System
open System.IO
open System.Reflection
open EnumerableTest.Runner

module Program =
  [<EntryPoint>]
  let main argv =
    let files =
      argv |> Seq.map (fun arg -> FileInfo(arg))
    let assemblies =
      files |> Seq.map (fun file -> Assembly.LoadFile(file.FullName))
    let testSuite =
      assemblies |> Seq.collect TestSuite.ofAssembly
    let result =
      testSuite |> TestSuite.runAsync |> Async.RunSynchronously
    let isAllPassed =
      result
      |> Seq.collect TestClassResult.allTestResult
      |> Seq.forall (function | Passed _ -> true | _ -> false)
    let exitCode =
      if isAllPassed
        then 0
        else -1
    let printer = TestPrinter(Console.Out, Console.BufferWidth - 1)
    printer.PrintAsync(result) |> Async.RunSynchronously
    exitCode

namespace EnumerableTest.Runner.Console

open System
open System.Collections.Generic
open System.IO
open System.Reactive.Concurrency
open System.Threading
open System.Threading.Tasks
open EnumerableTest
open EnumerableTest.Runner
open EnumerableTest.Sdk
open Basis.Core

type TestPrinter(writer: TextWriter, width: int, isVerbose: bool) =
  let printer = StructuralTextWriter(writer)

  let queue = ConcurrentActionQueue()

  let printSeparatorAsync c =
    printer.WriteLineAsync(String.replicate (width - printer.IndentLength) c)
    
  let printHardSeparatorAsync () =
    printSeparatorAsync "="

  let printSoftSeparatorAsync () =
    printSeparatorAsync "-"

  let printExceptionAsync source (e: exn) =
    async {
      do! printer.WriteLineAsync(sprintf "RUNTIME ERROR in %s" source)
      use indenting = printer.AddIndent()
      do! printer.WriteLineAsync(string e)
    }

  let rec printMarshalPropertiesAsync properties =
    async {
      use indenting = printer.AddIndent()
      for KeyValue (key, result) in properties do
        match (result: MarshalResult).ToResult() with
        | Success value ->
          do! printer.WriteLineAsync(sprintf "%s: %s" key value.String)
          do! printMarshalPropertiesAsync value.Properties
        | Failure e ->
          do! printer.WriteLineAsync(sprintf "%s (!): %s" key e.String)
          do! printMarshalPropertiesAsync e.Properties
    }

  let rec printMarshalValueAsync key (value: MarshalValue) =
    async {
      do! printer.WriteLineAsync(sprintf "%s: %s" key value.String)
      do! printMarshalPropertiesAsync value.Properties
    }

  let printTestDataAsync =
    function
    | EmptyTestData ->
      Async.result ()
    | DictionaryTestData testData ->
      async {
        for KeyValue (key, value) in testData do
          do! printMarshalValueAsync key value
      }

  let printAssertionTestAsync i (result: SerializableAssertionTest) =
    async {
      let mark =
        if result.IsPassed
          then "."
          else "x"
      do! printer.WriteLineAsync(sprintf "%d. %s %s" (i + 1) mark result.Name)
      use indenting = printer.AddIndent()
      if result.IsPassed |> not then
        do! result.Data |> printTestDataAsync
    }

  let rec printTestAsync i (test: SerializableTest) =
    async {
      match test with
      | AssertionTest test ->
        return! printAssertionTestAsync i test
      | GroupTest test ->
        do! printer.WriteLineAsync(sprintf "Group: %s" test.Name)
        use indenting = printer.AddIndent()
        do! test.Data |> printTestDataAsync
        for (i, test) in test.Tests |> Seq.indexed do
          do! printTestAsync i test
    }

  let printTestMethodAsync i (testMethodResult: TestMethodResult) =
    async {
      if isVerbose || testMethodResult |> TestMethodResult.isPassed |> not then
        do! printSoftSeparatorAsync ()
        do! printer.WriteLineAsync(sprintf "Method: %s" testMethodResult.MethodName)
        use indenting = printer.AddIndent()
        for (i, test) in testMethodResult.Result.Tests |> Seq.indexed do
          do! printTestAsync i test
        match testMethodResult.DisposingError with
        | Some e ->
          do! printMarshalValueAsync "RUNTIME ERROR in Dispose" e
        | None -> ()
    }

  let printNotCompletedMethodsAsync (testMethodSchemas: array<TestMethodSchema>) =
    async {
      if testMethodSchemas |> Array.isEmpty |> not then
        do! printSoftSeparatorAsync ()
        do! printer.WriteLineAsync("Not completed methods:")
        use indenting = printer.AddIndent()
        for (i, schema) in testMethodSchemas |> Seq.indexed do
          do! printer.WriteLineAsync(sprintf "%d. %s" i schema.MethodName)
    }

  let printAsync (testClassResult: TestClassResult) =
    async {
      if isVerbose || testClassResult |> TestClassResult.isPassed |> not then
        do! printHardSeparatorAsync ()
        do! printer.WriteLineAsync(sprintf "Type: %s" testClassResult.TypeFullName)
        use indenting = printer.AddIndent()
        match testClassResult.InstantiationError with
        | Some e ->
          do! printExceptionAsync "constructor" e
        | None ->
          for (i, testMethodResult) in testClassResult.TestMethodResults |> Seq.indexed do
            do! testMethodResult |> printTestMethodAsync i
          do! printNotCompletedMethodsAsync testClassResult.NotCompletedMethods
    }

  let printWarningsAsync (warnings: IReadOnlyList<Warning>) =
    async {
      if warnings.Count > 0 then
        do! printHardSeparatorAsync ()
        do! printer.WriteLineAsync("Warnings:")
        use indenting = printer.AddIndent()
        for (i, warning) in warnings |> Seq.indexed do
          do! printSoftSeparatorAsync ()
          do! printer.WriteLineAsync(sprintf "%d. %s" i warning.Message)
          if isVerbose then
            use indenting = printer.AddIndent()
            for KeyValue (key, value) in warning.Data do
              do! printer.WriteLineAsync(sprintf "%s: %A" key value)
    }

  let printSummaryAsync (count: AssertionCount) =
    async {
      do! printHardSeparatorAsync ()
      let message =
        sprintf "Total: %d, Violated: %d, Error: %d, Not completed: %d"
          count.TotalCount
          count.ViolatedCount
          count.ErrorCount
          count.NotCompletedCount
      return! printer.WriteLineAsync(message)
    }

  member this.PrintWarningsAsync(warnings) =
    queue.Enqueue(printWarningsAsync warnings)

  member this.PrintSummaryAsync(count) =
    queue.Enqueue(printSummaryAsync count)

  member this.QueueGotEmpty =
    queue.GotEmpty

  interface IObserver<TestClassResult> with
    override this.OnNext(testClassResult) =
      queue.Enqueue(printAsync testClassResult)

    override this.OnError(_) = ()

    override this.OnCompleted() = ()

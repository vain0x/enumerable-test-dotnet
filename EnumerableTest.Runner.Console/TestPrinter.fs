namespace EnumerableTest.Runner.Console

open System
open System.IO
open EnumerableTest
open EnumerableTest.Runner
open EnumerableTest.Sdk
open Basis.Core

type TestPrinter(writer: TextWriter, width: int) =
  let printer = StructuralTextWriter(writer)

  let printSeparatorAsync () =
    printer.WriteLineAsync(String.replicate (width - printer.IndentLength) "-")

  let printExceptionAsync source (e: exn) =
    async {
      do! printer.WriteLineAsync(sprintf "RUNTIME ERROR in %s" source)
      use indenting = printer.AddIndent()
      do! printer.WriteLineAsync(string e)
    }

  let printAssertionAsync i testName (result: Assertion) =
    async {
      let mark =
        match result with
        | Passed          -> "."
        | Violated _      -> "*"
      do! printer.WriteLineAsync(sprintf "%d. %s %s" (i + 1) mark testName)
      use indenting = printer.AddIndent()
      match result with
      | Passed            -> ()
      | Violated message  ->
        return! printer.WriteLineAsync(message)
    }

  let rec printTestAsync i (test: Test) =
    async {
      match test with
      | AssertionTest test ->
        return! printAssertionAsync i test.Name test.Assertion
      | GroupTest test ->
        do! printer.WriteLineAsync(sprintf "Group: %s" test.Name)
        use indenting = printer.AddIndent()
        for (i, test) in test.Tests |> Seq.indexed do
          do! printTestAsync i test
    }

  let printTestMethodAsync i (testMethod: TestMethod) =
    async {
      if testMethod |> TestMethod.isPassed |> not then
        do! printer.WriteLineAsync(sprintf "Method: %s" testMethod.MethodName)
        use indenting = printer.AddIndent()
        for (i, test) in testMethod.Result.Tests |> Seq.indexed do
          do! printTestAsync i test
        match testMethod.DisposingError with
        | Some e ->
          do! printExceptionAsync "Dispose" e
        | None -> ()
    }

  member this.PrintSummaryAsync(count: AssertionCount) =
    async {
      do! printSeparatorAsync ()
      let message =
        sprintf "Total: %d, Violated: %d, Error: %d"
          count.TotalCount
          count.ViolatedCount
          count.ErrorCount
      return! printer.WriteLineAsync(message)
    }

  member this.PrintAsync(testClass: TestClass) =
    async {
      if testClass |> TestClass.isPassed |> not then
        do! printSeparatorAsync ()
        do! printer.WriteLineAsync(sprintf "Type: %s" testClass.TypeFullName)
        use indenting = printer.AddIndent()
        match testClass.InstantiationError with
        | Some e ->
          do! printExceptionAsync "constructor" e
        | None ->
          for (i, testMethod) in testClass.Result |> Seq.indexed do
            do! testMethod |> printTestMethodAsync i
    }

  interface IObserver<TestClass> with
    override this.OnNext(testClass) =
      this.PrintAsync(testClass) |> Async.RunSynchronously

    override this.OnError(_) = ()

    override this.OnCompleted() = ()

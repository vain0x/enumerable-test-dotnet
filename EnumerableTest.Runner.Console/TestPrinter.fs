namespace EnumerableTest.Runner.Console

open System
open System.IO
open EnumerableTest
open EnumerableTest.Runner
open Basis.Core

type TestPrinter(writer: TextWriter, width: int) =
  let printer = StructuralTextWriter(writer)

  let printSeparatorAsync () =
    printer.WriteLineAsync(String.replicate (width - printer.IndentLength) "-")

  let printTestErrorAsync testError =
    async {
      let methodName  = testError |> TestError.errorMethodName
      do! printer.WriteLineAsync(sprintf "RUNTIME ERROR in %s" methodName)
      return! printer.WriteLineAsync(string testError.Error)
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

  let printTestMethodResultAsync i (testMethodResult: TestMethodResult) =
    async {
      match testMethodResult with
      | Success test when test.IsPassed -> ()
      | Success test ->
        do! printer.WriteLineAsync(sprintf "Method: %s" test.Name)
        use indenting = printer.AddIndent()
        for  (i, test) in test.Tests |> Seq.indexed do
          do! printTestAsync i test
      | Failure testError ->
        let methodName = testError |> TestError.methodName
        do! printer.WriteLineAsync(sprintf "Method: %s" methodName)
        use indenting = printer.AddIndent()
        return! printTestErrorAsync testError
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

  member this.PrintAsync(testClassResult: TestClassResult) =
    async {
      if testClassResult |> TestClassResult.isAllPassed |> not then
        let (testClass, testMethodResults) = testClassResult
        do! printSeparatorAsync ()
        do! printer.WriteLineAsync(sprintf "Type: %s" testClass.Type.FullName)
        use indenting = printer.AddIndent()
        for (testIndex, testMethodResult) in testMethodResults |> Seq.indexed do
          do! testMethodResult |> printTestMethodResultAsync testIndex
    }

  interface IObserver<TestClassResult> with
    override this.OnNext(testClassResult) =
      this.PrintAsync(testClassResult) |> Async.RunSynchronously

    override this.OnError(_) = ()

    override this.OnCompleted() = ()

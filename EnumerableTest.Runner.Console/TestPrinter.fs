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

  let printAssertionResultAsync i testName (result: AssertionResult) =
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
      | AssertionTest assertionResult ->
        return! printAssertionResultAsync i test.Name assertionResult
      | GroupTest tests ->
        do! printer.WriteLineAsync(sprintf "test group %s" test.Name)
        use indenting = printer.AddIndent()
        for (i, test) in tests |> Seq.indexed do
          do! printTestAsync i test
    }

  let printTestMethodResultAsync i (testMethodResult: TestMethodResult) =
    async {
      match testMethodResult with
      | Success test when test.IsPassed -> ()
      | Success test ->
        do! printer.WriteLineAsync(sprintf "method %s" test.Name)
        use indenting = printer.AddIndent()
        for  (i, test) in test.Tests |> Seq.indexed do
          do! printTestAsync i test
      | Failure testError ->
        let methodName = testError |> TestError.methodName
        do! printer.WriteLineAsync(sprintf "method %s" methodName)
        use indenting = printer.AddIndent()
        return! printTestErrorAsync testError
    }

  let printSummaryAsync testSuiteResult =
    let (count, violateCount, errorCount) = testSuiteResult |> TestSuiteResult.countResults
    let message = sprintf "Total: %d, Violated: %d, Error: %d" count violateCount errorCount
    printer.WriteLineAsync(message)

  member this.PrintAsync(testSuiteResult: TestSuiteResult) =
    async {
      let testSuiteResult =
        testSuiteResult
        |> Seq.filter (TestClassResult.isAllPassed >> not)
        |> Seq.toArray
      for (typeIndex, (testClass, testMethodResults)) in testSuiteResult |> Seq.indexed do
        if typeIndex > 0 then
          do! printSeparatorAsync ()
        do! printer.WriteLineAsync(sprintf "type %s" testClass.Type.FullName)
        use indenting = printer.AddIndent()
        for (testIndex, testMethodResult) in testMethodResults |> Seq.indexed do
          do! testMethodResult |> printTestMethodResultAsync testIndex
      if testSuiteResult.Length > 0 then
        do! printSeparatorAsync ()
      do! printSummaryAsync testSuiteResult
    }

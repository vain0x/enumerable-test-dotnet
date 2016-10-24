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
      let methodName  = testError |> TestError.methodName
      do! printer.WriteLineAsync(sprintf "RUNTIME ERROR in %s" methodName)
      return! printer.WriteLineAsync(string testError.Error)
    }

  let printTestResultAsync i testName (result: Result<TestResult, TestError>) =
    async {
      let mark =
        match result with
        | Passed _              -> "."
        | Violated _            -> "*"
        | Error _               -> "!"
      do! printer.WriteLineAsync(sprintf "%d. %s %s" (i + 1) mark testName)
      use indenting = printer.AddIndent()
      return!
        match result with
        | Passed _              -> Async.result ()
        | Violated message      -> printer.WriteLineAsync(message)
        | Error testError       -> testError |> printTestErrorAsync
    }

  let rec printTestAsync i (test: Test) =
    test.Match
      ( fun testResult ->
          printTestResultAsync i test.Name (testResult |> Success)
      , fun tests ->
          async {
            do! printer.WriteLineAsync(sprintf "method %s" test.Name)
            use indenting = printer.AddIndent()
            for (i, test) in tests |> Seq.indexed do
              do! printTestAsync i test
          }
      )

  let printSummaryAsync testSuiteResult =
    let (count, violateCount, errorCount) = testSuiteResult |> TestSuiteResult.countResults
    let message = sprintf "Total: %d, Violated: %d, Error: %d" count violateCount errorCount
    printer.WriteLineAsync(message)

  member this.PrintAsync(testSuiteResult: TestSuiteResult) =
    async {
      // Don't print all-green test classes.
      let testSuiteResult =
        testSuiteResult |> Seq.filter
          (fun testObjectResult ->
            testObjectResult |> TestObjectResult.allTestResult
            |> Seq.exists (function | Passed _ -> false | _ -> true)
          )
        |> Seq.toArray
      for (typeIndex, (testObject, tests)) in testSuiteResult |> Seq.indexed do
        if typeIndex > 0 then
          do! printSeparatorAsync ()
        do! printer.WriteLineAsync(sprintf "type %s" testObject.Type.FullName)
        use indenting = printer.AddIndent()
        for (testIndex, test) in tests |> Seq.indexed do
          match test with
          | Success test when test.IsPassed -> ()
          | Success test ->
            do! printTestAsync testIndex test
          | Failure error ->
            do! printTestErrorAsync error
      if testSuiteResult.Length > 0 then
        do! printSeparatorAsync ()
      do! printSummaryAsync testSuiteResult
    }

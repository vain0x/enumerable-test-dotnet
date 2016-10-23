namespace EnumerableTest.Runner.Console

open System
open System.IO
open EnumerableTest
open EnumerableTest.Runner

type TestPrinter(writer: TextWriter, width: int) =
  let printer = StructuralTextWriter(writer)

  let printSeparatorAsync () =
    printer.WriteLineAsync(String.replicate (width - printer.IndentLength) "-")

  let printTestResultAsync i testName (testResult: TestResult) =
    async {
      let mark =
        testResult.Match
          ( fun () -> "."
          , fun _ -> "*"
          , fun _ -> "!"
          )
      do! printer.WriteLineAsync(sprintf "%d. %s %s" (i + 1) mark testName)
      use indenting = printer.AddIndent()
      return!
        testResult.Match
          ( fun () ->
              Async.result ()
          , fun message ->
              printer.WriteLineAsync(message)
          , fun error ->
              printer.WriteLineAsync(string error)
          )
    }

  let rec printTestAsync i (test: Test) =
    test.Match
      ( fun testResult ->
          printTestResultAsync i test.Name testResult
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

  member this.PrintAsync(resultSet: seq<Type * Test[]>) =
    async {
      // Don't print all-green test classes.
      let resultSet =
        resultSet |> Seq.filter
          (fun (typ, tests) -> tests |> Seq.exists (fun test -> not test.IsPassed))
        |> Seq.toArray
      for (typeIndex, (typ, tests)) in resultSet |> Seq.indexed do
        if typeIndex > 0 then
          do! printSeparatorAsync ()
        do! printer.WriteLineAsync(sprintf "type %s" typ.FullName)
        use indenting = printer.AddIndent()
        for (testIndex, test) in tests |> Seq.indexed do
          if not test.IsPassed then
            do! printTestAsync testIndex test
      if resultSet.Length > 0 then
        do! printSeparatorAsync ()
      do! printSummaryAsync resultSet
    }

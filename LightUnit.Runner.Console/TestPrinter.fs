namespace LightUnit.Runner.Console

open System
open System.IO
open LightUnit
open LightUnit.Runner

type TestPrinter(writer: TextWriter) =
  let printer = StructuralTextWriter(writer)

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

  member this.PrintAsync(resultSet: seq<Type * Test[]>) =
    async {
      for (typeIndex, (typ, tests)) in resultSet |> Seq.indexed do
        let isAllPassed = tests |> Seq.forall (fun test -> test.IsPassed)
        if not isAllPassed then
          if typeIndex > 0 then
            do! printer.WriteLineAsync("----- ----- ----- ----- -----")
          do! printer.WriteLineAsync(sprintf "type %s" typ.FullName)
          use indenting = printer.AddIndent()
          for test in tests do
            if not test.IsPassed then
              do! printTestAsync 0 test
    }

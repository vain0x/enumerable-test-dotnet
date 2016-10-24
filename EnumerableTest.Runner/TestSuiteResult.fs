namespace EnumerableTest.Runner

open System
open System.Reflection
open EnumerableTest
open Basis.Core

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestError =
  let methodName (testError: TestError) =
    match testError.Method with
    | TestErrorMethod.Constructor               -> "constructor"
    | TestErrorMethod.Method testCase
    | TestErrorMethod.Dispose testCase          -> testCase.Method.Name

  /// Gets the name of the method where the exception was thrown.
  let errorMethodName (testError: TestError) =
    match testError.Method with
    | TestErrorMethod.Constructor               -> "constructor"
    | TestErrorMethod.Method testCase           -> testCase.Method.Name
    | TestErrorMethod.Dispose _                 -> "Dispose"

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassResult =
  let allAssertionResults (testClassResult: TestClassResult) =
    testClassResult
    |> snd
    |> Seq.collect
      (function
        | Success test -> test.InnerResults |> Seq.map Success
        | Failure error -> seq { yield Failure error }
      )

  let isAllPassed testClassResult =
    testClassResult
    |> allAssertionResults
    |> Seq.forall
      (function
        | Success test when test.IsPassed -> true
        | _ -> false
      )

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSuiteResult =
  let allAssertionResults (testSuiteResult: TestSuiteResult) =
    testSuiteResult |> Seq.collect TestClassResult.allAssertionResults

  let countResults testSuiteResult =
    let results = testSuiteResult |> allAssertionResults
    results |> Seq.fold
      (fun (count, violateCount, errorCount) (result: Result<AssertionResult, TestError>) ->
        let count = count + 1
        match result with
        | Success assertionResult ->
          match assertionResult with
          | Passed              -> (count, violateCount, errorCount)
          | Violated _          -> (count, violateCount + 1, errorCount)
        | Failure _             -> (count, violateCount, errorCount + 1)
      ) (0, 0, 0)

  let isAllPassed testSuiteResult =
    testSuiteResult |> Seq.forall TestClassResult.isAllPassed

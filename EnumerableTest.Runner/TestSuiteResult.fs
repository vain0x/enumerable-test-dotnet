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
  let allAssertions (testClassResult: TestClassResult) =
    testClassResult
    |> snd
    |> Seq.collect
      (function
        | Success test -> test.Assertions |> Seq.map Success
        | Failure error -> seq { yield Failure error }
      )

  let isAllPassed testClassResult =
    testClassResult
    |> allAssertions
    |> Seq.forall
      (function
        | Success test when test.IsPassed -> true
        | _ -> false
      )

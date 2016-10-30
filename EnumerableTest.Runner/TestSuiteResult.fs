namespace EnumerableTest.Runner

open System
open System.Reflection
open EnumerableTest
open Basis.Core

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestError =
  let methodName (testMethod: TestMethod) (testError: TestError) =
    match testError.Method with
    | TestErrorMethod.Constructor ->
      "constructor"
    | TestErrorMethod.Method
    | TestErrorMethod.Dispose ->
      testMethod.MethodName

  /// Gets the name of the method where the exception was thrown.
  let errorMethodName (testMethod: TestMethod) (testError: TestError) =
    match testError.Method with
    | TestErrorMethod.Constructor ->
      "constructor"
    | TestErrorMethod.Method ->
      testMethod.MethodName
    | TestErrorMethod.Dispose ->
      "Dispose"

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassResult =
  let allAssertions (testClassResult: TestClassResult) =
    testClassResult
    |> snd
    |> Seq.collect
      (function
        | (_, Success test) -> test.Assertions |> Seq.map Success
        | (_, Failure error) -> seq { yield Failure error }
      )

  let isAllPassed testClassResult =
    testClassResult
    |> allAssertions
    |> Seq.forall
      (function
        | Success test when test.IsPassed -> true
        | _ -> false
      )

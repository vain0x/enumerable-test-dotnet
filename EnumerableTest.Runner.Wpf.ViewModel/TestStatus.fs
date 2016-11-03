namespace EnumerableTest.Runner.Wpf

open Basis.Core
open EnumerableTest.Sdk
open EnumerableTest.Runner

type TestStatus =
  | NotCompleted
  | Passed
  | Violated
  | Error

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestStatus =
  let ofGroupTest (groupTest: GroupTest) =
    if groupTest.ExceptionOrNull |> isNull then
      if groupTest.IsPassed
        then Passed
        else Violated
    else
      Error

  let ofAssertion (assertion: Assertion) =
    if assertion.IsPassed
      then Passed
      else Violated

  let ofTestMethod (testMethod: TestMethod) =
    match testMethod.DisposingError with
    | Some _ ->
      Error
    | None ->
      testMethod.Result |> ofGroupTest

type NotExecutedResult() =
  static member val Instance =
    new NotExecutedResult()

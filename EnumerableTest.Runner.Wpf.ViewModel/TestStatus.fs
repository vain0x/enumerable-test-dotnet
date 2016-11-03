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
    if groupTest.IsPassed
      then Passed
      else Violated

  let ofAssertion (assertion: Assertion) =
    if assertion.IsPassed
      then Passed
      else Violated

  let ofTestResult =
    function
    | Success (AssertionTest test) ->
      test.Assertion |> ofAssertion
    | Success (GroupTest test) ->
      test |> ofGroupTest
    | Failure _ ->
      Error

  let ofTestMethodResult =
    function
    | Success groupTest ->
      groupTest |> ofGroupTest
    | Failure _ ->
      Error

type NotExecutedResult() =
  static member val Instance =
    new NotExecutedResult()

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

  let ofArray (statuses: array<TestStatus>) =
    let rec loop i current =
      if i = statuses.Length then
        current
      else
        let loop = loop (i + 1)
        match (current, statuses.[i]) with
        | (Error, _) | (_, Error) ->
          Error
        | (Violated, _) | (_, Violated) ->
          Violated |> loop
        | (_, NotCompleted) | (NotCompleted, _) ->
          NotCompleted |> loop
        | _ ->
          Passed |> loop
    loop 0 Passed

type NotExecutedResult() =
  static member val Instance =
    new NotExecutedResult()

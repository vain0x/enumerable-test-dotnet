namespace EnumerableTest.Runner

open EnumerableTest

[<AutoOpen>]
module AssertionResultExtension =
  let (|True|False|) (assertionResult: AssertionResult) =
    match assertionResult with
    | :? AssertionResult.PassedAssertionResult as a ->
      True a
    | :? AssertionResult.ViolatedAssertionResult as a ->
      False a
    | a -> failwithf "Unknown assertion: %A" a

  let (|Passed|Violated|) =
    function
    | True a -> Passed
    | False a ->Violated a.Message

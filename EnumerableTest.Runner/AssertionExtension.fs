namespace EnumerableTest.Runner

open EnumerableTest

[<AutoOpen>]
module AssertionExtension =
  let (|True|False|) (assertion: Assertion) =
    match assertion with
    | :? TrueAssertion as a ->
      True a
    | :? FalseAssertion as a ->
      False a
    | a -> failwithf "Unknown assertion: %A" a

  let (|Passed|Violated|) =
    function
    | True a -> Passed
    | False a ->Violated a.Message

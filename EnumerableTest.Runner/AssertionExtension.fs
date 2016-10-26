namespace EnumerableTest.Runner

open EnumerableTest

[<AutoOpen>]
module AssertionExtension =
  let (|True|False|) (assertion: Assertion) =
    match assertion with
    | :? PassedAssertion as a ->
      True a
    | :? ViolatedAssertion as a ->
      False a
    | a -> failwithf "Unknown assertion: %A" a

  let (|Passed|Violated|) =
    function
    | True a -> Passed
    | False a ->Violated a.Message

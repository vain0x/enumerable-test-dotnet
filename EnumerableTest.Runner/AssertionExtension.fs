namespace EnumerableTest.Runner

open EnumerableTest

[<AutoOpen>]
module AssertionExtension =
  let (|True|False|Equal|Catch|) (assertion: Assertion) =
    match assertion with
    | :? TrueAssertion as a ->
      True a
    | :? FalseAssertion as a ->
      False a
    | :? EqualAssertion as a ->
      Equal a
    | :? CatchAssertion as a ->
      Catch a
    | a -> failwithf "Unknown assertion: %A" a

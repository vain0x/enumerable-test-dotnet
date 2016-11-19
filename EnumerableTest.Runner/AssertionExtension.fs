namespace EnumerableTest.Runner

open EnumerableTest.Sdk

[<AutoOpen>]
module AssertionExtension =
  let (|True|False|Equal|Satisfy|Catch|) (assertion: Assertion) =
    match assertion with
    | :? TrueAssertion as a ->
      True a
    | :? FalseAssertion as a ->
      False a
    | :? EqualAssertion as a ->
      Equal a
    | :? SatisfyAssertion as a ->
      Satisfy a
    | :? CatchAssertion as a ->
      Catch a
    | a -> failwithf "Unknown assertion: %A" a

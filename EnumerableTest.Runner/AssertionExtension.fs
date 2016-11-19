namespace EnumerableTest.Runner

open EnumerableTest.Sdk

[<AutoOpen>]
module AssertionExtension =
  let (|True|Custom|Equal|Satisfy|Catch|) (assertion: Assertion) =
    match assertion with
    | :? TrueAssertion as a ->
      True a
    | :? CustomAssertion as a ->
      Custom a
    | :? EqualAssertion as a ->
      Equal a
    | :? SatisfyAssertion as a ->
      Satisfy a
    | :? CatchAssertion as a ->
      Catch a
    | a -> failwithf "Unknown assertion: %A" a

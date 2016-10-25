namespace EnumerableTest.Runner

open EnumerableTest

[<AutoOpen>]
module AssertionResultExtension =
  let (|Passed|Violated|) (assertionResult: AssertionResult) =
    match assertionResult.Match(Choice1Of2, Choice2Of2) with
    | Choice1Of2 () -> Passed
    | Choice2Of2 message -> Violated message

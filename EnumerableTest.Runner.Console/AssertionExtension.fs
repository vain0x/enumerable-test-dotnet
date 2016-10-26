namespace EnumerableTest.Runner.Console

open EnumerableTest.Runner

[<AutoOpen>]
module AssertionExtension =
  let (|Passed|Violated|) =
    function
    | True a -> Passed
    | False a -> Violated a.Message

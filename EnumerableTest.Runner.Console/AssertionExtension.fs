namespace EnumerableTest.Runner.Console

open EnumerableTest
open EnumerableTest.Runner

[<AutoOpen>]
module AssertionExtension =
  let (|Passed|Violated|) (a: Assertion) =
    if a.IsPassed then
      Passed
    else
      let message =
        match a with
        | True _ -> failwith "never"
        | False a -> a.Message
        | Equal a ->
          sprintf "Expected: %A\r\nActual: %A" a.Target a.Actual
        | Catch a ->
          sprintf
            "An exception of a type should be thrown but didn't.\r\nExpected: typeof(%A)"
            a.Type
      Violated message

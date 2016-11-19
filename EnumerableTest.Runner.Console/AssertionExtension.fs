namespace EnumerableTest.Runner.Console

open EnumerableTest.Runner
open EnumerableTest.Sdk

[<AutoOpen>]
module AssertionExtension =
  let (|Passed|Violated|) (a: Assertion) =
    if a.IsPassed then
      Passed
    else
      let message =
        match a with
        | True _ -> failwith "never"
        | False a ->
          seq {
            yield a.Message
            for KeyValue (key, value) in a.Data do
              yield sprintf "%s: %A" key value
          }
          |> String.concat "\r\n"
        | Equal a ->
          sprintf "Expected: %A\r\nActual: %A" a.Expected a.Actual
        | Satisfy a ->
          sprintf
            "A value should satisfy a predicate but didn't.\r\nValue: %A\r\nPredicate: %A"
            a.Value a.Predicate
        | Catch a ->
          sprintf
            "An exception of a type should be thrown but didn't.\r\nExpected: typeof(%A)"
            a.Type
      Violated message

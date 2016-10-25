namespace EnumerableTest.Runner

open System
open EnumerableTest

[<AutoOpen>]
module TestExtension =
  let (|AssertionTest|GroupTest|) (test: Test) =
    match test.Match(Func<_, _>(Choice1Of2), Func<_, _>(Choice2Of2)) with
    | Choice1Of2 ar -> AssertionTest ar
    | Choice2Of2 tests -> GroupTest tests

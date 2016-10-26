namespace EnumerableTest.Runner

open System
open EnumerableTest

[<AutoOpen>]
module TestExtension =
  let (|AssertionTest|GroupTest|) (test: Test) =
    match test with
    | :? Test.AssertionTest as test ->
      AssertionTest test
    | :? Test.GroupTest as test ->
      GroupTest test
    | test ->
      failwithf "Unknown test: %A" test

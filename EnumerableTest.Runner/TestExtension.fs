namespace EnumerableTest.Runner

open System
open EnumerableTest
open EnumerableTest.Sdk

[<AutoOpen>]
module TestExtension =
  let (|AssertionTest|GroupTest|) (test: Test) =
    match test with
    | :? AssertionTest as test ->
      AssertionTest test
    | :? GroupTest as test ->
      GroupTest test
    | test ->
      failwithf "Unknown test: %A" test

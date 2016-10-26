namespace EnumerableTest.UnitTest

open Persimmon

[<AutoOpen>]
module Assertions =
  let inline assertSatisfies predicate x =
    if x |> predicate then
      pass ()
    else
      sprintf "A value should satisfy a predicate but didn't.\r\nThe value: %s" (string x)
      |> fail

  let assertSeqEquals expected actual =
    actual |> Seq.toList |> assertEquals (expected |> Seq.toList)

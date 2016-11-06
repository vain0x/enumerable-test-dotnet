namespace EnumerableTest.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner.Wpf

module ReadOnlyListTest =
  open System.Collections.Generic

  let ``test symmetricDifferenceBy`` =
    let body (left, right, expected) =
      test {
        let difference =
          ReadOnlyList.symmetricDifferenceBy fst fst
            (left :> IReadOnlyList<_>)
            (right :> IReadOnlyList<_>)
        do!
          ( difference.Left |> Seq.toArray
          , difference.Intersect |> Seq.toArray
          , difference.Right |> Seq.toArray
          ) |> assertEquals expected
      }
    parameterize {
      case
        ( [| (0, "a"); (1, "b") |]
        , [||]
        , ( [| (0, "a"); (1, "b") |]
          , [||]
          , [||]
          ))
      case
        ( [||]
        , [| (0, "A"); (1, "B") |]
        , ( [||]
          , [||]
          , [| (0, "A"); (1, "B") |]
          ))
      case
        ( [| (1, "b"); (2, "c") |]
        , [| (0, "A"); (3, "D") |]
        , ( [| (1, "b"); (2, "c") |]
          , [||]
          , [| (0, "A"); (3, "D") |]
          ))
      case
        ( [| (0, "a"); (1, "b"); (2, "c") |]
        , [| (0, "A"); (2, "C") |]
        , ( [| (1, "b") |]
          , [| (0, (0, "a"), (0, "A")); (2, (2, "c"), (2, "C")) |]
          , [||]
          ))
      case
        ( [| (0, "a"); (2, "c") |]
        , [| (2, "C"); (1, "B") |]
        , ( [| (0, "a") |]
          , [| (2, (2, "c"), (2, "C")) |]
          , [| (1, "B") |]
          ))
      run body
    }

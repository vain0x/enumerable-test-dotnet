﻿namespace EnumerableTest.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest

module TestExtensionTest =
  module ToTestGroupTest =
    let body (name, isPassed, testsCondition, exceptionCondition) tests =
      test {
        let group = (tests :> seq<Test>).ToTestGroup(name)
        do! group.Name |> assertEquals name
        do! group.IsPassed |> assertEquals isPassed
        do! group.Tests |> assertSatisfies testsCondition
        do! group.ExceptionOrNull |> assertSatisfies exceptionCondition
      }

    let emptyCase =
      Seq.empty |> body ("empty", true, Array.isEmpty, isNull)

    let passingCase =
      seq {
        yield (0).Is(0)
        yield "abc".TestSatisfy(fun s -> s.StartsWith("a"))
        yield Test.Catch(fun () -> exn() |> raise)
      }
      |> body
        ( "passing"
        , true
        , fun a -> a.Length = 3
        , isNull
        )

    let violatedCase =
      seq {
        yield (0).Is(0)
        yield (0).Is(1)
        yield (1).Is(1)
      }
      |> body
        ( "violated"
        , false
        , fun a -> a.Length = 3
        , isNull
        )

    let passingExceptionCase =
      let e = exn()
      seq {
        yield (0).Is(0)
        yield (1).Is(1)
        e |> raise
      }
    |> body
      ( "passing-exception"
      , false
      , fun a -> a.Length = 2
      , (=) e
      )

    let violatedExceptionCase =
      let e = exn()
      seq {
        yield (0).Is(0)
        yield (0).Is(1)
        e |> raise
      }
    |> body
      ( "violated-exception"
      , false
      , fun a -> a.Length = 2
      , (=) e
      )

  let ``test TestSequence`` =
    test {
      do!
        TestExtension.TestSequence([0; 1; 2], 0, 1, 2)
        |> assertSatisfies (fun a -> a.IsPassed)
      do! 
        TestExtension.TestSequence([], 0)
        |> assertSatisfies (fun a -> a.IsPassed |> not)
    }
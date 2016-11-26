namespace EnumerableTest.UnitTest

open System
open System.Threading
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Runner

module TestClassTest =
  type Passing() =
    member this.PassingTest() =
      seq {
        yield (0).Is(0)
      }

  type Never() =
    member this.PassingTest() =
      seq {
        yield (0).Is(0)
      }

    member this.ViolatedTest() =
      seq {
        yield (0).Is(1)
      }

    member this.NeverTest(): seq<Test> =
      seq {
        while true do
          ()
      }

  type Uninstantiatable() =
    do exn() |> raise

    member this.PassingTest() =
      seq {
        yield (0).Is(0)
      }

  module test_create =
    let body (isPassed, instantiationErrorCondition, resultCondition, notCompletedCondition) typ =
      test {
        let timeout = TimeSpan.FromMilliseconds 50.0
        let testClass = TestClass.create timeout typ
        do! testClass.InstantiationError
            |> assertSatisfies instantiationErrorCondition
        do! testClass.Result
            |> assertSatisfies resultCondition
        do! testClass.NotCompletedMethods
            |> assertSatisfies notCompletedCondition
        do! testClass |> TestClass.isPassed
            |> assertEquals isPassed
      }

    let passingCase =
      typeof<Passing>
      |> body
        ( true
        , Option.isNone
        , Array.length >> (=) 1
        , Array.isEmpty
        )

    let neverCase =
      typeof<Never>
      |> body
        ( false
        , Option.isNone
        , Array.length >> (=) 2
        , Array.length >> (=) 1
        )

    let uninstantiatableCase =
      typeof<Uninstantiatable>
      |> body
        ( false
        , Option.isSome
        , Array.isEmpty
        , Array.isEmpty
        )

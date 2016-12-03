namespace EnumerableTest.UnitTest

open System
open System.Threading
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Runner
open EnumerableTest.Runner.Console

module TestClassTest =
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
      typeof<TestClass.Passing>
      |> body
        ( true
        , Option.isNone
        , Array.length >> (=) 1
        , Array.isEmpty
        )

    let neverCase =
      typeof<TestClass.Never>
      |> body
        ( false
        , Option.isNone
        , Array.length >> (=) 2
        , Array.length >> (=) 1
        )

    let uninstantiatableCase =
      typeof<TestClass.Uninstantiatable>
      |> body
        ( false
        , Option.isSome
        , Array.isEmpty
        , Array.isEmpty
        )

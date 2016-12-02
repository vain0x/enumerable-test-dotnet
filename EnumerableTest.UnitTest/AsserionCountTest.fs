namespace EnumerableTest.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Sdk
open EnumerableTest.Runner

module AsserionCountTest =
  module test_ofAssertion =
    let passedCase =
      test {
        do!
          Assertion.Pass
          |> SerializableAssertion.ofAssertion
          |> AssertionCount.ofAssertion
          |> assertEquals AssertionCount.onePassed
      }

    let violatedCase =
      test {
        do!
          Assertion(false, "violated", Seq.empty)
          |> SerializableAssertion.ofAssertion
          |> AssertionCount.ofAssertion
          |> assertEquals AssertionCount.oneViolated
      }

  module test_ofGroupTest =
    let normalCase =
      test {
        do!
          [|
            (0).Is(0)
            (0).Is(1)
          |].ToTestGroup("normal-group")
          |> SerializableTest.ofGroupTest
          |> AssertionCount.ofGroupTest
          |> assertEquals
            {
              TotalCount =
                2
              ViolatedCount =
                1
              ErrorCount =
                0
              NotCompletedCount =
                0
            }
      }

    let exceptionCase =
      test {
        do!
          (seq {
            yield (0).Is(1)
            exn() |> raise
          }).ToTestGroup("exception-group")
          |> SerializableTest.ofGroupTest
          |> AssertionCount.ofGroupTest
          |> assertEquals
            {
              TotalCount =
                2
              ViolatedCount =
                1
              ErrorCount =
                1
              NotCompletedCount =
                0
            }
      }

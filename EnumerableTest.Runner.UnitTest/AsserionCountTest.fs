namespace EnumerableTest.Runner.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Sdk
open EnumerableTest.Runner

module AsserionCountTest =
  module test_ofAssertionTest =
    let passedCase =
      test {
        do!
          (Test.FromResult("passed-test", true) :?> AssertionTest)
          |> SerializableTest.ofAssertionTest
          |> AssertionCount.ofAssertionTest
          |> assertEquals AssertionCount.onePassed
      }

    let violatedCase =
      test {
        do!
          (Test.FromResult("violated-test", false) :?> AssertionTest)
          |> SerializableTest.ofAssertionTest
          |> AssertionCount.ofAssertionTest
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

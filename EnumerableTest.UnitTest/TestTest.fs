namespace EnumerableTest.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Sdk

module TestTest =
  let passedTest = Test.Pass("pass")
  let violatedTest = Test.FromResult("violate", false, "it violated")

  let assertion1 = Assertion.Pass
  let assertion2 = Assertion(false, "assertion2 violated", [])
  let assertionTest1 = Test.OfAssertion("assertion1", assertion1)
  let assertionTest2 = Test.OfAssertion("assertion2", assertion2)
  let groupTest =
    (seq [assertionTest1; assertionTest2]).ToTestGroup("group1")

  let emptyGroupTest =
    Seq.empty.ToTestGroup("empty group")

  let nestedGroupTest =
    let innerTest1 = (seq [assertionTest1]).ToTestGroup("inner group1") :> Test
    let innerTest2 = (seq [assertionTest2]).ToTestGroup("inner group2") :> Test
    (seq [ innerTest1; innerTest2; Test.Pass("pass") ]).ToTestGroup("outer group")

  let ``test IsPassed`` =
    test {
      do! passedTest |> assertSatisfies (fun t -> t.IsPassed)
      do! violatedTest |> assertSatisfies (fun t -> t.IsPassed |> not)
      do! emptyGroupTest |> assertSatisfies (fun t -> t.IsPassed)
      do! groupTest |> assertSatisfies (fun t -> t.IsPassed |> not)
    }

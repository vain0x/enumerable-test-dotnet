namespace EnumerableTest.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest

module TestTest =
  let passedTest = Test.Pass("pass")
  let violatedTest = Test.Violate("violate", "it violated")

  let assertionResult1 = AssertionResult.OfPassed
  let assertionResult2 = AssertionResult.OfViolated "assertion2 violated"
  let assertionTest1 = Test.OfAssertion("assertion1", assertionResult1)
  let assertionTest2 = Test.OfAssertion("assertion2", assertionResult2)
  let groupTest =
    Test.OfTestGroup("group1", seq [assertionTest1; assertionTest2])

  let emptyGroupTest =
    Test.OfTestGroup("empty group", Seq.empty)

  let nestedGroupTest =
    let innerTest1 = Test.OfTestGroup("inner group1", seq [assertionTest1])
    let innerTest2 = Test.OfTestGroup("inner group2", seq [assertionTest2])
    Test.OfTestGroup("outer group", seq [ innerTest1; innerTest2; Test.Pass("pass") ])

  let ``test IsPassed`` =
    test {
      do! passedTest |> assertSatisfies (fun t -> t.IsPassed)
      do! violatedTest |> assertSatisfies (fun t -> t.IsPassed |> not)
      do! emptyGroupTest |> assertSatisfies (fun t -> t.IsPassed)
      do! groupTest |> assertSatisfies (fun t -> t.IsPassed |> not)
    }

  let ``test InnerResults`` =
    test {
      do! passedTest.InnerResults |> assertSatisfies (Seq.length >> (=) 1)
      do! violatedTest.InnerResults |> assertSatisfies (Seq.length >> (=) 1)
      do! emptyGroupTest.InnerResults |> assertSeqEquals []
      do! groupTest.InnerResults |> assertSeqEquals [assertionResult1; assertionResult2]
      do! nestedGroupTest.InnerResults |> assertSatisfies (Seq.length >> (=) 3)
    }

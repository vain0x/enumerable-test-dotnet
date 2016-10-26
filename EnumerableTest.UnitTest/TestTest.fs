namespace EnumerableTest.UnitTest

open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest

module TestTest =
  let passedTest = Test.Pass("pass")
  let violatedTest = Test.Violate("violate", "it violated")

  let assertion1 = TrueAssertion.Instance
  let assertion2 = FalseAssertion("assertion2 violated")
  let assertionTest1 = Test.OfAssertion("assertion1", assertion1)
  let assertionTest2 = Test.OfAssertion("assertion2", assertion2)
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

  let ``test Assertions`` =
    test {
      do! passedTest.Assertions |> assertSatisfies (Seq.length >> (=) 1)
      do! violatedTest.Assertions |> assertSatisfies (Seq.length >> (=) 1)
      do! emptyGroupTest.Assertions |> assertSeqEquals []
      do! groupTest.Assertions |> assertSeqEquals [assertion1; assertion2]
      do! nestedGroupTest.Assertions |> assertSatisfies (Seq.length >> (=) 3)
    }

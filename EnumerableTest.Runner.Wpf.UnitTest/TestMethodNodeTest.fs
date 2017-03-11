namespace EnumerableTest.Runner.Wpf.UnitTest

open System
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Runner
open EnumerableTest.Runner.Wpf
open EnumerableTest.Runner.UnitTest

module TestMethodNodeTest =
  let testMethodSchema: TestMethodSchema =
    {
      MethodName =
        "method"
    }

  let ``test initial state`` =
    test {
      let node = TestMethodNode(testMethodSchema, Command.never)
      do! node.Name |> assertEquals "method"
      do! node.LastResult.Value |> assertEquals (NotExecutedResult.Instance :> obj)
      do! node.Children |> assertSatisfies Seq.isEmpty
      do! node.TestStatistic.Value |> assertEquals TestStatistic.notCompleted
    }

  let ``test UpdateResult`` =
    test {
      let node = TestMethodNode(testMethodSchema, Command.never)
      let duration = TimeSpan.FromMilliseconds(1.2)
      let testMethodResult =
        TestMethodResult.ofResult
          testMethodSchema.MethodName
          ([| (0).Is(1) |].ToTestGroup("group") |> SerializableTest.ofGroupTest)
          None
          duration
      node.UpdateResult(testMethodResult)
      do! node.LastResult.Value |> assertEquals (testMethodResult :> obj)
      do! node.TestStatistic.Value
          |> assertEquals
            {
              AssertionCount =
                AssertionCount.oneViolated
              Duration =
                duration
            }
    }

namespace EnumerableTest.Runner.Wpf.UnitTest

open System
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest
open EnumerableTest.Runner
open EnumerableTest.Runner.Wpf
open EnumerableTest.Sdk
open EnumerableTest.Runner.UnitTest

module FolderNodeTest =
  module test_FindOrAddFolderNode =
    let empty () =
      FolderNode.CreateRoot()

    let ``add a chlid`` =
      test {
        let root = empty ()
        do! root.Children.Count |> assertEquals 0
        let node = root.FindOrAddFolderNode(["a"])
        do! node.Name |> assertEquals "a"
        do! root.Children |> assertSeqEquals [node]
      }

    let ``add many descendants`` =
      test {
        let root = empty ()
        let ax1 = root.FindOrAddFolderNode(["a"; "ax"; "ax1"])
        let a = root.Children.[0]
        let ax = a.Children.[0]
        do! [a; ax; ax1] |> List.map (fun n -> n.Name) |> assertEquals ["a"; "ax"; "ax1"]
      }

    let ``add a child under a internal node`` =
      test {
        let root = empty ()
        let ax1 = root.FindOrAddFolderNode(["a"; "ax"; "ax1"])
        let a = root.Children.[0] :?> FolderNode
        let ay = a.FindOrAddFolderNode(["ay"])
        do! a.Children |> assertSatisfies (Seq.contains ay)
      }

  module test_TestStatistic =
    let ``test updating`` =
      test {
        let node = FolderNode("folderNode")
        let a = TestMethodNode({ MethodName = "a" }, Command.never)
        let b = TestMethodNode({ MethodName = "b" }, Command.never)
        node.AddChild(a)
        node.AddChild(b)
        do! node.TestStatistic.Value |> assertEquals
              {
                AssertionCount =
                  AssertionCount.ofNotCompleted 2
                Duration =
                  TimeSpan.Zero
              }
        a.UpdateResult(
          let groupTest =
            [| (0).Is(0) |].ToTestGroup("a")
            |> SerializableTest.ofGroupTest
          in TestMethod.ofResult "a" groupTest None TimeSpan.Zero
          )
        b.UpdateResult(
          let groupTest =
            (seq { do exn() |> raise }).ToTestGroup("b")
            |> SerializableTest.ofGroupTest
          in TestMethod.ofResult "b" groupTest None TimeSpan.Zero
          )
        do! node.TestStatistic.Value |> assertEquals
              {
                AssertionCount =
                  {
                    TotalCount =
                      2
                    ViolatedCount =
                      0
                    ErrorCount =
                      1
                    NotCompletedCount =
                      0
                  }
                Duration =
                  TimeSpan.Zero
              }
      }

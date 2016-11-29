namespace EnumerableTest.UnitTest

open System
open System.Collections.Generic
open System.Reactive.Subjects
open System.Threading
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open Basis.Core
open Reactive.Bindings
open EnumerableTest
open EnumerableTest.Runner
open EnumerableTest.Runner.Wpf
open EnumerableTest.Sdk

module TestTreeTest =
  let cancelCommand =
    ReactiveProperty.create false
    |> ReactiveCommand.ofFunc (fun () -> ())

  module TestTreeNodeTest =
    let empty () =
      FolderNode.CreateRoot()

    let seed () =
      test {
        let root = empty ()
        do root.FindOrAddFolderNode(["a"; "ax"; "ax1"]) |> ignore
        do root.FindOrAddFolderNode(["a"; "ax"; "ax2"]) |> ignore
        do root.FindOrAddFolderNode(["a"; "ay"]) |> ignore
        do root.FindOrAddFolderNode(["b"; "bx"]) |> ignore
        return root
      }

    module test_RouteOrFailure =
      let ``find self`` =
        test {
          let root = empty ()
          let node = root.RouteOrFailure([])
          do! node |> assertEquals (root :> TestTreeNode |> Success)
        }

      let ``find a child`` =
        test {
          let! root = seed ()
          let node = root.RouteOrFailure(["a"]) |> Result.get
          do! node.Name |> assertEquals "a"
          do! root.Children |> assertSatisfies (Seq.contains node)
        }

      let ``find a descendant`` =
        test {
          let! root = seed ()
          let node = root.RouteOrFailure(["a"; "ax"; "ax2"]) |> Result.get
          do! node.Name |> assertEquals "ax2"
          do! root.Children
              |> assertSatisfies
                (Seq.exists
                  (fun n ->
                    n.Name = "a" && n.Children |> Seq.exists
                      (fun n ->
                        n.Name = "ax" && n.Children |> Seq.contains node
                      )))
        }

      let ``find a descendant under an internal node`` =
        test {
          let! root = seed ()
          let a = root.Children.[0] :?> FolderNode
          let ax = a.RouteOrFailure(["ax"]) |> Result.get
          let ax1 = a.RouteOrFailure(["ax"; "ax1"]) |> Result.get
          do! ax.Name |> assertEquals "ax"
          do! ax1.Name |> assertEquals "ax1"
          do! ax.Children |> assertSatisfies (Seq.contains ax1)
        }

      let ``resolve no part of path`` =
        test {
          let! root = seed ()
          do! root.RouteOrFailure(["c"]) |> assertEquals (Failure (root :> TestTreeNode, ["c"]))
        }

      let ``resolve a part of path`` =
        test {
          let! root = seed ()
          let a = root.TryRoute(["a"]) |> Option.get
          do! root.RouteOrFailure(["a"; "z"]) |> assertEquals (Failure (a, ["z"]))
        }

  module TestMethodNodeTest =
    let testMethodSchema: TestMethodSchema =
      {
        MethodName =
          "method"
      }

    let ``test initial state`` =
      test {
        let node = TestMethodNode(testMethodSchema, cancelCommand)
        do! node.Name |> assertEquals "method"
        do! node.LastResult.Value |> assertEquals (NotExecutedResult.Instance :> obj)
        do! node.Children |> assertSatisfies Seq.isEmpty
        do! node.TestStatistic.Value |> assertEquals TestStatistic.notCompleted
      }

    let ``test UpdateResult`` =
      test {
        let node = TestMethodNode(testMethodSchema, cancelCommand)
        let duration = TimeSpan.FromMilliseconds(1.2)
        let testMethod =
          TestMethod.ofResult
            testMethodSchema.MethodName
            ([| (0).Is(1) |].ToTestGroup("group"))
            None
            duration
        node.UpdateResult(testMethod)
        do! node.LastResult.Value |> assertEquals (testMethod :> obj)
        do! node.TestStatistic.Value
            |> assertEquals
              {
                AssertionCount =
                  AssertionCount.oneViolated
                Duration =
                  duration
              }
      }

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
          let a = TestMethodNode({ MethodName = "a" }, cancelCommand)
          let b = TestMethodNode({ MethodName = "b" }, cancelCommand)
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
            TestMethod.ofResult "a" (GroupTest("a", [| (0).Is(0) |], null)) None TimeSpan.Zero
            )
          b.UpdateResult(
            TestMethod.ofResult "b" (GroupTest("b", [||], exn())) None TimeSpan.Zero
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

  module TestTreeTest =
    type TestClass1() =
      member this.PassingTest() =
        TestClass.passingTest

      member this.ViolatingTest() =
        TestClass.violatingTest

      member this.ThrowingTest() =
        TestClass.throwingTest

    type TestClass1Updated() =
      // Unchanged.
      member this.ViolatingTest() =
        TestClass.violatingTest

      // Fixed.
      member this.ThrowingTest() =
        TestClass.passingTest

      // Added.
      member this.NewPassingTest() =
        TestClass.passingTest

    type ControlPanel =
      {
        SchemaUpdatedObserver:
          IObserver<TestSuiteSchemaDifference>
        TestResultObserver:
          IObserver<TestResult>
        TestAssembly:
          TestAssembly
        Runner:
          PermanentTestRunner
        Tree:
          TestTree
      }

    let seed () =
      let schemaUpdated =
        new Subject<_>()
      let testResults =
        new Subject<_>()
      let testAssembly =
        { new TestAssembly() with
            override this.SchemaUpdated =
              schemaUpdated :> _
            override this.TestResults =
              testResults :> _
            override this.CancelCommand =
              cancelCommand :> _
            override this.Start() = ()
            override this.Dispose() = ()
        }
      let testAssemblyObservable =
        ReactiveProperty.create testAssembly
      let runner =
        { new PermanentTestRunner() with
            override this.AssemblyAdded =
              testAssemblyObservable :> _
            override this.Dispose() = ()
        }
      let testTree =
        new TestTree(runner)
      {
        SchemaUpdatedObserver =
          schemaUpdated :> IObserver<_>
        TestResultObserver =
          testResults :> IObserver<_>
        TestAssembly =
          testAssembly
        Runner =
          runner
        Tree =
          testTree
      }

    let afterFirstSchemaUpdated () =
      let controlPanel = seed ()
      let (schema, connectable) =
        TestSuite.ofTypesAsObservable [typeof<TestClass1>]
      controlPanel.SchemaUpdatedObserver.OnNext(TestSuiteSchema.difference [||] schema)
      let classNode =
        let path =
          [
            "EnumerableTest"
            "UnitTest"
            "TestTreeTest"
            "TestTreeTest"
            "TestClass1"
          ]
        controlPanel.Tree.Root.TryRoute(path)
        |> Option.get
      (controlPanel, classNode, connectable)

    let ``test reaction to first SchemaUpdated`` =
      test {
        let (controlPanel, classNode, _) = afterFirstSchemaUpdated ()
        do! classNode.Children |> assertSatisfies (Seq.length >> (=) 3)
        do!
          classNode.TestStatistic.Value
          |> TestStatus.ofTestStatistic
          |> assertEquals TestStatus.NotCompleted
      }

    let afterFirstExecution () =
      let (controlPanel, classNode, connectable) = afterFirstSchemaUpdated ()
      connectable.Subscribe(controlPanel.TestResultObserver) |> ignore
      connectable.Connect()
      connectable |> Observable.wait
      (controlPanel, classNode)

    let ``test reaction to TestResults`` =
      test {
        let (controlPanel, classNode) = afterFirstExecution ()
        do! classNode.Children |> assertSatisfies (Seq.length >> (=) 3)
        do!
          classNode.TestStatistic.Value
          |> TestStatus.ofTestStatistic
          |> assertEquals TestStatus.Error
      }

    let afterSecondSchemaUpdated () =
      let (controlPanel, classNode) = afterFirstExecution ()
      let fullPath =
        typeof<TestClass1>.FullName |> TestClassPath.ofFullName |> TestClassPath.fullPath
      let difference: TestSuiteSchemaDifference =
        let classDifference =
          TestClassSchema.difference
            (TestClassSchema.ofType typeof<TestClass1>)
            (TestClassSchema.ofType typeof<TestClass1Updated>)
        {
          Added =
            [||] :> IReadOnlyList<_>
          Removed =
            [||] :> IReadOnlyList<_>
          Modified =
            [(fullPath, classDifference)] |> Map.ofSeq
        }
      controlPanel.SchemaUpdatedObserver.OnNext(difference)
      (controlPanel, classNode)

    let ``test reaction to second SchemaUpdated`` =
      test {
        let (controlPanel, classNode) = afterSecondSchemaUpdated ()
        do!
          classNode.Children |> Seq.map (fun ch -> ch.Name)
          |> Seq.sort
          |> Seq.toList
          |> assertEquals
            ([
              "ViolatingTest"
              "ThrowingTest"
              "NewPassingTest"
            ] |> List.sort)
        do!
          classNode.Children
          |> Seq.map (fun ch -> ch.TestStatistic.Value |> TestStatus.ofTestStatistic)
          |> assertSatisfies (Seq.forall ((=) TestStatus.NotCompleted))
      }

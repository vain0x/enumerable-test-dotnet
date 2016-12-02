namespace EnumerableTest.UnitTest

open System
open System.Collections.Generic
open System.Reactive.Subjects
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner
open EnumerableTest.Runner.Wpf

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
        PermanentTestAssembly
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
      { new PermanentTestAssembly() with
          override this.SchemaUpdated =
            schemaUpdated :> _
          override this.TestResults =
            testResults :> _
          override this.CancelCommand =
            ObservableCommand.never
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

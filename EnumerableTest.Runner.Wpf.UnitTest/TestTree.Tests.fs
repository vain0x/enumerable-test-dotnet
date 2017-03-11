namespace EnumerableTest.Runner.Wpf.UnitTest

open System
open System.Collections.Generic
open System.Reactive.Subjects
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner
open EnumerableTest.Runner.Wpf
open EnumerableTest.Runner.UnitTest

module ``test TestTree`` =
  type TestClass1() =
    member this.PassingTest() =
      TestClasses.passingTest

    member this.ViolatingTest() =
      TestClasses.violatingTest

    member this.ThrowingTest() =
      TestClasses.throwingTest

  type TestClass1Updated() =
    // Unchanged.
    member this.ViolatingTest() =
      TestClasses.violatingTest

    // Fixed.
    member this.ThrowingTest() =
      TestClasses.passingTest

    // Added.
    member this.NewPassingTest() =
      TestClasses.passingTest

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
          override this.TestCompleted =
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
      new TestTree(runner, new NullNotifier())
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
    let types = [typeof<TestClass1>]
    let schema =
      TestSuiteSchema.ofTypes types
    let connectable =
      TestSuite.ofTypes types
    controlPanel.SchemaUpdatedObserver.OnNext(TestSuiteSchema.difference [||] schema)
    let classNode =
      let path =
        [
          "EnumerableTest"
          "Runner"
          "Wpf"
          "UnitTest"
          "test TestTree"
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
    connectable.Connect() |> ignore
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
      typeof<TestClass1> |> Type.fullName |> Type.FullName.fullPath
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

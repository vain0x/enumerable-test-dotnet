namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel

type TestTree(runner: PermanentTestRunner) =
  let children = ObservableCollection<TestAssemblyNode>()

  let subscription =
    runner.AssemblyAdded |> Observable.subscribe
      (fun testAssembly ->
        children.Add(new TestAssemblyNode(testAssembly))
      )

  member this.Children =
    children

  member this.Dispose() =
    subscription.Dispose()
    for node in children do
      node.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

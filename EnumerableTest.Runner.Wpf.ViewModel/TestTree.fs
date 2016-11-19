namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.IO
open System.Reflection
open System.Threading
open EnumerableTest.Runner
open EnumerableTest.Sdk

type TestTree() =
  let children = ObservableCollection<TestAssemblyNode>()

  let fileNames = HashSet<_>()

  member this.LoadFile(file: FileInfo) =
    if fileNames.Add(file.FullName) then
      children.Add(new TestAssemblyNode(file))

  member this.Children =
    children

  member this.Dispose() =
    for node in children do
      node.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

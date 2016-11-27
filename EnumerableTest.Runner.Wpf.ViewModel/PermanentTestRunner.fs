namespace EnumerableTest.Runner.Wpf

open System
open System.IO

type PermanentTestRunner() =
  let testTree = new TestTree()

  member this.TestTree = testTree

  member this.LoadFile(file: FileInfo) =
    testTree.LoadFile(file)

  member this.Dispose() =
    testTree.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

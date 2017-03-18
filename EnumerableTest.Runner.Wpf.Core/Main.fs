namespace EnumerableTest.Runner.Wpf

open System
open System.IO
open System.Reactive.Disposables
open System.Reactive.Subjects
open System.Reflection
open EnumerableTest.Runner
open EnumerableTest.Runner.UI.Notifications

[<Sealed>]
type Main() =
  let disposables =
    new CompositeDisposable()

  let notifier =
    new Notifier<Success, Info, Warning>(new Subject<_>()) :> INotifier
    |> tap disposables.Add

  let runner =
    new FileLoadingPermanentTestRunner(notifier)
    |> tap disposables.Add

  let testTree =
    new TestTree(runner, notifier)
    |> tap disposables.Add

  do
    let assemblyFiles =
      let thisFile = FileInfo(Assembly.GetExecutingAssembly().Location)
      [|
        yield! FileSystemInfo.getTestAssemblies thisFile
        yield! AppArgument.files
      |]

    for assemblyFile in assemblyFiles do
      runner.LoadFile(assemblyFile)

  member this.Notifier =
    notifier

  member this.TestTree =
    testTree

  member this.Dispose() =
    disposables.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

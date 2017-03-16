namespace EnumerableTest.Runner.Wpf

open System
open System.IO
open System.Reactive.Disposables;
open System.Reflection
open EnumerableTest.Runner
open EnumerableTest.Runner.Wpf.UI.Notifications

[<Sealed>]
type Main(toastNotifier: IToastNotifier) =
  let disposables =
    new CompositeDisposable()

  let notifier =
    new ConcreteNotifier()
    |> tap disposables.Add

  let logFile =
    new LogFile()
    |> tap disposables.Add

  let runner =
    new FileLoadingPermanentTestRunner(notifier)
    |> tap disposables.Add

  let testTree =
    new TestTree(runner, notifier)
    |> tap disposables.Add

  do
    logFile.ObserveNotifications(notifier)

  do
    toastNotifier |> ToastNotifier.subscribeNotifier notifier
    |> disposables.Add

  do
    let assemblyFiles =
      let thisFile = FileInfo(Assembly.GetExecutingAssembly().Location)
      [|
        yield! FileSystemInfo.getTestAssemblies thisFile
        yield! AppArgument.files
      |]

    for assemblyFile in assemblyFiles do
      runner.LoadFile(assemblyFile)

  member this.TestTree =
    testTree

  member this.Dispose() =
    disposables.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

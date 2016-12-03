namespace EnumerableTest.Runner.Wpf

open System
open System.IO
open System.Reactive.Concurrency
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reactive.Threading.Tasks
open System.Reflection
open System.Windows.Input
open Reactive.Bindings
open Reactive.Bindings.Extensions
open EnumerableTest.Sdk
open EnumerableTest.Runner

[<AbstractClass>]
type PermanentTestAssembly() =
  inherit TestAssembly()

  abstract CancelCommand: ObservableCommand<unit>

  abstract SchemaUpdated: IObservable<TestSuiteSchemaDifference>

[<Sealed>]
type FileLoadingTestAssembly(file: FileInfo) =
  inherit PermanentTestAssembly()

  let assemblyName = AssemblyName.GetAssemblyName(file.FullName)

  let current =
    ReactiveProperty.create (None: option<OneshotTestAssembly>)

  let cancel () =
    match current.Value with
    | Some testAssembly ->
      testAssembly.Dispose()
      current.Value <- None
    | None -> ()

  let cancelCommand =
    current
    |> ReactiveProperty.map Option.isSome
    |> ObservableCommand.ofFunc cancel

  let currentTestSchema =
    ReactiveProperty.create [||]

  let schemaUpdated =
    currentTestSchema.Pairwise().Select
      (fun pair ->
        TestSuiteSchema.difference pair.OldItem pair.NewItem
      )

  let testResults =
    new Subject<_>()

  let subscription =
    new SingleAssignmentDisposable()

  let load () =
    cancel ()
    let testAssembly = new OneshotTestAssembly(file)
    let subscriptions = new CompositeDisposable()
    let unload () =
      subscriptions.Dispose()
      cancel()
    testAssembly.SchemaFuture |> Observable.subscribe
      (fun schema -> currentTestSchema.Value <- schema)
    |> subscriptions.Add
    testAssembly.TestResults.Subscribe
      ( testResults.OnNext
      , ignore >> unload
      , unload
      )
    |> subscriptions.Add
    current.Value <- Some testAssembly
    testAssembly.Start()

  let start () =
    load ()
    subscription.Disposable <-
      file |> FileInfo.subscribeChanged (TimeSpan.FromMilliseconds(100.0)) load

  member this.AssemblyName =
    assemblyName

  override this.CancelCommand =
    cancelCommand

  member this.TestSchema =
    currentTestSchema :> IReadOnlyReactiveProperty<_>

  override this.SchemaUpdated =
    schemaUpdated

  override this.TestResults =
    testResults :> IObservable<_>

  override this.Start() =
    start ()

  override this.Dispose() =
    subscription.Dispose()

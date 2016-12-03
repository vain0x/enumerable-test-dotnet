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
open Basis.Core
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
type FileLoadingPermanentTestAssembly(notifier: Notifier, file: FileInfo) =
  inherit PermanentTestAssembly()

  let assemblyName =
    match Result.catch (fun () -> AssemblyName.GetAssemblyName(file.FullName)) with
    | Success name ->
      name
    | Failure _ ->
      todo ""

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
    match OneshotTestAssembly.ofFile file with
    | Success testAssembly ->
      let subscriptions = new CompositeDisposable()
      let unload () =
        subscriptions.Dispose()
        cancel()
      currentTestSchema.Value <- testAssembly.Schema
      testAssembly.TestResults.Subscribe
        ( testResults.OnNext
        , ignore >> unload
        , unload
        )
      |> subscriptions.Add
      current.Value <- Some testAssembly
      testAssembly.Start()
    | Failure e ->
      notifier.NotifyWarning
        ( sprintf "Couldn't load an assembly '%s'." assemblyName.Name
        , [| ("Exception", e :> obj) |]
        )

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

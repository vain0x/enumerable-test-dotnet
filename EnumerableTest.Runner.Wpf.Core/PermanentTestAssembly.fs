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

  let currentTestAssembly =
    ReactiveProperty.create (None: option<OneshotTestAssembly>)

  let cancel () =
    match currentTestAssembly.Value with
    | Some testAssembly ->
      testAssembly.Dispose()
      currentTestAssembly.Value <- None
    | None -> ()

  let cancelCommand =
    currentTestAssembly
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
    currentTestAssembly
      .Select
        (fun testAssembly ->
          match testAssembly with
          | Some testAssembly ->
            testAssembly.TestResults
            |> fun o -> o.Finally(fun () -> cancel ())
          | None ->
            Observable.Empty()
        )
    |> fun o -> o.Switch()

  let subscription =
    new SingleAssignmentDisposable()

  let load () =
    cancel ()
    match OneshotTestAssembly.ofFile file with
    | Success testAssembly ->
      currentTestSchema.Value <- testAssembly.Schema
      currentTestAssembly.Value <- Some testAssembly
      testAssembly.Start()
    | Failure e ->
      notifier.NotifyWarning
        ( sprintf "Couldn't load an assembly '%s'." file.Name
        , [| ("Exception", e :> obj) |]
        )

  let start () =
    load ()
    subscription.Disposable <-
      file |> FileInfo.subscribeChanged (TimeSpan.FromMilliseconds(100.0)) load

  override this.CancelCommand =
    cancelCommand

  member this.TestSchema =
    currentTestSchema :> IReadOnlyReactiveProperty<_>

  override this.SchemaUpdated =
    schemaUpdated

  override this.TestResults =
    testResults

  override this.Start() =
    start ()

  override this.Dispose() =
    subscription.Dispose()

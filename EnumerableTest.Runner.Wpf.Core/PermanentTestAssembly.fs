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
open FSharp.Control.Reactive
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

  let disposables =
    new CompositeDisposable()

  let cancelRequested =
    new Subject<_>()

  let reloadRequested =
    new Subject<_>()

  let tryLoad () =
    match OneshotTestAssembly.ofFile file with
    | Success testAssembly ->
      testAssembly |> Some
    | Failure e ->
      notifier.NotifyWarning
        ( sprintf "Couldn't load an assembly '%s'." file.Name
        , [| ("Exception", e :> obj) |]
        )
      None

  let currentTestAssembly =
    reloadRequested
    |> Observable.map tryLoad
    |> Observable.merge (cancelRequested |> Observable.map (fun () -> None))
    |> ReactiveProperty.ofObservable None

  let testAssemblyTrash =
    let trash = new SerialDisposable()
    currentTestAssembly |> Observable.subscribe
      (fun testAssembly ->
        trash.Disposable <-
          match testAssembly with
          | Some testAssembly -> testAssembly :> IDisposable
          | None -> Disposable.Empty
      ) |> ignore
    disposables.Add(trash)
    trash

  let cancel () =
    cancelRequested.OnNext(())

  let cancelCommand =
    currentTestAssembly
    |> ReactiveProperty.map Option.isSome
    |> ObservableCommand.ofFunc cancel

  let currentTestSchema =
    currentTestAssembly
    |> Observable.choose id
    |> Observable.map (fun testAssembly -> testAssembly.Schema)
    |> ReactiveProperty.ofObservable TestSuiteSchema.empty

  let schemaUpdated =
    currentTestSchema
    |> Observable.pairwise
    |> Observable.map
      (fun (oldSchema, newSchema) ->
        TestSuiteSchema.difference oldSchema newSchema
      )

  let testCompleted =
    currentTestAssembly
    |> Observable.map
      (fun testAssembly ->
        match testAssembly with
        | Some testAssembly ->
          testAssembly.TestCompleted
          |> Observable.finallyDo cancel
        | None ->
          Observable.Empty()
      )
    |> Observable.switch

  let start () =
    reloadRequested.OnNext(())
    file |> FileInfo.subscribeChanged (TimeSpan.FromMilliseconds(100.0))
      (fun () -> reloadRequested.OnNext(()))
    |> disposables.Add

  do
    currentTestAssembly
    |> Observable.choose id
    |> Observable.subscribe (fun testAssembly -> testAssembly.Start())
    |> ignore

  override this.CancelCommand =
    cancelCommand

  member this.TestSchema =
    currentTestSchema :> IReadOnlyReactiveProperty<_>

  override this.SchemaUpdated =
    schemaUpdated

  override this.TestCompleted =
    testCompleted

  override this.Start() =
    start ()

  override this.Dispose() =
    disposables.Dispose()

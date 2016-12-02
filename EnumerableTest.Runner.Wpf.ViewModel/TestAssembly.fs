namespace EnumerableTest.Runner.Wpf

open System
open System.IO
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reflection
open System.Windows.Input
open Reactive.Bindings
open Reactive.Bindings.Extensions
open EnumerableTest.Sdk
open EnumerableTest.Runner

module TestAssemblyModule =
  let load (assemblyName: AssemblyName) observer =
    MarshalValue.Recursion <- 3
    try
      let assembly = Assembly.Load(assemblyName)
      let (schema, connectable) =
        TestSuite.ofAssemblyAsObservable assembly
      connectable.Subscribe(observer) |> ignore<IDisposable>
      connectable.Connect()
      schema |> Some
    with
    | _ ->
      None

[<AbstractClass>]
type PermanentTestAssembly() =
  abstract CancelCommand: ObservableCommand<unit>

  abstract SchemaUpdated: IObservable<TestSuiteSchemaDifference>

  abstract TestResults: IObservable<TestResult>

  abstract Start: unit -> unit

  abstract Dispose: unit -> unit

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

[<Sealed>]
type FileLoadingTestAssembly(file: FileInfo) =
  inherit PermanentTestAssembly()

  let assemblyName = AssemblyName.GetAssemblyName(file.FullName)

  let currentDomain = ReactiveProperty.create None

  let currentTestSchema = ReactiveProperty.create [||]

  let cancel () =
    match currentDomain.Value with
    | Some domain ->
      (domain: AppDomain.DisposableAppDomain).Dispose()
      currentDomain.Value <- None
    | None -> ()

  let cancelCommand =
    currentDomain
    |> ReactiveProperty.map Option.isSome
    |> ObservableCommand.ofFunc cancel

  let schemaUpdated =
    currentTestSchema.Pairwise().Select
      (fun pair ->
        TestSuiteSchema.difference pair.OldItem pair.NewItem
      )

  let testResults =
    new Subject<TestResult>()

  let resultObserver =
    { new IObserver<_> with
        override this.OnNext(result) =
          testResults.OnNext(result)
        override this.OnError(_) =
          cancel ()
        override this.OnCompleted() =
          cancel ()
    }

  let load () =
    let domainName =
      sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())
    let domain =
      cancel ()
      let domain = AppDomain.create domainName
      currentDomain.Value <- Some domain
      domain
    let (schema, connectable) =
      domain.Value
      |> AppDomain.runObservable (TestAssemblyModule.load assemblyName)
    match schema with
    | Some schema->
      currentTestSchema.Value <- schema
      connectable.Subscribe(resultObserver) |> ignore<IDisposable>
      connectable.Connect()
    | None ->
      cancel ()

  let subscription =
    new SingleAssignmentDisposable()

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

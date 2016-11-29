namespace EnumerableTest.Runner.Wpf

open System
open System.IO
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reflection
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

type TestAssembly(file: FileInfo) =
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
    |> ReactiveCommand.ofFunc cancel

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

  member this.CancelCommand =
    cancelCommand

  member this.TestSchema =
    currentTestSchema :> IReadOnlyReactiveProperty<_>

  member this.SchemaUpdated =
    schemaUpdated :> IObservable<_>

  member this.TestResults =
    testResults :> IObservable<_>

  member this.Start() =
    start ()

  member this.Dispose() =
    subscription.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

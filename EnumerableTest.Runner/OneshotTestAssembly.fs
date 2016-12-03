namespace EnumerableTest.Runner

open System
open System.IO
open System.Reactive.Disposables
open System.Reactive.Subjects
open System.Reflection
open System.Threading.Tasks

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

[<Sealed>]
type OneshotTestAssembly(file: FileInfo) =
  inherit TestAssembly()

  let assemblyName =
    AssemblyName.GetAssemblyName(file.FullName)

  let resource =
    new CompositeDisposable()

  let domain =
    sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())
    |> AppDomain.create
    |> tap resource.Add

  let schemaFuture =
    new FutureSource<TestSuiteSchema>()
    |> tap resource.Add

  let testResults =
    new Subject<TestResult>()

  do
    Disposable.Create
      (fun () ->
        testResults.OnCompleted()
        testResults.Dispose()
      )
    |> resource.Add

  let resultObserver =
    { new IObserver<_> with
        override this.OnNext(result) =
          testResults.OnNext(result)
        override this.OnError(_) =
          resource.Dispose()
        override this.OnCompleted() =
          resource.Dispose()
    }

  let start () =
    let (schema, connectable) =
      domain.Value
      |> AppDomain.runObservable (TestAssemblyModule.load assemblyName)
    match schema with
    | Some schema->
      schemaFuture.Value <- schema
      connectable.Subscribe(resultObserver) |> resource.Add
      connectable.Connect()
    | None ->
      resource.Dispose()

  member this.AssemblyName =
    assemblyName

  member this.SchemaFuture =
    schemaFuture :> IFuture<_>

  override this.TestResults =
    testResults :> IObservable<_>

  override this.Start() =
    start ()

  override this.Dispose() =
    resource.Dispose()

namespace EnumerableTest.Runner

open System
open System.IO
open System.Reactive.Disposables
open System.Reactive.Subjects
open System.Reflection
open System.Threading.Tasks
open Basis.Core

module private OneshotTestAssemblyCore =
  let loadSchema (assemblyName: AssemblyName) () =
    Result.catch (fun () -> Assembly.Load(assemblyName))
    |> Result.map TestSuiteSchema.ofAssembly

  let load (assemblyName: AssemblyName) observer =
    MarshalValue.Recursion <- 3
    try
      let assembly = Assembly.Load(assemblyName)
      let connectable =
        TestSuite.ofAssembly assembly
      connectable.Subscribe(observer) |> ignore<IDisposable>
      connectable.Connect()
      () |> Some
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

  let testResults =
    new Subject<TestResult>()

  do
    Disposable.Create
      (fun () ->
        testResults.OnCompleted()
        testResults.Dispose()
      )
    |> resource.Add

  let testSuiteSchema =
    let result = 
      domain.Value |> AppDomain.run (OneshotTestAssemblyCore.loadSchema assemblyName)
    match result with
    | Success schema ->
      schema
    | Failure e ->
      todo ""

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
    let (result, connectable) =
      domain.Value
      |> AppDomain.runObservable (OneshotTestAssemblyCore.load assemblyName)
    match result with
    | Some ()->
      connectable.Subscribe(resultObserver) |> resource.Add
      connectable.Connect()
    | None ->
      resource.Dispose()

  member this.AssemblyName =
    assemblyName

  member this.Schema =
    testSuiteSchema

  override this.TestResults =
    testResults :> IObservable<_>

  override this.Start() =
    start ()

  override this.Dispose() =
    resource.Dispose()

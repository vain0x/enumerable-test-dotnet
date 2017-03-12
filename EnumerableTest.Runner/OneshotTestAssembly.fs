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

  let load (assemblyName: AssemblyName) marshalValueRecursion observer =
    MarshalValue.Recursion <- marshalValueRecursion
    try
      let assembly = Assembly.Load(assemblyName)
      let connectable =
        TestSuite.runTestAssembly assembly
      connectable.Subscribe(observer) |> ignore<IDisposable>
      connectable.Connect() |> ignore
      () |> Some
    with
    | _ ->
      None

[<Sealed>]
type OneshotTestAssembly(assemblyName, domain, testSuiteSchema) =
  inherit TestAssembly()

  let marshalValueRecursion =
    MarshalValue.Recursion

  let resource =
    new CompositeDisposable()

  do resource.Add(domain)

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
    let (result, connectable) =
      (domain: AppDomain.DisposableAppDomain).Value
      |> AppDomain.runObservable (OneshotTestAssemblyCore.load assemblyName marshalValueRecursion)
    match result with
    | Some ()->
      connectable.Subscribe(resultObserver) |> resource.Add
      connectable.Connect() |> ignore
    | None ->
      resource.Dispose()

  member this.AssemblyName =
    assemblyName

  member this.Schema =
    testSuiteSchema

  override this.TestCompleted =
    testResults :> IObservable<_>

  override this.Start() =
    start ()

  override this.Dispose() =
    resource.Dispose()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]  
module OneshotTestAssembly =
  let ofFile (file: FileInfo) =
    result {
      let! assemblyName =
        Result.catch (fun () -> AssemblyName.GetAssemblyName(file.FullName))
      let domain =
        sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())
        |> AppDomain.create
      let! schema = domain.Value |> AppDomain.run (OneshotTestAssemblyCore.loadSchema assemblyName)
      return new OneshotTestAssembly(assemblyName, domain, schema)
    }

namespace EnumerableTest.Runner

open System
open System.IO
open System.Reactive.Disposables
open System.Reactive.Subjects
open System.Reflection
open System.Threading
open System.Threading.Tasks
open Basis.Core

module private OneshotTestAssemblyCore =
  let loadSchema (assemblyName: AssemblyName) () =
    Result.catch (fun () -> Assembly.Load(assemblyName))
    |> Result.map TestSuiteSchema.ofAssembly

  let load (assemblyName: AssemblyName) marshalValueRecursion observer () =
    MarshalValue.Recursion <- marshalValueRecursion
    try
      let assembly = Assembly.Load(assemblyName)
      let connectable =
        TestRunner.runTestAssembly assembly
      connectable.Subscribe(observer) |> ignore<IDisposable>
      connectable.Connect() |> ignore
      Success ()
    with
    | e ->
      Failure e

[<Sealed>]
type OneshotTestAssembly(assemblyName, domain, testSuiteSchema) =
  inherit TestAssembly()

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

  let mutable isTerminated = false

  do
    // When all tests terminated, dispose this itself.
    let period = TimeSpan.FromMilliseconds(17.0)
    let onTick _ = if isTerminated then resource.Dispose()
    let timer = new Timer(onTick, (), period, period)
    resource.Add(timer)

  let resultObserver =
    { new IObserver<TestResult> with
        override this.OnNext(value) =
          testResults.OnNext(value)
        override this.OnError(e) =
          isTerminated <- true
        override this.OnCompleted() =
          isTerminated <- true
    } |> MarshalByRefObserver.ofObserver

  let start () =
    let f =
      OneshotTestAssemblyCore.load assemblyName MarshalValue.Recursion resultObserver
    let result =
      (domain: AppDomain.DisposableAppDomain).Value |> AppDomain.run f
    match result with
    | Success () ->
      ()
    | Failure _ ->
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

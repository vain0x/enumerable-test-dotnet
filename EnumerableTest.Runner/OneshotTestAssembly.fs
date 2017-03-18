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
type OneshotTestAssembly
  ( assemblyName: AssemblyName
  , domain: AppDomain.DisposableAppDomain
  , testSuiteSchema: TestSuiteSchema
  ) =
  inherit TestAssembly()

  let disposables =
    new CompositeDisposable()

  do disposables.Add(domain)

  let testCompleted =
    new Subject<TestResult>()

  do
    Disposable.Create
      (fun () ->
        testCompleted.OnCompleted()
        testCompleted.Dispose()
      )
    |> disposables.Add

  let start () =
    let onTerminated () =
      let onTick _ = testCompleted.OnCompleted()
      let dueTime = TimeSpan.FromMilliseconds(100.0)
      let timer = new Timer(onTick, (), dueTime, Timeout.InfiniteTimeSpan)
      disposables.Add(timer)
    let observer =
      { new IObserver<TestResult> with
          override this.OnNext(value) =
            testCompleted.OnNext(value)
          override this.OnError(e) =
            onTerminated ()
          override this.OnCompleted() =
            onTerminated ()
      } |> MarshalByRefObserver.ofObserver
    let load =
      OneshotTestAssemblyCore.load assemblyName MarshalValue.Recursion observer
    match domain.Value |> AppDomain.run load with
    | Success () ->
      ()
    | Failure _ ->
      disposables.Dispose()

  member this.AssemblyName =
    assemblyName

  member this.Schema =
    testSuiteSchema

  override this.TestCompleted =
    testCompleted :> IObservable<_>

  override this.Start() =
    start ()

  override this.Dispose() =
    disposables.Dispose()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]  
module OneshotTestAssembly =
  let private domainName (assemblyName: AssemblyName) =
    sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())

  let ofFile (file: FileInfo) =
    result {
      let! assemblyName =
        Result.catch (fun () -> AssemblyName.GetAssemblyName(file.FullName))
      let domain = AppDomain.create (domainName assemblyName)
      let loadSchema = OneshotTestAssemblyCore.loadSchema assemblyName
      let! schema = domain.Value |> AppDomain.run loadSchema
      return new OneshotTestAssembly(assemblyName, domain, schema)
    }

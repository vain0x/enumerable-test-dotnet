namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.IO
open System.Reflection
open System.Threading
open EnumerableTest.Runner
open EnumerableTest.Sdk

module Model =
  let loadAssembly (assemblyName: AssemblyName) =
    MarshalValue.Recursion <- 2
    let assembly = Assembly.Load(assemblyName)
    let testSuite = TestSuite.ofAssembly assembly
    let results = ResizeArray()
    let execution =
      testSuite |> TestSuite.runAsync
    execution |> Observable.subscribe
      (fun testClassResult ->
        results.Add(testClassResult |> TestClassResult.toSerializable)
      )
      |> ignore<IDisposable>
    execution.Connect()
    execution |> Observable.wait
    results :> seq<_>

type TestTree() =
  let children = ObservableCollection<TestClassNode>()

  let tryFindChild testClassFullName =
    children |> Seq.tryFind (fun ch -> ch.Name = testClassFullName)

  let disposables = ResizeArray<IDisposable>()

  let context =
    SynchronizationContext.Current

  let watchAssemblyFile (load: unit -> unit) (file: FileInfo) =
    let watcher = new FileSystemWatcher(file.DirectoryName, file.Name)
    watcher.NotifyFilter <- NotifyFilters.LastWrite
    watcher.Changed.Add(fun _ -> load ())
    watcher.EnableRaisingEvents <- true
    disposables.Add(watcher)

  let updateResult result =
    for (testClassFullName, testMethodResults) in result do
      match tryFindChild testClassFullName with
      | Some node ->
        node.Update(testMethodResults)
      | None ->
        let node = TestClassNode(testClassFullName)
        node.Update(testMethodResults)
        children.Add(node)

  member this.LoadAssemblyInNewDomain(assemblyName: AssemblyName) =
    let domainName =
      sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())
    use runnerDomain =
      AppDomain.create domainName
    let result =
      runnerDomain.Value |> AppDomain.run (fun () -> Model.loadAssembly assemblyName)
    context.Send ((fun _ -> updateResult result), ())

  member this.LoadFile(file: FileInfo) =
    let assemblyName = AssemblyName.GetAssemblyName(file.FullName)
    let load () = this.LoadAssemblyInNewDomain(assemblyName)
    watchAssemblyFile load file
    load ()

  member this.Children =
    children

  member this.Dispose() =
    for disposable in disposables do
      disposable.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

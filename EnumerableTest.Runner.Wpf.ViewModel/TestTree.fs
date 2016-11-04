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
    try
      let assembly = Assembly.Load(assemblyName)
      let testSuite = TestSuite.ofAssembly assembly
      testSuite
    with
    | _ -> TestSuite.empty

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

  let updateResult (testSuite: TestSuite) =
    for testClass in testSuite do
      match tryFindChild testClass.TypeFullName with
      | Some node ->
        node.Update(testClass)
      | None ->
        let node = TestClassNode(testClass.TypeFullName)
        node.Update(testClass)
        children.Add(node)

  member this.LoadAssemblyInNewDomain(assemblyName: AssemblyName) =
    let domainName =
      sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())
    use runnerDomain =
      AppDomain.create domainName
    let testSuite =
      runnerDomain.Value |> AppDomain.run (fun () -> Model.loadAssembly assemblyName)
    context.Send ((fun _ -> updateResult testSuite), ())

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

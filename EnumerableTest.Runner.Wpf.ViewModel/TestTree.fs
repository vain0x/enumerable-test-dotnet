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

  let updateResult assemblyShortName (testSuite: TestSuite) =
    let oldNodes =
      children |> Seq.filter (fun node -> node.AssemblyShortName = assemblyShortName)
    let difference =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestClassNode).Name)
        (fun testClass -> (testClass: TestClass).TypeFullName)
        (oldNodes |> Seq.toArray)
        testSuite
    for removedNode in difference.Left do
      children.Remove(removedNode) |> ignore<bool>
    for (_, updatedNode, testClass) in difference.Intersect do
      updatedNode.Update(testClass)
    for testClass in difference.Right do
      let node = TestClassNode(assemblyShortName, testClass.TypeFullName)
      node.Update(testClass)
      children.Add(node)

  member this.LoadAssemblyInNewDomain(assemblyName: AssemblyName) =
    let domainName =
      sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())
    use runnerDomain =
      AppDomain.create domainName
    let testSuite =
      runnerDomain.Value |> AppDomain.run (fun () -> Model.loadAssembly assemblyName)
    context.Send ((fun _ -> updateResult assemblyName.Name testSuite), ())

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

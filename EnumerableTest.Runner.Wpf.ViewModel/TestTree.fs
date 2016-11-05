namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.IO
open System.Reflection
open System.Threading
open DotNetKit.Observing
open DotNetKit.Threading.Experimental
open EnumerableTest.Runner
open EnumerableTest.Sdk

module Model =
  let loadAssembly (assemblyName: AssemblyName) observer =
    MarshalValue.Recursion <- 2
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
    watcher.Changed
      .Throttle(
        TimeSpan.FromMilliseconds(100.0),
        (fun _ -> ()),
        (fun _ _ -> ()),
        Scheduler.WorkerThread
      )
      .Add(load)
    watcher.EnableRaisingEvents <- true
    disposables.Add(watcher)

  let updateSchema assemblyShortName (schema: TestSuiteSchema) =
    let nodes =
      children
      |> Seq.filter (fun node -> node.AssemblyShortName = assemblyShortName)
      |> Seq.toArray
    let difference =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestClassNode).Name)
        fst
        nodes
        schema
    for removedNode in difference.Left do
      children.Remove(removedNode) |> ignore<bool>
    for (_, updatedNode, testClassSchema) in difference.Intersect do
      updatedNode.UpdateSchema(testClassSchema)
    for testClassSchema in difference.Right do
      let node = TestClassNode(assemblyShortName, testClassSchema |> fst)
      node.UpdateSchema(testClassSchema)
      children.Add(node)

  let updateResult assemblyShortName (testClass: TestClass) =
    children
    |> Seq.tryFind (fun node -> node.Name = testClass.TypeFullName)
    |> Option.iter (fun node -> node.Update(testClass))

  let loadAssembly (assemblyName: AssemblyName) =
    let domainName =
      sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())
    let runnerDomain =
      AppDomain.create domainName
    let result =
      runnerDomain.Value
      |> AppDomain.runObservable (Model.loadAssembly assemblyName)
    match result with
    | (Some schema, connectable) ->
      context |> SynchronizationContext.send
        (fun () -> updateSchema assemblyName.Name schema)
      connectable.Subscribe
        { new IObserver<_> with
            override this.OnNext(testClass) =
              context |> SynchronizationContext.send
                (fun () -> updateResult assemblyName.Name testClass)
            override this.OnError(_) = ()
            override this.OnCompleted() =
              (runnerDomain :> IDisposable).Dispose()
        } |> ignore<IDisposable>
      connectable.Connect()
    | (None, _) ->
      (runnerDomain :> IDisposable).Dispose()

  member this.LoadFile(file: FileInfo) =
    let assemblyName = AssemblyName.GetAssemblyName(file.FullName)
    let load () = loadAssembly assemblyName
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

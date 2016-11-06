namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.IO
open System.Reflection
open System.Threading
open DotNetKit.Observing
open EnumerableTest.Sdk
open EnumerableTest.Runner

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

type TestAssemblyNode(file: FileInfo) =
  let context =
    SynchronizationContext.Current

  let assemblyName = AssemblyName.GetAssemblyName(file.FullName)

  let children = ObservableCollection<_>()

  let updateSchema (schema: TestSuiteSchema) =
    let difference =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestClassNode).Name)
        (fun testClassSchema -> (testClassSchema: TestClassSchema).TypeFullName)
        (children |> Seq.toArray)
        schema
    for removedNode in difference.Left do
      children.Remove(removedNode) |> ignore<bool>
    for (_, updatedNode, testClassSchema) in difference.Intersect do
      updatedNode.UpdateSchema(testClassSchema)
    for testClassSchema in difference.Right do
      let node = TestClassNode(assemblyName.Name, testClassSchema.TypeFullName)
      node.UpdateSchema(testClassSchema)
      children.Add(node)

  let updateResult (testMethodResult: TestMethodResult) =
    children
    |> Seq.tryFind (fun node -> node.Name = testMethodResult.TypeFullName)
    |> Option.iter (fun node -> node.Update(testMethodResult.Method))
    
  let testClassObserver onCompleted =
    { new IObserver<_> with
        override this.OnNext(testClass) =
          context |> SynchronizationContext.send
            (fun () -> updateResult testClass)
        override this.OnError(_) = ()
        override this.OnCompleted() = onCompleted ()
    }

  let load () =
    let domainName =
      sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())
    let runnerDomain =
      AppDomain.create domainName
    let dispose =
      (runnerDomain :> IDisposable).Dispose
    let result =
      runnerDomain.Value
      |> AppDomain.runObservable (Model.loadAssembly assemblyName)
    match result with
    | (Some schema, connectable) ->
      context |> SynchronizationContext.send
        (fun () -> updateSchema schema)
      connectable.Subscribe(testClassObserver dispose) |> ignore<IDisposable>
      connectable.Connect()
    | (None, _) ->
      dispose ()

  let subscription =
    file |> FileInfo.subscribeChanged (TimeSpan.FromMilliseconds(100.0)) load

  do load ()

  member this.Name = assemblyName.Name

  member this.Children =
    children

  member this.Dispose() =
    subscription.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

  interface INodeViewModel with
    override this.IsExpanded =
      Uptodate.True

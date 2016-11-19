namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.IO
open System.Reflection
open System.Threading
open EnumerableTest.Sdk
open EnumerableTest.Runner

module Model =
  let loadAssembly (assemblyName: AssemblyName) observer =
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

type TestAssemblyNode(file: FileInfo) =
  let context =
    SynchronizationContext.capture ()

  let assemblyName = AssemblyName.GetAssemblyName(file.FullName)

  let currentDomain = ReactiveProperty.create None

  let cancel () =
    match currentDomain.Value with
    | Some domain ->
      ((domain: AppDomain.DisposableAppDomain) :> IDisposable).Dispose()
      context |> SynchronizationContext.send
        (fun () -> currentDomain.Value <- None)
    | None -> ()

  let cancelCommand =
    let command = currentDomain |> ReactiveProperty.map Option.isSome |> ReactiveCommand.create
    command.Subscribe(cancel) |> ignore<IDisposable>
    command

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
      cancel ()
      let domain = AppDomain.create domainName
      context |> SynchronizationContext.send
        (fun () -> currentDomain.Value <- Some domain)
      domain
    let result =
      runnerDomain.Value
      |> AppDomain.runObservable (Model.loadAssembly assemblyName)
    match result with
    | (Some schema, connectable) ->
      context |> SynchronizationContext.send
        (fun () -> updateSchema schema)
      connectable.Subscribe(testClassObserver cancel) |> ignore<IDisposable>
      connectable.Connect()
    | (None, _) ->
      cancel ()

  let testStatistic =
    children
    |> ReadOnlyUptodateCollection.ofObservableCollection
    |> ReadOnlyUptodateCollection.collect
      (fun node -> (node: TestClassNode).TestStatistic |> ReadOnlyUptodateCollection.ofUptodate)
    |> ReadOnlyUptodateCollection.sumBy TestStatistic.groupSig

  let subscription =
    file |> FileInfo.subscribeChanged (TimeSpan.FromMilliseconds(100.0)) load

  do load ()

  member this.Name = assemblyName.Name

  member this.Children =
    children

  member this.CancelCommand =
    cancelCommand

  member this.TestStatistic =
    testStatistic

  member this.Dispose() =
    cancel ()
    subscription.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

  interface INodeViewModel with
    override val IsExpanded =
      ReactiveProperty.create true
      |> ReactiveProperty.asReadOnly

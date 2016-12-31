namespace EnumerableTest.Runner

[<AutoOpen>]
module Misc =
  open System

  let todo message =
    NotImplementedException(message) |> raise

  let tap f x =
     f x
     x

  let tryCast<'x, 'y> (x: 'x) =
    match x |> box with
    | :? 'y as y ->
      Some y
    | _ ->
      None

module Seq =
  open System.Collections.Generic

  let indexed xs =
    xs |> Seq.mapi (fun i x -> (i, x))

  /// Applies f for each element in xs and partition them into two list.
  /// The first is y's where f x = Some y
  /// and the other is x's where f x = None.
  let paritionMap (f: 'x -> option<'y>) (xs: seq<'x>): (IReadOnlyList<'y> * IReadOnlyList<'x>) =
    let firsts = ResizeArray()
    let seconds = ResizeArray()
    for x in xs do
      match f x with
      | Some y ->
        firsts.Add(y)
      | None ->
        seconds.Add(x)
    (firsts :> IReadOnlyList<_>, seconds :> IReadOnlyList<_>)

module Option =
  let tryCatch f =
    try
      f ()
      None
    with
    | e -> Some e

module Result =
  open Basis.Core

  let catch f =
    try
      f () |> Success
    with
    | e -> Failure e

  let toObj =
    function
    | Success x ->
      x :> obj
    | Failure x ->
      x :> obj

  let ofObj<'s, 'f> (o: obj) =
    match o with
    | :? 's as value ->
      value |> Success |> Some
    | :? 'f as value ->
      value |> Failure |> Some
    | _ ->
      None

module Dictionary =
  open System.Collections.Generic

  let tryFind key (this: Dictionary<_, _>) =
    match this.TryGetValue(key) with
    | (true, value) ->
      Some value
    | (false, _) ->
      None

  let ofSeq kvs =
    let this = Dictionary()
    for (key, value) in kvs do
      this.Add(key, value)
    this

module ReadOnlyList =
  open System.Collections.Generic
  open EnumerableTest.Runner

  type SymmetricDifference<'k, 'x, 'y> =
    {
      Left                      : IReadOnlyList<'x>
      Intersect                 : IReadOnlyList<'k * 'x * 'y>
      Right                     : IReadOnlyList<'y>
    }

  let symmetricDifferenceBy
      (xKey: 'x -> 'k)
      (yKey: 'y -> 'k)
      (xs: IReadOnlyList<'x>)
      (ys: IReadOnlyList<'y>)
    =
    let xMap = xs |> Seq.map (fun x -> (xKey x, x)) |> Map.ofSeq
    let (intersect, right) =
      ys |> Seq.paritionMap
        (fun y ->
          let k = yKey y
          xMap
          |> Map.tryFind k
          |> Option.map (fun x -> (k, x, y))
        )
    let intersectKeys =
      intersect |> Seq.map (fun (k, _, _) -> k) |> set
    let left =
      xs
      |> Seq.filter (fun x -> intersectKeys |> Set.contains (xKey x) |> not)
      |> Seq.toArray
    {
      Left                      = left :> IReadOnlyList<_>
      Intersect                 = intersect
      Right                     = right
    }

module Counter =
  open System.Threading

  let private counter = ref 0
  let generate () =
    Interlocked.Increment(counter)

module String =
  open Basis.Core

  let replace (source: string) (dest: string) (this: string) =
    this.Replace(source, dest)

  let convertToLF this =
    this |> replace "\r\n" "\n" |> replace "\r" "\n"

  let splitByLinebreak this =
    this |> convertToLF |> Str.splitBy "\n"

module Async =
  let result x =
    async {
      return x
    }

  let run f =
    async {
      return f ()
    }

  let map f a =
    async {
      let! x = a
      return f x
    }

module Disposable =
  open System

  let dispose (x: obj) =
    match x with
    | null -> ()
    | :? IDisposable as disposable ->
      disposable.Dispose()
    | _ -> ()

  let ofObj (x: obj) =
    { new IDisposable with
        override this.Dispose() =
          x |> dispose
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Type =
  open System
  open System.Collections
  open System.Collections.Generic
  open Basis.Core

  let tryGetGenericTypeDefinition (this: Type) =
    if this.IsGenericType
      then this.GetGenericTypeDefinition() |> Some
      else None

  let interfaces (this: Type) =
    seq {
      if this.IsInterface then
        yield this
      yield! this.GetInterfaces()
    }

  let isCollectionType (this: Type) =
    seq {
      for ``interface`` in this |> interfaces do
        if ``interface`` = typeof<ICollection> then
          yield true
        else
          match ``interface`` |> tryGetGenericTypeDefinition with
          | Some genericInterface ->
            if genericInterface = typedefof<IReadOnlyCollection<_>>
              || genericInterface = typedefof<ICollection<_>>
              then yield true
          | None -> ()
    }
    |> Seq.exists id

  let isKeyValuePairType (this: Type) =
    this |> tryGetGenericTypeDefinition |> Option.exists ((=) typedefof<KeyValuePair<_, _>>)

  let tryMatchKeyedCollectionType (this: Type) =
    query {
      for ``interface`` in this |> interfaces do
      where (``interface``.IsGenericType)
      let genericInterface = ``interface``.GetGenericTypeDefinition()
      where
        (genericInterface = typedefof<IReadOnlyCollection<_>>
        || genericInterface = typedefof<ICollection<_>>)
      let elementType = ``interface``.GetGenericArguments().[0]
      where (elementType |> isKeyValuePairType)
      let types = elementType.GetGenericArguments()
      select (KeyValuePair<_, _>(types.[0], types.[1]))
    }
    |> Seq.tryHead

  let prettyName: Type -> string =
    let abbreviations =
      [
        (typeof<int>, "int")
        (typeof<int64>, "long")
        (typeof<float>, "double")
        (typeof<obj>, "object")
        (typeof<string>, "string")
      ] |> dict
    let rec prettyName (this: Type) =
      if this.IsGenericType then
        let name = this.Name |> Str.takeWhile ((<>) '`')
        let arguments = this.GenericTypeArguments |> Seq.map prettyName |> String.concat ", "
        sprintf "%s<%s>" name arguments
      else
        match abbreviations.TryGetValue(this) with
        | (true, name) -> name
        | (false, _) -> this.Name
    prettyName

module SynchronizationContext =
  open System.Threading

  let immediate =
    { new SynchronizationContext() with
        override this.Post(f, state) =
          f.Invoke(state)
        override this.Send(f, state) =
          f.Invoke(state)
    }

  let current () =
    SynchronizationContext.Current |> Option.ofObj

  let capture () =
    match current () with
    | Some c -> c
    | None -> immediate

  let send f (this: SynchronizationContext) =
    this.Send((fun _ -> f ()), ())

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Command =
  open System.Windows.Input

  let never =
    let canExecuteChanged = Event<_, _>()
    { new ICommand with
        override this.CanExecute(_) = false
        override this.Execute(_) = ()
        [<CLIEvent>]
        override this.CanExecuteChanged = canExecuteChanged.Publish
    }

module Observable =
  open System
  open System.Collections
  open System.Collections.Generic
  open System.Reactive.Linq
  open System.Reactive.Subjects
  open System.Threading

  type NotificationCollection<'x>(source: IObservable<'x>) =
    let notifications = ResizeArray()
    let mutable result = (None: option<option<exn>>)
    let subscription =
      source.Subscribe
        ( notifications.Add
        , fun error ->
            result <- Some (Some error)
        , fun () ->
            result <- Some None
        )

    member this.Result = result

    member this.Count = notifications.Count

    interface IEnumerable<'x> with
      override this.GetEnumerator() =
        notifications.GetEnumerator() :> IEnumerator<_>

      override this.GetEnumerator() =
        notifications.GetEnumerator() :> IEnumerator

    interface IDisposable with
      override this.Dispose() =
        subscription.Dispose()

  let collectNotifications (this: IObservable<_>) =
    new NotificationCollection<_>(this)

  type IConnectableObservable<'x> =
    inherit IObservable<'x>

    abstract member Connect: unit -> unit

  let subscribeEnd f (observable: IObservable<_>) =
    let observer =
      { new IObserver<_> with
          override this.OnNext(value) = ()
          override this.OnError(error) =
            error |> Some |> f
          override this.OnCompleted() =
            f None
      }
    observable.Subscribe(observer)

  let waitTimeout (timeout: TimeSpan) observable =
    use event = new ManualResetEvent(initialState = false)
    use subscription =
      observable
      |> subscribeEnd (fun _ -> event.Set() |> ignore<bool>)
    event.WaitOne(timeout)

  let wait observable =
    observable
    |> waitTimeout Timeout.InfiniteTimeSpan
    |> ignore<bool>

  /// Creates a connectable observable
  /// which executes async tasks when connected and notifies each result.
  let ofParallel asyncs =
    let gate = obj()
    let subject = new Subject<_>()
    let computation =
      async {
        let! (_: array<unit>) =
          asyncs |> Seq.map
            (fun a ->
              async {
                let! x = a
                lock gate (fun () -> (subject :> IObserver<_>).OnNext(x))
              }
            )
          |> Async.Parallel
        (subject :> IObserver<_>).OnCompleted()
      }
    { new IConnectableObservable<_> with
        override this.Connect() =
          Async.Start(computation)
        override this.Subscribe(observer) =
          (subject :> IObservable<_>).Subscribe(observer)
    }

  let startParallel computations =
    let gate = obj()
    let subject = new Subject<_>()
    let computations = computations |> Seq.toArray
    let connect () =
      let mutable count = 0
      for computation in computations do
        async {
          let! x = computation
          lock gate (fun () -> (subject :> IObserver<_>).OnNext(x))
          if Interlocked.Increment(&count) = computations.Length then
            lock gate (fun () -> (subject :> IObserver<_>).OnCompleted())
        } |> Async.Start
    { new IConnectableObservable<_> with
        override this.Connect() =
          connect ()
        override this.Subscribe(observer) =
          (subject :> IObservable<_>).Subscribe(observer)
    }

module MarshalByRefObject =
  open System

  type MarshalByRefValue<'x>(value: 'x) =
    inherit MarshalByRefObject()

    member val Value = value with get, set

  let ofValue value =
    MarshalByRefValue(value)

module Environment =
  open System

  let commandLineArguments () =
    Environment.GetCommandLineArgs()
    |> Array.tail // SAFE: The first element is the path to the executable.

module AppDomain =
  open System
  open System.Threading

  type DisposableAppDomain(appDomain: AppDomain) =
    member this.Value = appDomain

    member this.Dispose() =
      AppDomain.Unload(appDomain)

    interface IDisposable with
      override this.Dispose() =
        this.Dispose()

  let create name =
    let appDomain = AppDomain.CreateDomain(name, null, AppDomain.CurrentDomain.SetupInformation)
    new DisposableAppDomain(appDomain)

  let run (f: unit -> 'x) (this: AppDomain) =
    let result = MarshalByRefObject.ofValue None
    this.DoCallBack
      (fun () ->
        result.Value <- f () |> Some
      )
    result.Value |> Option.get

  let runObservable (f: IObserver<'y> -> 'x) (this: AppDomain) =
    let gate = obj()
    let notifications = MarshalByRefObject.ofValue [||]
    let isCompleted = MarshalByRefObject.ofValue false
    let mutable subscribers = [||]
    let mutable index = 0
    let mutable timerOrNone = None
    let observer =
      { new IObserver<_> with
          override this.OnNext(x) =
            lock gate
              (fun () -> notifications.Value <- Array.append notifications.Value [| x |])
          override this.OnError(_) = ()
          override this.OnCompleted() =
            lock gate (fun () -> isCompleted.Value <- true)
      }
    let notify _ =
      lock gate
        (fun () ->
          while index < notifications.Value.Length do
            for observer in subscribers do
              (observer :> IObserver<_>).OnNext(notifications.Value.[index])
            index <- index + 1
          if isCompleted.Value && index = notifications.Value.Length then
            match timerOrNone with
            | Some timer ->
              (timer :> IDisposable).Dispose()
              timerOrNone <- None
              for observer in subscribers do
                (observer :> IObserver<_>).OnCompleted()
            | None -> ()
        )
    let result =
      this |> run (fun () -> f observer)
    let connectable =
      { new Observable.IConnectableObservable<'y> with
          override this.Subscribe(observer) =
            subscribers <- Array.append subscribers [| observer |]
            { new IDisposable with
                override this.Dispose() = ()
            }
          override this.Connect() =
            timerOrNone <-
              new Timer(notify, (), TimeSpan.Zero, TimeSpan.FromMilliseconds(17.0))
              |> Some
      }
    (result, connectable)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FileInfo =
  open System
  open System.IO
  open System.Reactive.Linq

  let subscribeChanged (threshold: TimeSpan) onChanged (file: FileInfo) =
    let watcher = new FileSystemWatcher(file.DirectoryName, file.Name)
    watcher.NotifyFilter <- NotifyFilters.LastWrite
    watcher.Changed
      .Select(ignore)
      .Throttle(threshold)
      .Add(onChanged)
    watcher.EnableRaisingEvents <- true
    watcher :> IDisposable

module FileSystemInfo =
  open System
  open System.IO

  let sandboxFile =
    new FileInfo(@"..\..\..\EnumerableTest.Sandbox\bin\Debug\EnumerableTest.Sandbox.dll")

  /// Enumerates ancestors in descending order.
  let ancestors =
    let rec loop (directory: DirectoryInfo) =
      seq {
        yield directory
        match directory.Parent with
        | null -> ()
        | parent ->
          yield! loop parent
      }
    fun fs ->
      match (fs: FileSystemInfo) with
      | :? DirectoryInfo as directory ->
        loop directory
      | :? FileInfo as file ->
        loop file.Directory
      | _ ->
        Seq.empty

  let findTestAssemblies (thisFile: FileInfo) =
    seq {
      for packagesDirectory in thisFile |> ancestors |> Seq.filter (fun d -> d.Name = "packages") do
      for solutionDirectory in packagesDirectory.Parent |> Option.ofObj |> Option.toArray do
      for suffix in [".UnitTest"; ".Tests"; ".UnitTests"] do
      for projectDirectory in solutionDirectory.GetDirectories(sprintf "*%s" suffix) do
      for binDirectory in projectDirectory.GetDirectories("bin") do
      for debugDirectory in binDirectory.GetDirectories("Debug") do
      for extension in [".dll"; ".exe"] do
      yield! debugDirectory.GetFiles(sprintf "*%s%s" suffix extension)
    }

  let getTestAssemblies (thisFile: FileInfo) =
    seq {
#if DEBUG
      yield sandboxFile
#endif
      yield! findTestAssemblies thisFile
    }

module ReactiveProperty =
  open System.Reactive.Linq
  open Reactive.Bindings

  let create x =
    new ReactiveProperty<_>(initialValue = x)

  let map (f: 'x -> 'y) (this: IReadOnlyReactiveProperty<'x>) =
    this.Select(f).ToReactiveProperty()

module ReactiveCommand =
  open System
  open Reactive.Bindings

  let create (canExecute: IReadOnlyReactiveProperty<_>) =
    new ReactiveCommand<_>(canExecuteSource = canExecute)

  let ofFunc (f: _ -> unit) canExecute =
    let it = create canExecute
    it.Subscribe(f) |> ignore<IDisposable>
    it

namespace EnumerableTest.Runner

open System.Collections.Generic

module Seq =
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

module Observable =
  open System
  open System.Threading
  open DotNetKit.Observing

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

  let wait observable =
    use event = new ManualResetEvent(initialState = false)
    observable
    |> subscribeEnd (fun _ -> event.Set() |> ignore<bool>)
    |> ignore<IDisposable>
    event.WaitOne() |> ignore<bool>

  /// Creates a connectable observable
  /// which executes async tasks when connected and notifies each result.
  let ofParallel asyncs =
    let gate = obj()
    let subject = Subject.Create()
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
    let subject = Subject.Create()
    let computations = computations |> Seq.toArray
    let connect () =
      let mutable count = 0
      for computation in computations do
        async {
          let! x = computation
          lock gate (fun () -> (subject :> IObserver<_>).OnNext(x))
          if Interlocked.Increment(&count) = computations.Length then
            (subject :> IObserver<_>).OnCompleted()
        } |> Async.Start
    { new IConnectableObservable<_> with
        override this.Connect() =
          connect ()
        override this.Subscribe(observer) =
          (subject :> IObservable<_>).Subscribe(observer)
    }

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
      for suffix in ["Test"; "UnitTest"; "Tests"; "UnitTests"] do
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
      yield!
        Environment.GetCommandLineArgs()
        |> Seq.filter (fun path -> path.EndsWith(".vshost.exe") |> not)
        |> Seq.map FileInfo
      yield! findTestAssemblies thisFile
    }

module MarshalByRefObject =
  open System

  type MarshalByRefValue<'x>(value: 'x) =
    inherit MarshalByRefObject()

    member val Value = value with get, set

  let ofValue value =
    MarshalByRefValue(value)

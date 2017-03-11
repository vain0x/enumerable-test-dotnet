namespace EnumerableTest.Runner

module MarshalByRefObject =
  open System

  type MarshalByRefValue<'x>(value: 'x) =
    inherit MarshalByRefObject()

    member val Value = value with get, set

  let ofValue value =
    MarshalByRefValue(value)

module AppDomain =
  open System
  open System.Reactive.Linq
  open System.Reactive.Subjects
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
      { new IConnectableObservable<'y> with
          override this.Subscribe(observer) =
            subscribers <- Array.append subscribers [| observer |]
            { new IDisposable with
                override this.Dispose() = ()
            }
          override this.Connect() =
            let timer =
              new Timer(notify, (), TimeSpan.Zero, TimeSpan.FromMilliseconds(17.0))
            timerOrNone <- Some timer
            timer :> IDisposable
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

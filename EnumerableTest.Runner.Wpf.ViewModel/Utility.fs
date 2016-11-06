﻿namespace EnumerableTest.Runner.Wpf

module Counter =
  let private counter = ref 0
  let generate () =
    counter |> incr
    !counter

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

module AppDomain =
  open System
  open System.Threading
  open EnumerableTest.Runner

  type DisposableAppDomain(appDomain: AppDomain) =
    member this.Value = appDomain

    interface IDisposable with
      override this.Dispose() =
        AppDomain.Unload(appDomain)

  let create name =
    let appDomain = AppDomain.CreateDomain(name, null, AppDomain.CurrentDomain.SetupInformation)
    new DisposableAppDomain(appDomain)

  type private MarshalByRefValue<'x>(value: 'x) =
    inherit MarshalByRefObject()

    member val Value = value with get, set

  let run (f: unit -> 'x) (this: AppDomain) =
    let result = MarshalByRefValue(None)
    this.DoCallBack
      (fun () ->
        result.Value <- f () |> Some
      )
    result.Value |> Option.get

  let runObservable (f: IObserver<'y> -> 'x) (this: AppDomain) =
    let gate = obj()
    let notifications = MarshalByRefValue([||])
    let isCompleted = MarshalByRefValue(false)
    let mutable subscribers = [||]
    let mutable index = 0
    let mutable timer = None
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
            for observer in subscribers do
              (observer :> IObserver<_>).OnCompleted()
            match timer with
            | Some timer -> (timer :> IDisposable).Dispose()
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
            timer <- new Timer(notify, (), TimeSpan.Zero, TimeSpan.FromMilliseconds(17.0)) |> Some
      }
    (result, connectable)

module SynchronizationContext =
  open System.Threading

  let send f (this: SynchronizationContext) =
    this.Send((fun _ -> f ()), ())

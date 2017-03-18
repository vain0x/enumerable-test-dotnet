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

  let invoke (f: unit -> unit) (domain: AppDomain) =
    domain.DoCallBack(fun () -> f ())

  let run (f: unit -> 'x) (this: AppDomain) =
    let result = MarshalByRefObject.ofValue None
    this.DoCallBack
      (fun () ->
        result.Value <- f () |> Some
      )
    result.Value |> Option.get

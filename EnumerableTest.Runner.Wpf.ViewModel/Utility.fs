namespace EnumerableTest.Runner.Wpf

module Counter =
  let private counter = ref 0
  let generate () =
    counter |> incr
    !counter

module AppDomain =
  open System

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

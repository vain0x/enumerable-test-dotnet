namespace EnumerableTest.Runner.Wpf

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

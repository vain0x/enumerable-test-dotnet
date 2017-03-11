namespace EnumerableTest.Runner

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

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Array =
  open System

  let decomposeLast (array: array<_>) =
    if array.Length = 0 then
      ArgumentException() |> raise
    else
      (array.[0..(array.Length - 2)], array.[array.Length - 1])

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
      Left:
        IReadOnlyList<'x>
      Intersect:
        IReadOnlyList<'k * 'x * 'y>
      Right:
        IReadOnlyList<'y>
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
      Left =
        left :> IReadOnlyList<_>
      Intersect =
        intersect
      Right =
        right
    }

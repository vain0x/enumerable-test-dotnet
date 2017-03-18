namespace EnumerableTest.Runner

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

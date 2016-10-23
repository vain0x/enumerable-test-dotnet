namespace EnumerableTest.Runner

module Seq =
  let indexed xs =
    xs |> Seq.mapi (fun i x -> (i, x))

module Result =
  open Basis.Core

  let catch f =
    try
      f () |> Success
    with
    | e -> Failure e

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

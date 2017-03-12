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

module Environment =
  open System

  let commandLineArguments () =
    Environment.GetCommandLineArgs()
    |> Array.tail // SAFE: The first element is the path to the executable.

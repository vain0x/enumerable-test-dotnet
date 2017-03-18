namespace EnumerableTest.Runner.Console

open System
open System.IO
open System.Reactive.Subjects
open System.Runtime.CompilerServices
open EnumerableTest.Runner.UI.Notifications

[<RequireQualifiedAccess>]
type Success =
  | Success

[<RequireQualifiedAccess>]
type Info =
  | Info

[<RequireQualifiedAccess>]
type Warning =
  | CouldNotLoadAssemblyFile
    of FileInfo * exn
with
  override this.ToString() =
    match this with
    | CouldNotLoadAssemblyFile (file, _) ->
      sprintf "Couldn't load an assembly '%s'." file.Name

type INotifier =
  INotifier<Success, Info, Warning>

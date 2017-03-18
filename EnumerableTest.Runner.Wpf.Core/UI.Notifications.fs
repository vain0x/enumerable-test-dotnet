namespace EnumerableTest.Runner.Wpf

open System
open System.IO
open EnumerableTest.Runner.UI.Notifications

[<RequireQualifiedAccess>]
type Success =
  | AssemblyExecutionCompleted
    of string
with
  override this.ToString() =
    match this with
    | AssemblyExecutionCompleted assemblyName ->
      sprintf "'%s' completed." assemblyName

[<RequireQualifiedAccess>]
type Info =
  | AssemblyLoaded
    of string
with
  override this.ToString() =
    match this with
    | AssemblyLoaded assemblyName ->
      sprintf "Loading '%s'..." assemblyName

[<RequireQualifiedAccess>]
type Warning =
  | AssemblyAborted
    of string
  | CouldNotLoadAssembly
    of string * exn
  | CouldNotFindChildNode
    of parentNodeName: string * childNodeName: string
  | NodeIsNotTestMethodNode
    of string
with
  override this.ToString() =
    match this with
    | CouldNotLoadAssembly (assemblyName, exn) ->
      sprintf "Couldn't load an assembly '%s'." assemblyName
    | AssemblyAborted assemblyName ->
      sprintf "Aborting '%s'..." assemblyName
    | CouldNotFindChildNode (parentName, childName) ->
      sprintf "Node '%s' doesn't have a child node named '%s'." parentName childName
    | NodeIsNotTestMethodNode nodeName ->
      sprintf "Node '%s' isn't a test method node." nodeName

type INotifier =
  INotifier<Success, Info, Warning>

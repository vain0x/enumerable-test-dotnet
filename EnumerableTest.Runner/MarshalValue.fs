namespace EnumerableTest.Runner

open System
open System.Collections.Generic
open Basis.Core

type MarshalResult =
  | MarshalResult
    of Result<MarshalValue, exn>
with
  member this.AsObject =
    let (MarshalResult result) = this
    result |> Result.toObj

and MarshalProperty =
  KeyValuePair<string, MarshalResult>

and
  [<Serializable>]
  MarshalValue =
  {
    TypeName:
      string
    String:
      string
    Properties:
      array<MarshalProperty>
  }
with
  override this.ToString() =
    this.String

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MarshalValue =
  let mutable Recursion = 0

  let ofNull =
    {
      TypeName =
        "null"
      String =
        "null"
      Properties =
        [||]
    }

  let ofObj =
    function
    | null ->
      ofNull
    | value ->
      {
        TypeName =
          value.GetType().Name
        String =
          value.ToString()
        Properties =
          [||] // TODO:
      }

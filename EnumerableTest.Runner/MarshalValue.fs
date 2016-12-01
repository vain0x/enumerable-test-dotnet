﻿namespace EnumerableTest.Runner

open System
open System.Collections
open System.Collections.Generic
open System.Reflection
open Basis.Core

type MarshalResult =
  | MarshalResult
    of Result<MarshalValue, exn>
with
  member this.Unwrap() =
    let (MarshalResult value) = this
    value

  member this.AsObject =
    this.Unwrap() |> Result.toObj

  member this.ValueOrThrow =
    this.Unwrap() |> Result.get

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

  type private Factory =
    {
      Recursion:
        int
      Function:
        int -> obj -> MarshalValue
      Invoke:
        obj -> MarshalValue
    }
  with
    static member Create(recursion, f) =
      {
        Recursion = recursion
        Function = f
        Invoke = f recursion
      }

  let private create (factory: Factory) typ string properties =
    {
      TypeName =
        typ |> Type.prettyName
      String =
        string
      Properties =
        if factory.Recursion < 0
        then [||]
        else properties |> Seq.toArray
    }

  let private getPublicInstancePropertyValues source =
    seq {
      let bindingFlags = BindingFlags.Instance ||| BindingFlags.Public
      for propertyInfo in source.GetType().GetProperties(bindingFlags) do
        let getter = propertyInfo.GetMethod
        if getter |> isNull |> not
          && propertyInfo.GetIndexParameters() |> Array.isEmpty
        then
          let result =
            Result.catch (fun () -> getter.Invoke(source, [||]))
            |> Result.mapFailure
              (function
                | :? TargetInvocationException as e -> e.InnerException
                | e -> e
              )
          yield (propertyInfo, result)
    }

  let private publicProperties (factory: Factory) source =
    seq {
      for (propertyInfo, result) in source |> getPublicInstancePropertyValues do
        let result = result |> Result.map factory.Invoke
        yield KeyValuePair<_, _>(propertyInfo.Name, MarshalResult result)
    }

  let private ofCollection (factory: Factory) source =
    let typ = source.GetType()
    let collection = source |> Seq.cast
    let count = collection |> Seq.length
    let items =
      seq {
        for (i, x) in collection |> Seq.indexed do
          yield (i, x |> factory.Invoke)
      } |> Seq.cache
    let string =
      if count <= 10 then
         items
         |> Seq.map (fun (_, x) -> x.String)
         |> String.concat ", "
         |> sprintf "{%s}"
        else
          sprintf "{Count = %d}" count
    let properties =
      seq {
        yield! publicProperties factory source
        for (i, x) in items do
          let key = sprintf "[%d]" i 
          let value = x |> Success |> MarshalResult
          yield KeyValuePair<_, _>(key, value)
      }
    create factory typ string properties

  let private ofProperties factory (value: obj) =
    let typ = value.GetType()
    let properties = publicProperties factory value
    create factory typ (value |> string) properties

  let rec internal ofObjCore recursion value =
    let factory =
      Factory.Create(recursion - 1, ofObjCore)
    match value with
    | null ->
      ofNull
    | value when value.GetType() |> Type.isCollectionType ->
      value :?> IEnumerable |> ofCollection factory
    | value ->
      value |> ofProperties factory

  let rec ofObj value =
    value |> ofObjCore Recursion

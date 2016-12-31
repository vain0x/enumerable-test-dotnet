namespace EnumerableTest.Runner.UnitTest

open System.Collections.Generic
open Basis.Core
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner

module MarshalValueTest =
  module test_ofObj =
    let nullCase =
      test {
        do! MarshalValue.ofObjCore 1 null |> assertEquals MarshalValue.ofNull
      }

    type SinglePropertyClass() =
      member this.PublicProperty = "PropertyValue"

      member private this.PrivateProperty = 0

      member this.WriteOnlyProperty
        with set value = ()

      member this.IndexProperty
        with get i = 0

      static member StaticProperty = 0

    let classCase =
      test {
        let it = SinglePropertyClass() |> MarshalValue.ofObjCore 1
        do! it.TypeName |> assertEquals "SinglePropertyClass"
        do! it.Properties |> assertSatisfies (Seq.length >> (=) 1)
        let kv = it.Properties.[0]
        do! kv.Key |> assertEquals "PublicProperty"
        do! kv.Value.ValueOrThrow.String |> assertEquals "PropertyValue"
      }

    type ThrowingPropertyClass() =
      let ``exception`` = exn()

      member this.Exception() =
        ``exception``

      member this.ThrowingProperty =
        this.Exception() |> raise

    let ``catches exceptions thrown by getters`` =
      test {
        let instance = ThrowingPropertyClass()
        let it = instance |> MarshalValue.ofObjCore 1
        do! it.Properties |> assertSatisfies (Seq.length >> (=) 1)
        let result = it.Properties.[0].Value.Unwrap()
        do! result |> assertEquals (instance.Exception() |> Failure)
      }

    let ``test String`` =
      let body (value: obj, expected) =
        test {
          do! (value |> MarshalValue.ofObjCore 1).String |> assertEquals expected
        }
      parameterize {
        case ([||] :> obj, "{}")
        case ([| 0..9 |] :> obj, "{0, 1, 2, 3, 4, 5, 6, 7, 8, 9}")
        case ([| 0..10 |] :> obj, "{Count = 11}")
        case (Seq.empty :> obj, Seq.empty |> string)
        case ("hello" :> obj, "hello")
        run body
      }

  let ``test Item property`` =
    test {
      let instance = ResizeArray([0; 1; 2])
      let it = instance |> MarshalValue.ofObjCore 1
      do!
        it.Properties
        |> Array.map (fun p -> (p.Key, p.Value.ValueOrThrow.String))
        |> assertEquals
          [|
            ("Capacity", instance.Capacity |> string)
            ("Count", instance.Count |> string)
            ("[0]", "0")
            ("[1]", "1")
            ("[2]", "2")
          |]
    }

  let ``test keyed collection`` =
    test {
      let instance =
        [("a", 0); ("b", 1); ("c", 2)] |> Dictionary.ofSeq
      let it = instance |> MarshalValue.ofObjCore 1
      do!
        it.Properties
        |> Array.map (fun (KeyValue (key, value)) -> (key, value.ValueOrThrow.String))
        |> assertEquals
          [|
            ("Comparer", instance.Comparer |> string)
            ("Count", instance.Count |> string)
            ("Keys", "{a, b, c}")
            ("Values", "{0, 1, 2}")
            ("[a]", "0")
            ("[b]", "1")
            ("[c]", "2")
          |]
    }

  let ``test recursion`` =
    test {
      let q =
        query {
          let instance = ResizeArray([[||]; [| [| 0 |]; [| 1 |]; [| 2 |] |]])
          let it = instance |> MarshalValue.ofObjCore 2
          for KeyValue (k, v) in it.Properties do
          where (k = "[1]")
          for KeyValue (k, v) in v.ValueOrThrow.Properties do
          where (k = "[0]")
          select v.ValueOrThrow.Properties
        }
        |> Seq.toList
      do! q |> assertSatisfies (Seq.forall Seq.isEmpty)
    }

  let ``test ofObjFlat`` =
    test {
      let it = [|"x"; "y"|] |> MarshalValue.ofObjFlat
      do! it.String |> assertEquals "{x, y}"
      do! it.Properties |> assertSatisfies Array.isEmpty
    }

namespace EnumerableTest.Runner

open System
open System.Collections.Generic
open EnumerableTest
open EnumerableTest.Sdk

type SerializableAssertion =
  {
    IsPassed:
      bool
    Message:
      option<string>
    Data:
      array<KeyValuePair<string, MarshalValue>>
  }
with
  member this.MessageOrNull =
    this.Message |> Option.toObj

[<Serializable>]
[<AbstractClass>]
type SerializableTest(name) =
  abstract IsPassed: bool

  member this.Name: string = name

[<Serializable>]
[<Sealed>]
type SerializableAssertionTest(name, assertion: SerializableAssertion) =
  inherit SerializableTest(name)

  member this.Assertion =
    assertion

  override this.IsPassed =
    assertion.IsPassed

[<Serializable>]
[<Sealed>]
type SerializableGroupTest(name, tests, error) =
  inherit SerializableTest(name)

  member this.Tests =
    (tests: array<SerializableTest>)

  member this.Exception =
    (error: option<exn>)

  member this.ExceptionOrNull =
    this.Exception |> Option.toObj

  override val IsPassed =
    tests |> Array.forall (fun test -> (test: SerializableTest).IsPassed)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SerializableAssertion =
  let ofAssertion (assertion: Assertion) =
    {
      IsPassed =
        assertion.IsPassed
      Message =
        assertion.MessageOrNull |> Option.ofObj
      Data =
        [|
          for KeyValue (key, value) in assertion.Data do
            let marshalValue =
              if assertion.IsPassed
              then MarshalValue.ofObjFlat value
              else MarshalValue.ofObj value
            yield KeyValuePair<_, _>(key, marshalValue)
        |]
    }

[<AutoOpen>]
module SerializableTestExtension =
  let (|AssertionTest|GroupTest|) (test: SerializableTest) =
    match test with
    | :? SerializableAssertionTest as test ->
      AssertionTest test
    | :? SerializableGroupTest as test ->
      GroupTest test
    | _ ->
      ArgumentException() |> raise

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SerializableTest =
  let rec ofAssertionTest (test: AssertionTest) =
    let assertion = test.Assertion |> SerializableAssertion.ofAssertion
    SerializableAssertionTest(test.Name, assertion)

  let rec ofGroupTest (test: GroupTest) =
    let tests = test.Tests |> Array.map ofTest
    let e = test.ExceptionOrNull |> Option.ofObj
    SerializableGroupTest(test.Name, tests, e)

  and ofTest (test: Test): SerializableTest =
    match test with
    | :? AssertionTest as test ->
      test |> ofAssertionTest :> _
    | :? GroupTest as test ->
      test |> ofGroupTest :> _
    | _ ->
      ArgumentException() |> raise

  let rec assertions =
    function
    | AssertionTest test ->
      test.Assertion |> Seq.singleton
    | GroupTest test ->
      test.Tests |> Seq.collect assertions

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
        assertion.Data |> Seq.toArray
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SerializableTest =
  let rec ofTest (test: Test): SerializableTest =
    match test with
    | :? AssertionTest as test ->
      let assertion = test.Assertion |> SerializableAssertion.ofAssertion
      SerializableAssertionTest(test.Name, assertion) :> _
    | :? GroupTest as test ->
      let tests = test.Tests |> Array.map ofTest
      let e = test.ExceptionOrNull |> Option.ofObj
      SerializableGroupTest(test.Name, tests, e) :> _
    | _ ->
      ArgumentException() |> raise

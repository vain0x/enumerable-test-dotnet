namespace EnumerableTest.Runner

open System
open System.Collections
open System.Collections.Generic
open EnumerableTest.Sdk

[<Serializable>]
[<AbstractClass>]
type SerializableTestData internal () =
  do ()

[<Serializable>]
[<Sealed>]
type SerializableEmptyTestData() =
  inherit SerializableTestData()

  static member val Empty =
    SerializableEmptyTestData()

[<Serializable>]
[<Sealed>]
type SerializableDictionaryTestData(items: array<KeyValuePair<string, MarshalValue>>) =
  inherit SerializableTestData()

  member this.GetEnumerator() =
    (items |> Array.toSeq).GetEnumerator()

  interface IEnumerable<KeyValuePair<string, MarshalValue>> with
    override this.GetEnumerator() =
      this.GetEnumerator()

    override this.GetEnumerator() =
      this.GetEnumerator() :> IEnumerator

[<AutoOpen>]
module SerializableTestDataExtension =
  let (|EmptyTestData|DictionaryTestData|) (testData: SerializableTestData) =
    match testData with
    | :? SerializableEmptyTestData ->
      EmptyTestData
    | :? SerializableDictionaryTestData as testData ->
      DictionaryTestData testData
    | _ ->
      ArgumentException() |> raise

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]  
module SerializableTestData =
  let ofTestData isFlat: TestData -> SerializableTestData =
    function
    | :? EmptyTestData ->
      SerializableEmptyTestData.Empty :> SerializableTestData
    | :? DictionaryTestData as testData ->
      let pairs =
        [|
          for KeyValue (key, value) in testData do
            let value = MarshalValue.ofObj isFlat value
            yield KeyValuePair<_, _>(key, value)
        |]
      SerializableDictionaryTestData(pairs) :> SerializableTestData
    | _ ->
      ArgumentException() |> raise

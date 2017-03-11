namespace EnumerableTest.Runner
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection

module TypeTest =
  open System
  open System.Collections
  open System.Collections.Generic

  module ``test FullName`` =
    let ``test decompose`` =
      test {
        let assert' expected fullName =
          Type.FullName.Create(fullName) |> Type.FullName.decompose
          |> assertEquals expected
        do!
          "globalType"
          |> assert' ([||], [||], "globalType")
        do!
          "Foo.nonnestedType"
          |> assert' ([|"Foo"|], [||], "nonnestedType")
        do!
          "Foo.Collection+Enumerator"
          |> assert' ([|"Foo"|], [|"Collection"|], "Enumerator")
      }

  let ``test isCollectionType true`` =
    let body (typ, expected) =
      test {
        do! typ |> assertSatisfies (Type.isCollectionType >> (=) expected)
      }
    parameterize {
      case (typeof<ICollection>, true)
      case (typeof<ICollection<int>>, true)
      case (typeof<IReadOnlyList<int>>, true)
      case (typeof<IList>, true)
      case (typeof<ResizeArray<int>>, true)
      case (typeof<IDictionary<int, string>>, true)
      case (typeof<array<int>>, true)
      case (typeof<IEnumerable>, false)
      case (typeof<IEnumerable<int>>, false)
      case (typeof<option<array<int>>>, false)
      run body
    }

  let ``test isKeyValuePairType`` =
    test {
      do! typeof<int> |> assertSatisfies (Type.isKeyValuePairType >> not)
      do! typeof<KeyValuePair<int, int>> |> assertSatisfies Type.isKeyValuePairType
    }

  let ``test tryMatchKeyedCollectionType`` =
    let body (typ, expected) =
      test {
        do! typ |> Type.tryMatchKeyedCollectionType |> assertEquals expected
      }
    parameterize {
      case
        ( typeof<IReadOnlyCollection<KeyValuePair<string, int>>>
        , Some (KeyValuePair(typeof<string>, typeof<int>))
        )
      case
        ( typeof<ICollection<KeyValuePair<string, int>>>
        , Some (KeyValuePair(typeof<string>, typeof<int>))
        )
      case
        ( typeof<Dictionary<string, int>>
        , Some (KeyValuePair(typeof<string>, typeof<int>))
        )
      case (typeof<ICollection>, None)
      case (typeof<IEnumerable<KeyValuePair<string, int>>>, None)
      run body
    }

  let ``test prettyName`` =
    let body (typ, expected) =
      test {
        do! typ |> Type.prettyName |> assertEquals expected
      }
    parameterize {
      case (typeof<int>, "int")
      case (typeof<string>, "string")
      case (typeof<byte>, "Byte")
      case (typeof<Tuple<int, string>>, "Tuple<int, string>")
      case (typeof<Dictionary<int, Tuple<int, string>>>, "Dictionary<int, Tuple<int, string>>")
      run body
    }

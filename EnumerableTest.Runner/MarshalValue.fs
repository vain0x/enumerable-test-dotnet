namespace EnumerableTest.Runner

open System
open System.Collections.Generic

type
  [<Serializable>]
  [<AbstractClass>]
  MarshalResult() =
  abstract Match<'x> : (MarshalValue -> 'x) * (exn -> 'x) -> 'x

and
  [<Serializable>]
  [<Sealed>]
  MarshalResultValue(marshalValue) =
  inherit MarshalResult()

  member this.MarshalValue = marshalValue

  override this.Match(onValue, _) =
    onValue this.MarshalValue

and
  [<Serializable>]
  [<Sealed>]
  MarshalResultException(``exception``) =
  inherit MarshalResult()

  member this.Exception = ``exception``

  override this.Match(_, onException) =
    onException this.Exception

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

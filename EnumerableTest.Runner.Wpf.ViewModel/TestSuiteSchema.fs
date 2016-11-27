namespace EnumerableTest.Runner.Wpf

open System.Collections.Generic
open EnumerableTest.Runner

type TestClassSchemaDifference =
  {
    Added:
      IReadOnlyList<TestMethodSchema>
    Removed:
      IReadOnlyList<TestMethodSchema>
    Modified:
      Map<string, TestMethodSchema>
  }
with
  static member Create(added, removed, modified) =
    {
      Added =
        added
      Removed =
        removed
      Modified =
        modified
    }

type TestSuiteSchemaDifference =
  {
    Added:
      IReadOnlyList<TestClassSchema>
    Removed:
      IReadOnlyList<TestClassSchema>
    Modified:
      Map<string, TestClassSchemaDifference>
  }
with
  static member Create(added, removed, modified) =
    {
      Added =
        added
      Removed =
        removed
      Modified =
        modified
    }

module TestClassSchema =
  let difference oldOne newOne =
    let d =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestMethodSchema).MethodName)
        (fun node -> (node: TestMethodSchema).MethodName)
        (oldOne: TestClassSchema).Methods
        (newOne: TestClassSchema).Methods
    let modified =
      d.Intersect |> Seq.map
        (fun (name, _, testMethodSchema) ->
          (name, testMethodSchema)
        )
      |> Map.ofSeq
    TestClassSchemaDifference.Create
      ( d.Right
      , d.Left
      , modified
      )

module TestSuiteSchema =
  let difference oldOne newOne =
    let d =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestClassSchema).TypeFullName)
        (fun node -> (node: TestClassSchema).TypeFullName)
        (oldOne: TestSuiteSchema)
        (newOne: TestSuiteSchema)
    let modified =
      d.Intersect |> Seq.map
        (fun (name, l, r) ->
          (name, TestClassSchema.difference l r)
        )
      |> Map.ofSeq
    TestSuiteSchemaDifference.Create
      ( d.Right
      , d.Left
      , modified
      )

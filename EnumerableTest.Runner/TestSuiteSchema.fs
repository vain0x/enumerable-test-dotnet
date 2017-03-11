namespace EnumerableTest.Runner

open System
open System.Reflection
open Basis.Core
open EnumerableTest

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassType =
  let testMethodInfos (typ: Type) =
    typ.GetMethods(BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic)
    |> Seq.filter
      (fun m ->
        not m.IsSpecialName
        && not m.IsGenericMethodDefinition
        && m.ReturnType = typeof<seq<Test>>
        && (m.GetParameters() |> Array.isEmpty)
      )

  let isTestClass (typ: Type) =
    typ.GetConstructor([||]) |> isNull |> not
    && typ |> testMethodInfos |> Seq.isEmpty |> not

  let instantiate (typ: Type): unit -> TestInstance =
    let defaultConstructor =
      typ.GetConstructor([||])
    fun () -> defaultConstructor.Invoke([||])

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestMethodSchema =
  let ofMethodInfo (m: MethodInfo): TestMethodSchema =
    {
      MethodName = m.Name
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassPath =
  let ofFullName (fullName: Type.FullName) =
    let (namespacePath, typePath, name) =
      fullName |> Type.FullName.decompose
    {
      NamespacePath =
        namespacePath
      ClassPath =
        typePath
      Name =
        name
    }

  let ofType (typ: Type) =
    typ |> Type.fullName |> ofFullName

  let fullPath (this: TestClassPath) =
    [
      yield! this.NamespacePath
      yield! this.ClassPath
      yield this.Name
    ]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestClassSchema =
  let ofType (typ: Type): TestClassSchema =
    {
      Path =
        typ |> TestClassPath.ofType
      TypeFullName =
        typ |> Type.fullName
      Methods = 
        typ
        |> TestClassType.testMethodInfos
        |> Seq.map TestMethodSchema.ofMethodInfo
        |> Seq.toArray
    }

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

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]  
module TestSuiteSchema =
  let empty: TestSuiteSchema = [||]

  let ofTypes types: TestSuiteSchema =
    types
    |> Seq.filter TestClassType.isTestClass
    |> Seq.map TestClassSchema.ofType
    |> Seq.toArray

  let ofAssembly (assembly: Assembly) =
    assembly.GetTypes() |> ofTypes

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
          (l.Path |> TestClassPath.fullPath, TestClassSchema.difference l r)
        )
      |> Map.ofSeq
    TestSuiteSchemaDifference.Create
      ( d.Right
      , d.Left
      , modified
      )

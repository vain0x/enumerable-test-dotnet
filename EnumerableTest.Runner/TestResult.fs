namespace EnumerableTest.Runner

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestResult =
  let create typ result: TestResult =
    {
      TypeFullName =
        typ |> Type.fullName
      Result =
        result
    }

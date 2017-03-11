namespace EnumerableTest.Runner

open EnumerableTest.Sdk

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AssertionCount =
  let create totalCount violatedCount errorCount notCompletedCount =
    {
      TotalCount =
        totalCount
      ViolatedCount =
        violatedCount
      ErrorCount =
        errorCount
      NotCompletedCount =
        notCompletedCount
    }

  let zero =
    create 0 0 0 0

  let onePassed =
    create 1 0 0 0

  let oneViolated =
    create 1 1 0 0

  let oneError =
    create 1 0 1 0

  let ofNotCompleted n =
    create n 0 0 n

  let add (l: AssertionCount) (r: AssertionCount) =
    {
      TotalCount =
        l.TotalCount + r.TotalCount
      ViolatedCount =
        l.ViolatedCount + r.ViolatedCount
      ErrorCount =
        l.ErrorCount + r.ErrorCount
      NotCompletedCount =
        l.NotCompletedCount + r.NotCompletedCount
    }

  let subtract (l: AssertionCount) (r: AssertionCount) =
    {
      TotalCount =
        l.TotalCount - r.TotalCount
      ViolatedCount =
        l.ViolatedCount - r.ViolatedCount
      ErrorCount =
        l.ErrorCount - r.ErrorCount
      NotCompletedCount =
        l.NotCompletedCount - r.NotCompletedCount
    }

  let groupSig =
    { new GroupSig<_>() with
        override this.Unit = zero
        override this.Multiply(l, r) = add l r
        override this.Divide(l, r) = subtract l r
    }

  let ofAssertionTest (test: SerializableAssertionTest) =
    if test.IsPassed
      then onePassed
      else oneViolated

  let ofGroupTest (groupTest: SerializableGroupTest) =
    seq {
      for assertionTest in groupTest |> SerializableTest.assertions do
        yield assertionTest |> ofAssertionTest
      if groupTest.Exception |> Option.isSome then
        yield oneError
    }
    |> Seq.fold add zero

  let ofTestMethod (testMethod: TestMethod) =
    testMethod.Result |> ofGroupTest
    |> add
      (match testMethod.DisposingError with
      | Some _ -> oneError
      | None -> zero
      )

  let isPassed (count: AssertionCount) =
    count.ViolatedCount = 0 && count.ErrorCount = 0 && count.NotCompletedCount = 0

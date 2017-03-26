namespace EnumerableTest.Runner

[<AbstractClass>]
type GroupSig<'x>() =
  abstract Unit: 'x
  abstract Multiply: 'x * 'x -> 'x
  abstract Divide: 'x * 'x -> 'x

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GroupSig =
  let ofInt32 =
    { new GroupSig<int>() with
        override this.Unit = 0
        override this.Multiply(l, r) = l + r
        override this.Divide(l, r) = l - r
    }

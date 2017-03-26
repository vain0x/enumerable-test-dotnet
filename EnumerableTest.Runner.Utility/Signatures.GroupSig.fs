namespace EnumerableTest.Runner

[<AbstractClass>]
type GroupSig<'x>() =
  abstract Unit: 'x
  abstract Multiply: 'x * 'x -> 'x
  abstract Divide: 'x * 'x -> 'x

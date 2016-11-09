namespace EnumerableTest.Runner

[<AbstractClass>]
type GroupSig<'x>() =
  abstract member Unit: 'x
  abstract member Multiply: 'x * 'x -> 'x
  abstract member Divide: 'x * 'x -> 'x

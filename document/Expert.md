# Expert documentation (上級者向けの解説)
### Set up/Tear down
If you want to do something before/after each test method (so-called set up and tear down), use the constructor and ``IDisposable.Dispose`` method. **EnumerableTest** instantiates a test class for each test method it has.

テストメソッドの実行前後に何かを行いたい場合 (いわゆる set up と tear down)、コンストラクターと Dispose を使います。EnumerableTest は1つのテストメソッドごとに1個のインスタンスを作成するわけです。

```csharp
public class MyTest
    : IDisposable  // IMPORTANT!
{
    public MyTest()
    {
        // set up
    }

    public void Dispose()
    {
         // tear down
    }

    // test methods...
}
```

# Tutorial -Deep Dive-
## Assembly auto detection (アセンブリーの自動検出)
If your solution directory is formed as below, you don't need to pass arguments to the runner because it can detect your test assemblies automatically.

ソリューションディレクトリーが以下の構造になっている場合、テストアセンブリーのファイルパスは自動で検出されるため、ランナー (EnumerableTest.Runner.Wpf) に引数を渡す必要はありません。

```
+ X (solution directory)
    + packages
        ... - EnumerableTest.Runner.Wpf.exe
    + X.Y.UnitTest (project directory)
        + bin
            + Debug
                - X.Y.UnitTest.dll (test assembly)
```

## Set up/Tear down
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

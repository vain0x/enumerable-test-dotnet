# EnumerableTest
[![NuGet version](https://badge.fury.io/nu/EnumerableTest.Core.svg)](https://badge.fury.io/nu/EnumerableTest.Core)

A unit testing framework for .NET framework.

.NET フレームワーク向けの単体テストフレームワーク。

![A screen shot of EnumerableTest.Runner.Wpf](documents/images/EnumerableTest.Runner.Wpf.Screenshot.png)

## Documents
- [Tutorial (チュートリアル)](documents/Tutorial.md)
- [Expert (上級者向けの解説)](documents/Expert.md)

### Why use **EnumerableTest**?
- You can write **parameterized tests** easily because of two functionalities:
    - **Continuous assertions**
        - Even if an assertion is violated, the rest assertions are also evaluated. This prevents you from being suffered from test methods with many assertions.
        - (ja) いずれかの表明が不成立になったとしても、残りの表明は評価されます。そのため、1つのテストメソッドのなかで多数の表明を実行しても、問題ありません。
    - **Test groups**
        - Tests (assertions) can be grouped into a test.
        - (ja) 複数のテスト (アサーション) をまとめて1つのテストとして扱うことができます。
- **Barrier-free colors**
    - Blue for the successful status, warm colors for bad statuses.
    - (ja) 成功を青で、失敗を暖色で表します。

## License
[MIT License](LICENSE.md)

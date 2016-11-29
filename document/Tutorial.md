# Tutorial (チュートリアル)
## 1. Create a Test Project (テストプロジェクトの作成)
Before installing **EnumerableTest**, we recommend to add a project for unit testing to your solution.

**EnumerableTest** をインストールする前に、単体テスト専用のプロジェクトを作成することをおすすめします。

Open your project (solution) with Visual Studio and select the File menu > Create New > Project and add a class library project named "X.UnitTest" (where X is your project name) to your solution. Note that you need to choose "Add to solution" (rather than "Create new solution") in a combobox.

Visual Studio でソリューションを開き、「ファイル」メニュー→「新規作成」→「プロジェクト」を選択し、クラスライブラリーを作成します。名前は「X.UnitTest」(X はプロジェクトの名前)としておくのが通例のようです。ダイアログにあるコンボボックスのうち、「新しいソリューションを作成」と表示されているところを、「ソリューションに追加」に変更しておく必要があります。

## 2. Install (インストール)
To install **EnumerableTest** to your projects, it's easy to use NuGet the package manager. See [how to find and install a package with NuGet](https://docs.nuget.org/ndocs/tools/package-manager-ui#finding-and-installing-a-package). You should find and install "EnumerableTest.Core" and "EnumerableTest.Runner.Wpf" to the Test project created above.

**EnumerableTest** をインストールするのには、NuGet パッケージマネージャーを使用すると簡単です。Visual Studio の「ツール」メニュー→「NuGetパッケージマネージャー」→「ソリューションの NuGet パッケージの管理」を選択すると、新しいタブが開きます (※バージョンによってはダイアログが開きます)。検索欄に "EnumerableTest" と入力するとパッケージが表示されますので、 "EnumerableTest.Core" と "EnumerableTest.Runner.Wpf" という名前のパッケージを、さきほど作成したテスト用プロジェクトにインストールします。

Then open a "test explorer" which shows results of unit tests. Build your solution with Visual Studio and find two files:

次に、単体テストの結果を表示するための「テストエクスプローラー」を実行しましょう。Visual Studio でプロジェクトをビルドして、次の2つのファイルをみつけてください:

- (A) ``X.UnitTest/bin/Debug/X.UnitTest.dll``
- (B) ``packages/EnumerableTest.Runner.Wpf/tools/EnumerableTest.Runner.Wpf.exe``

Drag (A) and drop to (B). You will see a window opens and displays nothing. Keep it open and continue to read.

(A) をドラッグして、(B) にドロップすると、真っ白のウィンドウが表示されるはずです。これを開けたまま、続きをお読みください。

## 3. Overview (概要)
To say it simply, to use **EnumerableTest**, all you need to do is to define *test methods* in which invokes assertion methods like other unit testing frameworks. Assertion methods provided by **EnumerableTest** (for example, `Is` which asserts two values are equal) returns a value of `Test` class and your test methods shall ``yield return`` them.

**EnumerableTest** の基本的な使い方は、他の単体フレームワークと同様に、テストメソッド (表明メソッドを書き並べたメソッド) を定義するだけです。**EnumerableTest** が提供する表明メソッド (例えば2つの値が等しいことを表明する `Is` など) は `Test` 型の値を返しますので、テストメソッドではこれらを ``yield return`` していきます。

### Successful tests (成功するテスト)
As the simplest example, let's test the ``++`` operator increments a variable. Add a .cs file to X.UnitTest project and copy-and-paste the following:

最も単純な例として、「変数をインクリメントすると値が1増える」ことをテストしてみましょう。X.UnitTest プロジェクトに次の内容のソースコードを追加します:

```csharp
using System.Collections.Generic;
using EnumerableTest;

// Define a "test class" in which contains test methods as a public class.
// テストメソッドを含むクラスを public class として宣言します。
public class OperatorTest
{
    // Define test methods as a public method.
    // Return type of them must be ``IEnumerable<Test>``.
    // テストメソッドを public メソッドとして宣言します。
    // 返り値の型は IEnumerable<Test> でなければなりません。
    public IEnumerable<Test> TestIncrement()
    {
        var n = 0;

        // Assert that n == 0.
        // n が 0 に等しいことを表明します。
        yield return n.Is(0);

        n++;

        // And assert that n == 1 here.
        // ここでは n が 1 に等しいことを表明します。
        yield return n.Is(1);
    }
}
```

And build the Test project. The test explorer automatically updates the content and shows that the Test is passed.

テストプロジェクトをビルドすると、テストエクスプローラーは自動的に更新され、1つのテストが通過(成功)したことを表示します。

### Violation (表明違反)
Next we show an example of a test which doesn't pass. Add the following method to the OperatorTest class.

```csharp
    public IEnumerable<Test> TestDecrement_Violated()
    {
        var n = 0;

        // Although this assertion is violated, the execution continues.
        // この表明は失敗するが、実行は継続される。
        yield return n.Is(1);

        n--;

        // This assertion succeeds.
        // この表明は成功する。
        yield return n.Is(-1);
    }
```

The first assertion (``n == 1``) "is violated" and the second assertion (``n == -1``) is passed. The result of this test method will be "violated" because one of assertions is violated.

``n == 1`` を表す1つ目の表明は「失敗」しますが、実行は継続され、2つ目の ``n == -1`` を表す表明が成功することを確認できます。少なくとも1つの表明が失敗しているため、テストメソッドの結果は「失敗」(表明違反)になります。

### Assertion methods (表明メソッド)
**EnumerableTest** provides three assertion methods for usual use and two for uncommon cases. Because we believe that the former three covers 99.9% of assertions, we explain only them here. The first one is `Is`, which has been already metioned above.

**EnumerableTest** が提供する表明メソッドには、普段使用する3つのメソッドと、特殊な用途に用いる2つのメソッドを提供しています。99.9% の表明は前者でまかなえるはずですので、ここではそれらだけ説明しましょう。1つは、さきほど紹介した `Is` です。

The second is `TestSatisfy`, which is a generalized method of `Is`. It allows you to test *any* property of a value. The following code shows a test method that tests an array isn't empty.

2つ目の `TestSatisfy` は、`Is` を一般化したものです。これは、値の任意の性質をテストするのに使用できます。次のコードは、「配列の長さがゼロでない」ことをテストするテストメソッドです。

```csharp
    public int[] MakeArray()
    {
        return new[] { 0, 1, 2 };
    }

    public IEnumerable<Test> test_MakeArray_returns_a_nonempty_array()
    {
        yield return MakeArray().TestSatisfy(a => a.Length != 0);
    }
```

The third is ``Test.Catch``, which tries to catch an exception. You can use this to test that a method rejects invalid arguments or something. For example, let's test that the array indexer rejects an invalid index.

3つ目は、例外を捕捉する ``Test.Catch`` です。例えば、メソッドが異常な引数を拒絶することをテストするのに使えます。次のコードは、「配列のインデックスの範囲外にアクセスすると例外が送出される」ことをテストするテストメソッドです。

```csharp
    public IEnumerable<Test> test_array_indexer_rejects_invalid_index()
    {
        var array = new[] { 0, 1, 2 };
        Test.Catch(() =>
        {
            return array[3];
        });
    }
```

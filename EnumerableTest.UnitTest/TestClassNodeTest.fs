namespace EnumerableTest.UnitTest

open System.Threading
open Persimmon
open Persimmon.Syntax.UseTestNameByReflection
open EnumerableTest.Runner
open EnumerableTest.Runner.Wpf

module TestClassNodeTest =
  type TestClass1() =
    member this.PassingTest() =
      TestClass.passingTest

    member this.ViolatingTest() =
      TestClass.violatingTest

    member this.ThrowingTest() =
      TestClass.throwingTest

  type TestClass1Updated() =
    // Unchanged.
    member this.ViolatingTest() =
      TestClass.violatingTest

    // Fixed.
    member this.ThrowingTest() =
      TestClass.passingTest

    // Added.
    member this.NewPassingTest() =
      TestClass.passingTest

  let ``test initial state`` =
    test {
      let node = TestClassNode(typeof<TestClass1>.FullName)
      do! node.Children |> assertSatisfies Seq.isEmpty
      do! node.TestStatus.Value |> assertEquals TestStatus.Passed
    }

  let ``test UpdateSchema`` =
    test {
      let node = TestClassNode(typeof<TestClass1>.FullName)
      let schema = TestClassSchema.ofType typeof<TestClass1>
      node.UpdateSchema(schema)
      do! node.Children
          |> Seq.map (fun ch -> ch.Name)
          |> set
          |> assertEquals (set ["PassingTest"; "ViolatingTest"; "ThrowingTest"])

      let updatedSchema =
        { TestClassSchema.ofType typeof<TestClass1Updated> with
            TypeFullName = schema.TypeFullName
        }
      node.UpdateSchema(updatedSchema)
      do! node.Children
          |> Seq.map (fun ch -> ch.Name)
          |> set
          |> assertEquals (set ["ViolatingTest"; "ThrowingTest"; "NewPassingTest"])
    }

  module test_Update =
    let seed () =
      test {
        let typ = typeof<TestClass1>
        let node = TestClassNode(typ.FullName)
        let schema = TestClassSchema.ofType typeof<TestClass1>
        node.UpdateSchema(schema)
        do! node.TestStatus.Value |> assertEquals TestStatus.NotCompleted
        return (typ, node)
      }

    let ``test Update`` =
      test {
        let! (typ, node) = seed ()
        let testClass = TestClass.create Timeout.InfiniteTimeSpan typ
        do
          for testMethod in testClass.Result do
            node.Update(testMethod)

        do! node.Children |> Seq.length |> assertEquals 3
        do! node.TestStatus.Value |> assertEquals TestStatus.Error
      }

    let ``test Update default constructor`` =
      test {
        let! (_, node) = seed ()
        let defaultConstructor = TestMethod.ofInstantiationError (exn())
        node.Update(defaultConstructor)
        do! node.Children |> Seq.length |> assertEquals 4
        do! node.TestStatus.Value |> assertEquals TestStatus.Error
      }

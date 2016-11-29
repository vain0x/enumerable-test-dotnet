namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.Generic
open System.IO
open System.Reactive.Subjects

type PermanentTestRunner() =
  let fileNames = HashSet<_>()

  let assemblies = ResizeArray()

  let assemblyAdded =
    new Subject<_>()

  member this.AssemblyAdded =
    assemblyAdded :> IObservable<_>

  member this.LoadFile(file: FileInfo) =
    if fileNames.Add(file.FullName) then
      let assembly = new FileLoadingTestAssembly(file)
      assemblies.Add(assembly)
      assemblyAdded.OnNext(assembly)
      assembly.Start()

  member this.Dispose() =
    assemblyAdded.Dispose()
    for assembly in assemblies do
      assembly.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

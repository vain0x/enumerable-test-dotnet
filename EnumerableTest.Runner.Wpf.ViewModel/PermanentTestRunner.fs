namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.Generic
open System.IO
open System.Reactive.Subjects
open EnumerableTest.Runner

[<AbstractClass>]
type PermanentTestRunner() =
  abstract AssemblyAdded: IObservable<PermanentTestAssembly>

  abstract Dispose: unit -> unit

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

[<Sealed>]
type FileLoadingPermanentTestRunner(notifier: Notifier) =
  inherit PermanentTestRunner()

  let fileNames = HashSet<_>()

  let assemblies = ResizeArray()

  let assemblyAdded =
    new Subject<_>()

  override this.AssemblyAdded =
    assemblyAdded :> IObservable<_>

  member this.LoadFile(file: FileInfo) =
    if fileNames.Add(file.FullName) then
      let assembly = new FileLoadingPermanentTestAssembly(notifier, file)
      assemblies.Add(assembly)
      assemblyAdded.OnNext(assembly)
      assembly.Start()

  override this.Dispose() =
    assemblyAdded.Dispose()
    for assembly in assemblies do
      assembly.Dispose()

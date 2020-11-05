using System;

namespace Microcelium.Importer.vNext.Impl
{
  /// <summary>
  /// Creates <see cref="IFilePersister"/>s via a Delegate method
  /// </summary>
  public class DelegatePersisterFactory : IFilePersisterFactory
  {
    private readonly Func<IFilePersister> factoryMethod;
    private readonly string dsn;

    /// <summary>
    /// Instantiates an <see cref="DelegatePersisterFactory"/>
    /// </summary>
    /// <param name="factoryMethod">the method that instantiates <see cref="IFilePersister"/></param>
    public DelegatePersisterFactory(Func<IFilePersister> factoryMethod)
    {
      this.factoryMethod = factoryMethod;
    }

    /// <inheritdoc />
    public IFilePersister CreateFilePersister() => factoryMethod();
  }
}
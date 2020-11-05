using System;
using Microsoft.Data.SqlClient;
using Microcelium.Importer.Persistence;

namespace Microcelium.Importer.Config
{
  /// <summary>
  /// The primary internal interface for Persister configuration
  /// </summary>
  internal interface IPersisterConfiguration<TRecord> where TRecord : class, IImportRecord
  {
    /// <summary>
    /// Builds the persister configuration from this and the <see cref="FileImportConfiguration{TRecord}" />
    /// </summary>
    void Build(FileImportConfiguration<TRecord> configuration);
  }

  /// <summary>
  /// The base and publically exposed configuration mechanism for persisters.
  /// Implement this class to create persistence.
  /// </summary>
  public abstract class PersisterConfiguration<TRecord> : IPersisterConfiguration<TRecord>
    where TRecord : class, IImportRecord
  {
    /// <summary>
    ///   The type yielded from <see cref="Build" />
    /// </summary>
    public abstract Type PersisterType {get; }

    private PersisterExceptionBehaviorConfiguration exceptionConfig;

    /// <summary>
    ///   Should pre-import prepartion steps be executed
    /// </summary>
    public bool Prepare { get; set; }

    /// <summary>
    ///   Configures the exception handling for the persister
    /// </summary>
    public PersisterConfiguration<TRecord> OnException(PersisterExceptionBehaviorConfiguration exceptionConfig)
    {
      if (exceptionConfig != null)
        throw ImporterException.PersisterExceptionBehaviorAlreadyConfigured(exceptionConfig);

      this.exceptionConfig = exceptionConfig;
      return this;
    }

    /// <inheritdoc />
    public abstract void Build(FileImportConfiguration<TRecord> configuration);
  }
}

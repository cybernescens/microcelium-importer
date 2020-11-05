using System;
using Microcelium.Importer.Config;

namespace Microcelium.Importer
{
  /// <summary>
  ///   The base Exception thrown by Microcelium.Importer
  /// </summary>
  public class ImporterException : Exception
  {
    /// <summary>
    ///   Instantiates the base Exception thrown by Microcelium.Importer
    /// </summary>
    public ImporterException(string message) : base(message)
    {
    }

    internal static ImporterException PersisterExceptionBehaviorAlreadyConfigured(
      PersisterExceptionBehaviorConfiguration exceptionConfig)
        => new ImporterException($"Persister Exception Behavior already configured as: `{exceptionConfig}`");
  }
}

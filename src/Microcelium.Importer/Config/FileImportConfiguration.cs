using System;
using Microsoft.Extensions.DependencyInjection;
using Microcelium.Importer;
using Microcelium.Importer.Persistence;
using Microcelium.Importer.Parsing;
using System.Linq;

namespace Microcelium.Importer.Config
{
  /// <summary>
  ///   The primary service configuration object
  /// </summary>
  internal interface IFileImportConfiguration<T> where T : class, IImportRecord
  {
    /// <summary>
    ///   Builds the <see cref="IFileImportService" /> from this configuration object
    /// </summary>
    IFileImportService Build();
  }

  /// <summary>
  ///   Configures the <see cref="FileImportService" />
  /// </summary>
  public class FileImportConfiguration<TRecord> : IFileImportConfiguration<TRecord>
    where TRecord : class, IImportRecord
  {
    private static readonly Type serviceType = typeof(IFileImportService<,>);

    /// <summary>
    ///   Base constructor for a FileImportCollection
    /// </summary>
    /// <param name="serviceCollection">the serviceCollection object for creating objects</param>
    protected FileImportConfiguration(IServiceCollection serviceCollection)
    {
      ServiceCollection = serviceCollection;
    }

    /// <summary>
    /// Collection of dependencies that eventually yields a <see cref="IServiceProvider" />
    /// </summary>
    public IServiceCollection ServiceCollection {get; private set;}

    /// <summary>
    /// Location of import files
    /// </summary>
    public string LocalUnc { get; set; }

    /// <summary>
    /// Predicate to filter any files
    /// </summary>
    public Func<string, bool> FileAcceptPredicate { get; set; }

    /// <summary>
    /// Should we recurse any directories in <see cref="LocalUnc" />
    /// </summary>
    public bool Recurse { get; set; }

    /// <summary>
    /// Reprocess files of unknown status
    /// </summary>
    public bool ReprocessUnknowns { get; set; }

    /// <summary>
    /// Reprocess files that have failed
    /// </summary>
    public bool ReprocessFailures { get; set; }

    /// <summary>
    ///   Gets and sets the <see cref="JournalConfiguration" />
    /// </summary>
    public JournalConfiguration JournalConfiguration {get; set; }

    /// <summary>
    ///   Gets and sets the <see cref="ParserConfiguration{T}" />.
    /// </summary>
    public ParserConfiguration<TRecord> ParserConfiguration { get; set; }

    /// <summary>
    ///   Gets the sets <see cref="PersisterConfiguration{T}" /> when none is provided.
    /// </summary>
    public PersisterConfiguration<TRecord> PersisterConfiguration {get; set; }

    /// <inheritdoc />
    public IFileImportService Build()
    {
      if (this.ParserConfiguration == null)
        throw new InvalidOperationException();

      if (this.PersisterConfiguration == null)
        throw new InvalidOperationException();

      JournalConfiguration.Build(this);
      ParserConfiguration.Build(this);
      PersisterConfiguration.Build(this);

      var serviceInterface = serviceType.MakeGenericType(
        ParserConfiguration.ParserType, PersisterConfiguration.PersisterType);

      var serviceImpl = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(x => x.GetTypes())
        .FirstOrDefault(x => x.GetInterfaces().Any(y => y.Equals(serviceInterface)));

      if (serviceImpl == null)
      {
        var typeparams = serviceInterface.GetGenericArguments();
        throw new InvalidOperationException(
          $"Could not resolve any implementation in any loaded assembly that " +
          $"implements service interface: `{serviceType}` with type parameters of " +
          $"`{typeparams[0]}` and `{typeparams[1]}`.");
      }

      ServiceCollection.AddSingleton(serviceInterface, serviceImpl);

      return (IFileImportService) ServiceCollection
        .BuildServiceProvider()
        .GetService(serviceInterface);
    }
  }
}

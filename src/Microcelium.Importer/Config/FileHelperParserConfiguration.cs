using System;
using FileHelpers;
using Microcelium.Importer.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Microcelium.Importer.Config
{
  /// <summary>
  ///  Configuration object that ultimately yields a <see cref="FileHelperSqlDataReaderParser{TRecord}" />
  /// </summary>
  public class FileHelperParserConfiguration<TRecord> : ParserConfiguration<TRecord>
    where TRecord : class, IImportRecord
  {
    private Action<FileLineContext<TRecord>> afterRead;
    private Action<FileLineContext<TRecord>> beforeRead;
    private IProgressReporter progressReporter;

    /// <summary>
    ///   Action performed after an file line is read and <typeparamref name="TRecord" /> is hydrated
    /// </summary>
    public FileHelperParserConfiguration<TRecord> AfterRead(Action<FileLineContext<TRecord>> afterRead)
    {
      if (afterRead == null)
        throw new InvalidOperationException();

      this.afterRead = afterRead;
      return this;
    }

    /// <summary>
    ///   Action performed after a file line is read and before a <typeparamref name="TRecord" /> is hydrated
    /// </summary>
    public FileHelperParserConfiguration<TRecord> BeforeRead(Action<FileLineContext<TRecord>> beforeRead)
    {
      if (beforeRead == null)
        throw new InvalidOperationException();

      this.beforeRead = beforeRead;
      return this;
    }

    /// <summary>
    ///   Configures the object that keeps track of the file parsing progress
    /// </summary>
    public FileHelperParserConfiguration<TRecord> ProgressReporter(IProgressReporter progressReporter)
    {
      if (progressReporter == null)
        throw new InvalidOperationException();

      this.progressReporter = progressReporter;
      return this;
    }

    public override Type ParserType => typeof(FileHelperSqlDataReaderParser<TRecord>);

    public override void Build(FileImportConfiguration<TRecord> configuration)
    {
      var sc = configuration.ServiceCollection;
      sc.AddSingleton<IExecuteBeforeReadRecord<TRecord>>(
        this.beforeRead == null
        ? new NoOpBeforeReadRecord<TRecord>()
        : new ExecuteBeforeReadRecord<TRecord>(this.beforeRead));

      sc.AddSingleton<IExecuteAfterReadRecord<TRecord>>(
        this.afterRead == null
        ? new NoOpAfterReadRecord<TRecord>()
        : new ExecuteAfterReadRecord<TRecord>(this.afterRead));

      //sc.AddSingleton<Type>(typeof(TRecord));
      sc.AddScoped<IProgressReporter, ProgressReporter>();
      sc.AddScoped<IParser<SqlParserReader<TRecord>, SqlDataReader>, FileHelperSqlDataReaderParser<TRecord>>();
      sc.AddScoped<FileHelperAsyncEngine<TRecord>, FileHelperAsyncEngine<TRecord>>();
      sc.AddScoped<IParserReader<SqlDataReader>, SqlParserReader<TRecord>>();
      sc.AddScoped<IParserEngine, FileHelperParserEngine>();
      sc.AddScoped<SqlDataReader, SqlDataReader>();
    }
  }

  /// <summary>
  ///   Extension methods to facilitate configuration of the <see cref="FileHelperSqlDataReaderParser{}" />
  /// </summary>
  public static class FileHelperConfigurationExtensions
  {
    /// <summary>
    ///  Instantiates and configures a parser backed by FileHelpers
    /// </summary>
    public static FileImportConfiguration<T> FileHelperParser<T>(
      this FileImportConfiguration<T> importConfiguration,
      Action<FileHelperParserConfiguration<T>> config,
      FileHelperParserConfiguration<T> parser = null)
      where T : class, IImportRecord
    {
      parser = parser ?? new FileHelperParserConfiguration<T>();
      config?.Invoke(parser);
      importConfiguration.ParserConfiguration = parser;
      return importConfiguration;
    }
  }
}

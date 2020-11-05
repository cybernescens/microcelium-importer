using System;
using Microsoft.Extensions.Logging;

namespace Microcelium.Importer
{
  /// <summary>
  ///   The primary logging mechanism for Microcelium.Importer
  /// </summary>
  public class LogProvider
  {
    private static readonly Lazy<LogProvider> instance = new Lazy<LogProvider>(Initialize);

    private LogProvider()
    {
      //initialize here
    }

    private static LogProvider Initialize() => new LogProvider();

    /// <summary>
    ///   Returns a <see cref="ILogger" /> for the provided <typeparamref name="T" />
    /// </summary>
    /// <typeparam name="T">the type currently logging to this <see cref="ILogger" /> instance</typeparam>
    public static ILogger For<T>() => For(typeof(T));

    /// <summary>
    ///   Returns a <see cref="ILogger" /> for the provided <paramref name="t" />
    /// </summary>
    /// <param name="t">the type currently logging to this <see cref="ILogger" /> instance</param>
    public static ILogger For(Type t)
    {
      throw new NotImplementedException();
    }
  }
}

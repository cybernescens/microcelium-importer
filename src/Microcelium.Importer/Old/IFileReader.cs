using System;
using System.Data;
using System.Data.SqlClient;
using Microcelium.Importer.vNext.Impl;

namespace Microcelium.Importer
{
  /// <summary>
  ///   Similar to the SqlConnection -&gt; SqlCommand -&gt; SqlDataReader pattern this
  ///   is responsible for creating the <see cref="FileDataReader{T}" /> that can be
  ///   passed to the <see cref="SqlBulkCopy" />
  /// </summary>
  public interface IFileReader : IDisposable
  {
    /// <summary>
    ///   Initializes the instance so it can be enumerated
    /// </summary>
    IDataReader ExecuteReader();
  }
}

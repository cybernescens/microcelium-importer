namespace Microcelium.Importer
{
  /// <summary>
  ///   Represents the entire set of data from one file. If there were a huge
  ///   file and the entire set of data didn't fit in memory, it could be
  ///   streamed and/or swapped in/out of memory, but it doesn't seem like an
  ///   issue that will come up.
  /// </summary>
  public interface IDbFileData
  {
    /// <summary>
    ///   short textual summary of data
    /// </summary>
    string Summary { get; }

    /// <summary>
    ///   write to db; may be in multiple tables
    /// </summary>
    void Save(int importEventId, string dsn);

    /// <summary>
    ///   delete all records
    ///   I would like something other than an instance method for this, since
    ///   you have to create an otherwise empty object for it. Basically,
    ///   importer creates the object and we want importer to be focused on
    ///   getting from a textual representation to an object representation and
    ///   NOT from object to db--so even though we have a polymorphic object in
    ///   the importer, we don't have one that's prepared to delete. It could
    ///   call the appropriate static method on the IDbFileData, but being a static
    ///   method, there's no way to genericize the call and ensure that such a
    ///   method is always created in every IDbFileData. An object method requires
    ///   creation of an object that otherwise doesn't make sense, since it
    ///   contains no actual data. One option would be to add a class (maybe
    ///   even a generic), like 'DbWriter' that takes the IDbFileData and writes
    ///   it. For now, we'll just instantiate the appropriate IDbFileData object
    ///   and provide a delete method on each one. See project
    ///   'FileImporterGenericExperiment' for alternative framework idea
    /// </summary>
    void Delete(int importEventId, string dsn);
  }
}
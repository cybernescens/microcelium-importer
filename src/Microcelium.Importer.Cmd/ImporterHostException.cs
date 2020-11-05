using System;

namespace Microcelium.Importer.Cmd
{
  public class ImporterHostException : Exception
  {
    public ImporterHostException(string message) : base(message) { }
  }
}
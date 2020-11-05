using System;

namespace Microcelium.Importer.Meta
{
  /// <summary>
  ///   Decorates Properties backed by the persistence store
  /// </summary>
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
  public class PersistenceAttribute : Attribute
  {
    /// <summary>
    /// Just let's us know where this property is going
    /// </summary>
    public PersistenceAttribute(string name = null)
    {
      Name = name;
    }

    /// <summary>
    /// The name of the field in the destination persistence store
    /// </summary>
    public string Name { get; }
  }
}

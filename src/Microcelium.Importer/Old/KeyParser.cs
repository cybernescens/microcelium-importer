using System;
using System.Text.RegularExpressions;

namespace Microcelium.Importer
{
  /// <summary>
  ///   everything starts as a string
  ///   These can be defined to call a specific parser on that string (delivered
  ///   from a switch statement somewhere) before adding the parameters to the
  ///   SqlCommand
  ///   Any time a string needs a new parser, add a type here and a method elsewhere
  /// </summary>
  public enum ParseKeyOpt
  {
    String,
    Int32,
    Int32Nullable,

    /// <summary>
    ///   yyyyMMdd
    /// </summary>
    DateIso8,

    Decimal,
    DateIso8Nullable,

    /// <summary>
    ///   $4.15 represented as 415 in text file, 4.15 decimal in db
    /// </summary>
    MoneyX100Nullable
  }

  public static class KeyParser
  {
    /// <summary>
    ///   todo: verify this object goes into the db correctly...
    ///   it should be the right object type, but we're returning a generic object.
    ///   I think that's just the contract the method promises, but double check
    /// </summary>
    public static object Parse(string s, ParseKeyOpt key)
    {
      switch (key)
      {
        case ParseKeyOpt.String:
          return s;
        case ParseKeyOpt.Int32:
          return int.Parse(s);
        case ParseKeyOpt.Int32Nullable:
          return s.Length == 0 || Regex.Replace(s, "\\s*", "").Length == 0 ? null : (int?)int.Parse(s);

        case ParseKeyOpt.Decimal:
          return decimal.Parse(s);
        case ParseKeyOpt.MoneyX100Nullable:
          return s == "" ? DBNull.Value : (object)(Convert.ToDecimal(int.Parse(s)) / 100m);
        case ParseKeyOpt.DateIso8Nullable:
          return s == "00000000" || s == "" ? DBNull.Value : (object)DateTime.ParseExact(s, "yyyyMMdd", null);
        case ParseKeyOpt.DateIso8:
          return s == "00000000" ? DateTime.MinValue : DateTime.ParseExact(s, "yyyyMMdd", null);

        default:
          throw new NotImplementedException();
      }
    }
  }
}
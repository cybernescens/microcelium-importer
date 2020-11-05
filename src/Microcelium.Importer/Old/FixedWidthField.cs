using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Microcelium.Importer
{
  /// <summary>
  ///   for fixed width files, a collection of these defines a line
  ///   added 2011 11 02 for MedImpact and future stuff
  /// </summary>
  public class FixedWidthField
  {
    public readonly string Name;
    public readonly string Notes;

    /// <summary>
    ///   optional
    /// </summary>
    public readonly ParseKeyOpt ParseKey;

    public readonly int Width;

    /// <summary>
    ///   usually we'll trim every field; this can be overridden
    /// </summary>
    public bool DoNotTrim;

    private bool? nullableOverride;

    //could also have a delegate, interface, or base class for custom parsers 
    // if that is null, we use the parse key. If it's not null, we call its
    // single method which takes a string and returns an object--but again, 
    // first be sure that technique works in general for dbs. Alternatively,
    // we can always select into db varchar and then fix there

    public FixedWidthField(string name, int width, string notes, ParseKeyOpt parseKey)
    {
      Name = name;
      Width = width;
      Notes = notes;
      ParseKey = parseKey;
    }

    public FixedWidthField(string name, int width, string notes) : this(name, width, notes, ParseKeyOpt.String) { }
    public FixedWidthField(string name, int width) : this(name, width, null) { }
    public FixedWidthField(string name, int width, ParseKeyOpt parseKey) : this(name, width, null, parseKey) { }

    private string SqlType
    {
      get
      {
        switch (ParseKey)
        {
          //case ParseKeyOpt.String: return "varchar(" + Width + ")";
          case ParseKeyOpt.Int32:
            return "int";
          case ParseKeyOpt.Int32Nullable:
            return "int";
          case ParseKeyOpt.DateIso8:
            return "datetime"; //could be 'date' in sql 2008
          case ParseKeyOpt.DateIso8Nullable:
            return "datetime"; //could be 'date' in sql 2008
          case ParseKeyOpt.MoneyX100Nullable:
            return "decimal(12,2)";
          default:
            return "varchar(" + Width + ")";
        }
      }
    }

    public bool Nullable
    {
      get
      {
        if (nullableOverride.HasValue)
          return nullableOverride.Value;
        switch (ParseKey)
        {
          case ParseKeyOpt.Int32Nullable:
            return true;
          case ParseKeyOpt.DateIso8Nullable:
            return true;
        }

        return false;
      }
      set => nullableOverride = value;
    }

    //todo: show invalid rows by making another method that wraps each thing in this SqlCast in a try/catch or iserr or something...

    /// <summary>
    ///   a line that casts/converts from varchar to specific type
    /// </summary>
    public string SqlCastConvertFromVarchar
    {
      get
      {
        var lname = ("[" + Name + "]").PadRight(40); //padded name
        switch (ParseKey)
        {
          case ParseKeyOpt.Int32:
            return string.Format(@"{0} = CAST ((CASE WHEN LEN([{1}])=0 OR ISNUMERIC([{1}])=0  THEN NULL ELSE [{1}] END)  AS INT)", lname, Name); //just will be an error if it's blank
          case ParseKeyOpt.Int32Nullable:
            return string.Format(@"{0} = CAST ((CASE WHEN LEN([{1}])=0 OR ISNUMERIC([{1}])=0  THEN NULL ELSE [{1}] END)  AS INT)", lname, Name);

          //case ParseKeyOpt.DateIso8Nullable: return string.Format(@"{0} = CAST ((CASE WHEN LEN([{1}])=0 THEN NULL ELSE [{1}] END)  AS date)", lname, Name);
          //LineNumber is very specific--later, make this something to identify a general row

          //          case ParseKeyOpt.DateIso8Nullable: return string.Format(@"{0} = CAST(
          //              CASE WHEN ISDATE([{1}])=1 
          //                   THEN [{1}] 
          //                   ELSE CASE WHEN [{1}] IN ('00000000','') THEN NULL ELSE 'ErrLine'+TraceLineNumber END
          //               END AS DATE)", lname, Name);

          //          case ParseKeyOpt.DateIso8Nullable: return string.Format(@"{0} = CAST(
          //              CASE WHEN ISDATE([{1}])=1 
          //                   THEN [{1}] 
          //                   ELSE CASE WHEN [{1}] IN ('00000000','') THEN NULL ELSE [{1}] END
          //               END AS DATE)", lname, Name);

          case ParseKeyOpt.DateIso8Nullable:
            return string.Format(@"{0} = CAST(CASE WHEN [{1}] IN ('00000000','') THEN NULL ELSE [{1}] END AS DATE)", lname, Name);

          //case ParseKeyOpt.MoneyX100Nullable: return string.Format(@"{0} = CAST([{1}] as decimal(12,2))/100.0", lname, Name);
          case ParseKeyOpt.MoneyX100Nullable:
            return string.Format(@"{0} = CAST ((CASE WHEN LEN([{1}])=0 OR ISNUMERIC([{1}])=0 THEN NULL ELSE [{1}] END) as decimal(12,2))/100.0", lname, Name);

          //case ParseKeyOpt.DateIso8:return 
          //  break;
          case ParseKeyOpt.Decimal:
            return string.Format(@" {0} = CAST ((CASE WHEN LEN([{1}])=0 OR ISNUMERIC([{1}])=0 THEN NULL ELSE [{1}] END) as decimal(12,2))", lname, Name);

          //  break;
          default:
            return string.Format(@"{0} = [{1}]", lname, Name);
        }
      }
    }

    public override string ToString() => string.Format(@"{0} [{1}]: {2}", ParseKey, Width, Name);

    /// <summary>
    /// </summary>
    /// <param name="varcharFields">set to true to ignore type and use varchar instead in all cases</param>
    /// <returns></returns>
    public string SqlCreateLine(bool varcharFields)
    {
      var t = varcharFields ? "varchar(" + Width + ")" : SqlType;
      var nullable = Nullable ? "NULL" : "NOT NULL";
      return string.Format(@"[{0}] {1} {2}", Name, t, nullable);

      //get { return string.Format(@"[{0}] {1}", Name, SqlType); }
    }

    internal FixedWidthField Clone()
    {
      var ret = new FixedWidthField(Name, Width, Notes, ParseKey);
      ret.nullableOverride = nullableOverride;
      ret.DoNotTrim = DoNotTrim;
      //todo: be sure this is everything
      return ret;
    }
  }

  public class FixedWidthFieldCollection : Collection<FixedWidthField>
  {
    /// <summary>
    ///   sum of all widths
    /// </summary>
    public int TotalWidth => GetWidths().Sum();

    public void AddNew(string name, int width, string notes, ParseKeyOpt parseKey) => Add(new FixedWidthField(name, width, notes, parseKey));

    public void AddNew(string name, int width, string notes) => Add(new FixedWidthField(name, width, notes));

    public void AddNew(string name, int width) => Add(new FixedWidthField(name, width));

    /// <summary>
    /// </summary>
    /// <param name="cstrTableVarcharStage"></param>
    /// <param name="varcharFields">don't use type info; create all as varchar fields</param>
    public string AsSqlCreate(string tableName, bool varcharFields)
    {
      var ftexts = new List<string>();
      foreach (var f in this)
      {
        ftexts.Add(f.SqlCreateLine(varcharFields));
      }

      var sql = string.Format(
        @"
create table [{0}]
(
  {1}
)
",
        tableName,
        string.Join(", " + Environment.NewLine, ftexts));
      return sql;
    }

    /// <summary>
    ///   example usage: wrap this in a "with t as ($this) select * into $typed from t"
    /// </summary>
    public string GetVarcharCastConvert(string tableName)
    {
      var lines = new List<string>();
      foreach (var f in this)
      {
        lines.Add(f.SqlCastConvertFromVarchar);
      }

      var joinedCastLines = string.Join(", " + Environment.NewLine, lines);
      return string.Format(
        @"
select
{0}
  from [{1}]
    ",
        joinedCastLines,
        tableName);
    }

    public string AsSimpleReport()
    {
      var sb = new StringBuilder();
      var i = 1;
      var accum = 0;
      foreach (var item in this)
      {
        sb.Append(i.ToString().PadLeft(4));
        sb.Append(item.Name.PadLeft(40));
        sb.Append(item.Width.ToString().PadLeft(9));
        sb.Append((accum + 1).ToString().PadLeft(9));
        sb.Append((accum + item.Width).ToString().PadLeft(9));
        sb.AppendLine();
        i++;
        accum += item.Width;
      }

      return sb.ToString();
    }

    /// <summary>
    ///   for use with substring
    /// </summary>
    public int[] GetWidths() => this.Select(f => f.Width).ToArray();

    /// <summary>
    ///   zero-based; for use with substring
    /// </summary>
    public int[] GetStartIndices()
    {
      var ret = new List<int>();
      var accum = 0;
      foreach (var w in GetWidths())
      {
        ret.Add(accum);
        accum += w;
      }

      return ret.ToArray();
    }

    public FixedWidthFieldCollection Clone()
    {
      var ret = new FixedWidthFieldCollection();
      foreach (var f in this)
      {
        ret.Add(f.Clone());
      }

      return ret;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microcelium.Importer
{
  /// <summary>
  ///   added 2011 11 02 for MedImpact and future stuff
  /// </summary>
  public abstract class LineParser
  {
    //up to the constructor for various subtypes like delimited and fixed 
    //width to acquire all the info needed to parse a line without further 
    //arguments

    public abstract List<string> SplitLine(string line);
    public abstract List<object> ParseLine(string line);

    //could have annotated line here.. something that parses to a LineInfo object with 
    // string GetField(name)
    // or 
    // LineInfo[] GetLineInfos(line)
    // where LineInfo had string Name, string Comments, and string Value

    public bool TryParse(string line, out List<object> parts)
    {
      try
      {
        parts = ParseLine(line);
        return true;
      }
      catch
      {
        parts = null;
        return false;
      }
    }
  }

  public class FixedWidthLineParser : LineParser
  {
    public readonly FixedWidthFieldCollection Fields;

    public FixedWidthLineParser(FixedWidthFieldCollection fields)
    {
      Fields = fields;
    }

    /// <summary>
    ///   just retrieve substrings
    /// </summary>
    public override List<string> SplitLine(string line)
    {
      var ret = new List<string>();
      var left = 0;
      foreach (var f in Fields)
      {
        try
        {
          var s = line.Substring(left, f.Width);
          if (!f.DoNotTrim)
            s = s.Trim();
          ret.Add(s);
        }
        catch (ArgumentOutOfRangeException e)
        {
          var msg = string.Format(@"Argument out of range attempting to find " + @"substring starting at column {0} with width {1}, looking for " + @"field '{2}'. See inner exception for details.", left, f.Width, f.Name);
          throw new ApplicationException(msg, e);
        }

        left += f.Width;
      }

      //todo: see if there are extra columns left

      return ret;
    }

    public override List<object> ParseLine(string line) => ParseParts(SplitLine(line));

    /// <summary>
    ///   parse parts of line
    /// </summary>
    private List<object> ParseParts(List<string> parts)
    {
      var ret = new List<object>();
      for (var i = 0; i < Fields.Count(); i++)
      {
        var f = Fields[i];
        var s = parts[i];
        ret.Add(KeyParser.Parse(s, f.ParseKey));
      }

      return ret;
    }
  }
}
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microcelium.Importer.Cmd
{
  internal class BadX12Importer
  {
    private static readonly SHA1 shaCsp = new SHA1CryptoServiceProvider();
    private readonly string dsn;

    public BadX12Importer(string dsn)
    {
      this.dsn = dsn;
    }

    public Result Import(string filePath)
    {
      var sha1Hex = GetSha1HexString(filePath);
      if (Sha1Exists(sha1Hex))
        return new Result {Successful = false, Message = "File aready exists; please manually remove."};

      var fi = new FileInfo(filePath);

      using (var conn = new SqlConnection(dsn))
      {
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
          cmd.CommandText = @"
            insert into ImportX12Event (
              Sha1, FileOriginalName, FileCreateMoment, FileLastWriteMoment, FileByteLength, FileLineCount, 
              ImportMoment, ImporterName, ImporterVersion, ImportCompletedMoment)
            values (
              @sha1, @fileOriginalName, @fileCreateMoment, @fileLastWriteMoment, @fileByteLength, 0,
              @now, @importerName, @importerVersion, @now)
          ";

          cmd.Parameters.Add("@sha1", SqlDbType.Char, 40).Value = sha1Hex;
          cmd.Parameters.Add("@fileOriginalName", SqlDbType.VarChar, 100).Value = fi.Name;
          cmd.Parameters.Add("@fileCreateMoment", SqlDbType.DateTime).Value = fi.CreationTime;
          cmd.Parameters.Add("@fileLastWriteMoment", SqlDbType.DateTime).Value = fi.LastWriteTime;
          cmd.Parameters.Add("@fileByteLength", SqlDbType.BigInt).Value = fi.Length;
          cmd.Parameters.Add("@now", SqlDbType.DateTime).Value = DateTime.Now;
          cmd.Parameters.Add("@importerName", SqlDbType.VarChar, 50).Value = typeof(BadX12Importer).FullName;
          cmd.Parameters.Add("@importerVersion", SqlDbType.VarChar, 15).Value =
            typeof(BadX12Importer).Assembly.GetName().Version.ToString();
          cmd.ExecuteNonQuery();
        }
      }

      return new Result {Successful = true};
    }

    private bool Sha1Exists(string sha1Hex)
    {
      using (var conn = new SqlConnection(dsn))
      {
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
          cmd.CommandText = "select count(*) from ImportX12Event where Sha1 = @sha1";
          cmd.Parameters.Add("@sha1", SqlDbType.Char, 40).Value = sha1Hex;

          var count = (int)cmd.ExecuteScalar();

          return count > 0;
        }
      }
    }

    private static byte[] GetSha1(string path)
    {
      byte[] sha1;
      using (Stream stream = File.OpenRead(path))
        sha1 = shaCsp.ComputeHash(stream);

      return sha1;
    }

    private static string GetSha1HexString(string path) => ConvertToHex(GetSha1(path));

    private static string ConvertToHex(byte[] bytes)
    {
      var hex = new StringBuilder();
      foreach (var b in bytes)
        hex.AppendFormat(b < 16 ? "0{0:x}" : "{0:x}", b);
      return hex.ToString();
    }

    public class Result
    {
      public bool Successful { get; set; }
      public string Message { get; set; }
    }
  }
}
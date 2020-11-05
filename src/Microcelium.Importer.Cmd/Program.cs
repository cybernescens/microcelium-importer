using System.Reflection.Metadata.Ecma335;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using PowerArgs;
using Serilog;

namespace Microcelium.Importer.Cmd
{
  internal class Program
  {
    private static int Main(string[] args)
    {
      Log.Logger = new LoggerConfiguration()
        .Enrich.WithProperty("Application", "Microcelium.Importer.Cmd")
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File(LogFile("Microcelium.Importer.Cmd"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 5)
        .CreateLogger();

      try
      {
        Args.InvokeAction<ImporterHost>(args);
      }
      catch (ArgException ex)
      {
        Log.Error(ex.Demystify(), "ArgumentException");
        Log.Information(ArgUsage.GenerateUsageFromTemplate<ImporterHost>().ToString());
        return -1;
      }
      catch (ImporterHostException e)
      {
        Log.Error(e.Demystify(), "ImporterHostException");
        return -1;
      }
      catch (Exception e)
      { 
        Log.Error(e.Demystify(), "Exception");
        return -1;
      }

      return 0;
    }

    public static string LogFile(string serviceName)
    {
      var shortName = $"{serviceName}_{DateTime.Now:yyyyMMdd}.log";
      var cfgVal = TryEnsureDirWritable("D:\\ialc_logs") ? "D:\\ialc_logs" : TryEnsureDirWritable("C:\\ialc_logs") ? "C:\\ialc_logs" : null;
      var useConfig = !string.IsNullOrEmpty(cfgVal) && TryEnsureDirWritable(cfgVal);
      var logFolder = useConfig ? cfgVal : Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "Log"); //fall back
      var fallbackOk = TryEnsureDirWritable(logFolder);
      if (!fallbackOk)
        return null;

      return Path.Combine(logFolder, shortName);
    }

    private static bool TryEnsureDirWritable(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      try
      {
        EnsureDirExists(path);
        return CanWrite(path);
      }
      catch
      {
        return false;
      }
    }

    private static void EnsureDirExists(string dirToEnsure)
    {
      var oneLevelUp = Path.GetDirectoryName(dirToEnsure);
      if (oneLevelUp != null)
        EnsureDirExists(oneLevelUp);
      if (!Directory.Exists(dirToEnsure))
        Directory.CreateDirectory(dirToEnsure);
    }

    private static bool CanWrite(string path)
    {
      try
      {
        string testFilePath;

        do
        {
          var testFileOffset = "test_write_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "delete_me.txt";
          testFilePath = Path.Combine(path, testFileOffset);
        } while (File.Exists(testFilePath));

        File.WriteAllText(testFilePath, "test; delete me");
        File.Delete(testFilePath);
        return true;
      }
      catch
      {
        return false;
      }
    }
  }
}
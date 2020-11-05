using System;
using Microsoft.Extensions.Logging;

namespace Microcelium.Importer
{
  /// <summary>
  /// Default progress reporter
  /// </summary>
  public interface IProgressReporter : IProgress<long>
  {
    /// <summary>
    ///
    /// </summary>
    void Initialize(long total);
  }

  /// <summary>
  /// Default File Progress Reporter. Steps is calculated so we usually only see
  /// a lot of progress indication for larger files between 40 and 50MB.
  /// So for example, 5KB gives us 1 step, 50KB = 1.0042 steps, 500KB = 1.0467, 5MB = 1.5842, 50MB = 100
  /// </summary>
  public class ProgressReporter : Progress<long>, IProgressReporter
  {
    const double MaxAllowed = 2d;
    const double MinAllowed = 0d;
    const double LocalMax = 50 * 1000 * 1000; //50MB
    const double LocalMin = 5 * 1000; //5KB

    private static readonly string[] Suffix = { "KB", "MB", "GB", "TB", "PB" };
    private readonly ILogger log = LogProvider.For<ProgressReporter>();
    private long total;
    private int lastReportedStep;
    private double interval;
    private bool completeReported;

    /// <summary>
    /// This calculates a scale between 0 and 2 and does 10 to the power of that result. So
    /// 5KB gives us 1 step, 50KB = 1.0042 steps, 500KB = 1.0467, 5MB = 1.5842, 50MB = 100; we'll mostly
    /// see a lot of steps when we fall between the 5MB and 50MB range.
    /// Scale Function: https://stackoverflow.com/questions/5294955/how-to-scale-down-a-range-of-numbers-with-a-known-min-and-max-value
    /// </summary>
    /// <param name="n">the number to calculate our steps for</param>
    /// <returns>the number of steps to then find the step interval</returns>
    private double CalculateSteps(long n) =>
      Math.Pow(
        10d,
        (MaxAllowed - MinAllowed)
          * (Math.Max(Math.Min(n, LocalMax), LocalMin) - LocalMin)
          / (LocalMax - LocalMin)
          + MinAllowed);

    /// <inheritdoc />
    public void Initialize(long total)
    {
      this.total = total;
      this.interval = total / CalculateSteps(total);
    }

    /// <inheritdoc />
    public void Report(long value)
    {
      var step = (int)(value / interval);
      var percent = Convert.ToDouble(value) / Convert.ToDouble(total);
      if (step == lastReportedStep && !value.Equals(total)) return;
      if (completeReported) return;
      log.LogInformation($"{FriendlyBytes(value)} / {FriendlyBytes(total)} ( {percent:P} )");
      lastReportedStep = step;
      completeReported = value.Equals(total);
    }

    private static string FriendlyBytes(long i)
    {
      var total = i * decimal.One; var exp = 0;
      while (total > 1024m && exp <= Suffix.Length)
      {
        ++exp;
        total /= 1024;
      }

      return exp == 0 ? $"{i} bytes" : $"{total:#.00} {Suffix[exp - 1]}";
    }
  }
}

using System;

namespace Microcelium.Importer.Config
{
  /// <summary>
  ///   Configuration for exception behavior
  /// </summary>
  public class PersisterExceptionBehaviorConfiguration
  {
    private ExceptionAction exceptionAction;

    private PersisterExceptionBehaviorConfiguration(ExceptionAction exceptionAction)
    {
      this.exceptionAction = exceptionAction;
    }

    /// <summary>
    ///   Specifies that on Exceptions we just want to Continue to the next file
    /// </summary>
    public static PersisterExceptionBehaviorConfiguration Continue()
      => new PersisterExceptionBehaviorConfiguration(new ContinueExceptionAction());

    /// <summary>
    ///   Specifies that on Exceptions we just want to Rethrow the exception to be caught elsewhere
    /// </summary>
    public static PersisterExceptionBehaviorConfiguration Rethrow()
      => new PersisterExceptionBehaviorConfiguration(new RethrowExceptionAction());

    /// <summary>
    ///   Performs the function callback supplied. When <paramref name="onException" />
    ///   returns true the exception is rethrown, false then we silently continue.
    /// </summary>
    public static PersisterExceptionBehaviorConfiguration Callback(Func<Exception, bool> onException)
      => new PersisterExceptionBehaviorConfiguration(new CallbackExceptionAction(onException));
  }
}

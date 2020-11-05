using System;
using Microsoft.Extensions.Logging;

namespace Microcelium.Importer
{
  public abstract class ExceptionAction
  {
    public abstract bool Handle(Exception e);
  }

  public class ContinueExceptionAction : ExceptionAction
  {
    private static ILogger log = LogProvider.For<ContinueExceptionAction>();

    public override bool Handle(Exception e)
    {
      log.LogWarning(e, "Configured to `continue` on exception...");
      return false;
    }
  }

  public class RethrowExceptionAction : ExceptionAction
  {
    private static ILogger log = LogProvider.For<ContinueExceptionAction>();

    public override bool Handle(Exception e)
    {
      log.LogError(e, "Configured to `throw` on exception...");
      return true;
    }
  }

  public class CallbackExceptionAction : ExceptionAction
  {
    private static ILogger log = LogProvider.For<ContinueExceptionAction>();

    private readonly Func<Exception, bool> callback;

    public CallbackExceptionAction(Func<Exception, bool> callback)
    {
      this.callback = callback;
    }

    public override bool Handle(Exception e)
    {
      log.LogWarning(e, "Excecuting `callback` on exception...");
      return callback(e);
    }
  }
}

using System;

namespace Bicikelj.Model.Logging
{
    public interface ILoggingService
    {
        void LogError(Exception e, string comment);
    }
}

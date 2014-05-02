using System;

namespace Bicikelj.Model
{
    public class ErrorState
    {
        public Exception Exception { get; set; }
        public string Context { get; set; }
        public bool DontLog { get; set; }
        
        public ErrorState()
        {
        }

        public ErrorState(Exception exception, string context = null)
        {
            this.Exception = exception;
            this.Context = context;
        }

        public ErrorState(Exception exception, string context, bool dontLog)
        {
            this.Exception = exception;
            this.Context = context;
            this.DontLog = dontLog;
        }

        public override string ToString()
        {
            string result = "";
            if (!string.IsNullOrWhiteSpace(Context))
                result += Context + "\n";
            if (this.Exception != null)
                result += this.Exception.Message;
            return result;
        }
    }
}

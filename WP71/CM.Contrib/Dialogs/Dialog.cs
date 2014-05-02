using System;
using System.Collections.Generic;
using System.Linq;

namespace Caliburn.Micro.Contrib.Dialogs
{
    public class Dialog<TResponse>
    {
        private TResponse _givenResponse;

        public Dialog(string subject, string message, params TResponse[] possibleResponses)
            : this(DialogType.None, subject, message, possibleResponses)
        {
        }

        public Dialog(DialogType dialogType, string message, params TResponse[] possibleResponses)
            : this(dialogType, dialogType.ToString(), message, possibleResponses)
        {
        }

        public Dialog(DialogType dialogType, string subject, string message, params TResponse[] possibleResponses)
        {
            if (!possibleResponses.Any())
                throw new ArgumentException("No possible responses are given", "possibleResponses");

            DialogType = dialogType;
            Subject = subject;
            Message = message;
            PossibleResponses = possibleResponses;
        }

        public Dialog(DialogType dialogType, string subject, object content, params TResponse[] possibleResponses)
        {
            if (!possibleResponses.Any())
                throw new ArgumentException("No possible responses are given", "possibleResponses");

            DialogType = dialogType;
            Subject = subject;
            Content = content;
            PossibleResponses = possibleResponses;
        }

        public Dialog(string subject, object content, IEnumerable<TResponse> possibleResponses)
        {
            if (!possibleResponses.Any())
                throw new ArgumentException("No possible responses are given", "possibleResponses");

            DialogType = DialogType.None;
            Subject = subject;
            Content = content;
            PossibleResponses = possibleResponses;
        }

        public string Subject { get; set; }
        public string Message { get; set; }
        public object Content { get; set; }

        public DialogType DialogType { get; set; }
        public IEnumerable<TResponse> PossibleResponses { get; protected set; }

        public TResponse GivenResponse
        {
            get { return _givenResponse; }
            set
            {
                _givenResponse = value;
                IsResponseGiven = true;
                if (ResponseGiven != null) ResponseGiven(this, EventArgs.Empty);
            }
        }

        public bool IsResponseGiven { get; private set; }
        public event EventHandler ResponseGiven;
    }

    public class Dialog : Dialog<Answer>
    {
        public Dialog(string subject, string message, params Answer[] possibleResponses)
            : base(subject, message, possibleResponses)
        {
        }

        public Dialog(DialogType dialogType, string message, params Answer[] possibleResponses)
            : base(dialogType, message, possibleResponses)
        {
        }

        public Dialog(DialogType dialogType, string subject, string message, params Answer[] possibleResponses)
            : base(dialogType, subject, message, possibleResponses)
        {
        }
    }
}
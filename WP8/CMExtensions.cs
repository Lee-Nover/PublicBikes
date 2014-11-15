using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caliburn.Micro
{
    public static class CMExtensions
    {
        public static void Publish(this IEventAggregator aggregator, object message)
        {
            aggregator.PublishOnUIThread(message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caliburn.Micro
{
    public static class CMExtensions
    {
        public static void Publish(this IEventAggregator agg, object message)
        {
            agg.PublishOnUIThread(message);
        }
    }
}

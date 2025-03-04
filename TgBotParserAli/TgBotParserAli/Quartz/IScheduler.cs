using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBotParserAli.Quartz
{
    internal interface IScheduler
    {
        void AddTimers(int channelId, Timer parseTimer, Timer postTimer);
        void RemoveTimers(int channelId);
    }
}

using Akka.Actor;
using IMAP.App.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.App.Actors
{
    class EventHubActor : ReceiveActor
    {
        public EventHubActor()
        {
            Receive<EventCmd>(cmd => ReceiveEventCmd(cmd));
        }

        private void ReceiveEventCmd(EventCmd cmd)
        {
            Console.WriteLine("EventName: {0}, payload: {1}", cmd.EventName, cmd.Payload);
        }
    }
}

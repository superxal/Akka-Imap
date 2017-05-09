using Akka.Actor;
using IMAP.App.Actors;
using IMAP.App.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.App
{

    public class StaticActors
    {
        public static IActorRef EventHubActor { get; set; }
        public static IActorRef IMAPActor { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var system = ActorSystem.Create("IMAP");

            StaticActors.EventHubActor = system.ActorOf<EventHubActor>("EventHubActor");
            StaticActors.IMAPActor = system.ActorOf<IMAPActor>("IMAPActor");

            StaticActors.IMAPActor.Tell(new IMAPSubscribeCmd("Test 1", "", @"(\w*Skola\w*)", @"(\w*hejsan\w*)"));
            StaticActors.IMAPActor.Tell(new IMAPSubscribeCmd("Test 2", "", @"(\w*Skola\w*)", ""));
            StaticActors.IMAPActor.Tell(new IMAPSubscribeCmd("Test 3", "", @"(\w*Programmering\w*)", ""));
            StaticActors.IMAPActor.Tell(new IMAPSubscribeCmd("Test 4", "", @"(\w*Programmering\w*)", ""));
            StaticActors.IMAPActor.Tell(new IMAPSubscribeCmd("Test 4", "", @"(\w*123\w*)", ""));
            StaticActors.IMAPActor.Tell(new IMAPSubscribeCmd("Test 4", "", @"(\w*456\w*)", ""));


            // TODO: Longer Time?
            system.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), StaticActors.IMAPActor, new IMAPHeartbeatCmd(), StaticActors.IMAPActor);

            StaticActors.IMAPActor.Tell(new IMAPReceiveMessagesCmd());

            Console.ReadLine();
        }
    }
}

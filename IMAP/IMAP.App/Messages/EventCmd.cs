namespace IMAP.App.Messages
{
    public class EventCmd
    {
        public string EventName { get; private set; }
        public string Payload { get; private set; }

        public EventCmd(string eventName, string payload)
        {
            EventName = eventName;
            Payload = payload;
        }
    }
}

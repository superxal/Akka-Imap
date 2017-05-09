namespace IMAP.App.Messages
{
    public class IMAPSubscribeCmd
    {
        public string EventName { get; private set; }
        public string SubjectRule { get; private set; }
        public string BodyRule { get; private set; }
        public string FromRule { get; private set; }

        public IMAPSubscribeCmd(string eventName, string fromRule, string subjectRule, string bodyRule)
        {
            EventName = eventName;
            SubjectRule = subjectRule;
            FromRule = fromRule;
            BodyRule = bodyRule;
        }
    }
}

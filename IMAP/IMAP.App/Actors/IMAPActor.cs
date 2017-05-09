using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using IMAP.App.Messages;
using MailKit.Net.Imap;
using MailKit;
using MailKit.Search;
using System.Text.RegularExpressions;
using System.Threading;

namespace IMAP.App.Actors
{
    class IMAPActor : ReceiveActor
    {
        private string _userName { get { return ConfigurationManager.AppSettings["IMAPUserName"]; } }
        private string _password { get { return ConfigurationManager.AppSettings["IMAPPassword"]; } }
        private string _server { get { return ConfigurationManager.AppSettings["IMAPServer"]; } }

        private List<IMAPSubscribeCmd> _subcriptions { get; set; }

        private ImapClient _idleClient { get; set; }
        private CancellationTokenSource _idleTokenSource { get; set; }
        private Thread _idleThread { get; set; }

        #region Startup
        protected override void PreStart()
        {
            _idleClient = new ImapClient(new ProtocolLogger("imap.log"));
            _subcriptions = new List<IMAPSubscribeCmd>();
            _idleTokenSource = new CancellationTokenSource();

            StartIdlingConnection();
            base.PreStart();
        }
        public IMAPActor()
        {
            Receive<IMAPReceiveMessagesCmd>(cmd => ReceiveIMAPReceiveMessagesCmd(cmd));
            Receive<IMAPSubscribeCmd>(cmd => ReceiveIMAPSubscribeCmd(cmd));
            Receive<IMAPHeartbeatCmd>(cmd => ReceiveIMAPHeartbeatCmd(cmd));
        }
        #endregion

        #region AkkaReceiveMethods
        private void ReceiveIMAPHeartbeatCmd(IMAPHeartbeatCmd cmd)
        {
            if (_idleClient == null)
            {
                _idleClient = new ImapClient(new ProtocolLogger("imap.log"));
                StartIdlingConnection();
            }
            else
            {
                if (!_idleClient.IsConnected)
                {
                    Console.WriteLine("Reconnecting idle thread");
                    StartIdlingConnection();
                }
            }
        }
        private void ReceiveIMAPReceiveMessagesCmd(IMAPReceiveMessagesCmd cmd)
        {
            // No need to handle messages if there are no subscribers
            if (_subcriptions.Count == 0)
                return;

            try
            {
                using (var client = new ImapClient(new ProtocolLogger(Console.OpenStandardOutput())))
                {
                    ConnectClient(client);

                    // Get all messages since already handled messages are deleted
                    var messages = client.Inbox.Search(SearchQuery.All);
                    if (messages.Count > 0)
                    {
                        foreach (var id in messages)
                        {
                            // get message
                            var message = client.Inbox.GetMessage(id);

                            foreach (var s in _subcriptions)
                            {
                                if (
                                    MatchRule(message.Subject, s.SubjectRule)
                                    || MatchRule(message.From.ToString(), s.FromRule)
                                    || MatchRule(message.GetTextBody(MimeKit.Text.TextFormat.Plain), s.BodyRule)
                                )
                                {
                                    StaticActors.EventHubActor
                                        .Tell(new EventCmd(s.EventName, message.GetTextBody(MimeKit.Text.TextFormat.Plain)));
                                }
                            }

                            // mark message for deletion
                            client.Inbox.AddFlags(id, MessageFlags.Deleted, true);
                        }

                        // Expunge all handled messages
                        client.Inbox.Expunge();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }

        }
        private void ReceiveIMAPSubscribeCmd(IMAPSubscribeCmd cmd)
        {
            _subcriptions.Add(cmd);
        }
        #endregion

        #region IdlingConnection
        private void StartIdlingConnection()
        {
            try
            {
                ConnectClient(_idleClient);

                if (_idleThread != null)
                {
                    if (!_idleThread.IsAlive)
                        _idleThread.Abort();
                }

                ThreadStart threadDelegate = new ThreadStart(this.IdleConnetionThread);
                _idleThread = new Thread(threadDelegate);
                _idleThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
        }
        private void IdleConnetionThread()
        {
            try
            {
                // watch for new messages
                _idleClient.Inbox.MessagesArrived += (sender, e) =>
                {
                    StaticActors.IMAPActor.Tell(new IMAPReceiveMessagesCmd());
                };

                _idleClient.Idle(_idleTokenSource.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
        }
        #endregion

        #region Helpers
        private bool MatchRule(string content, string rule)
        {
            if(rule == "")
            {
                return false; 
            }
            return Regex.Matches(content, rule, RegexOptions.IgnoreCase).Count > 0;
        }
        private void ConnectClient(ImapClient client)
        {
            if (client.IsConnected)
                client.Disconnect(true);

            client.Connect(_server, 993, true);

            // Authenticate
            client.AuthenticationMechanisms.RemoveWhere(x => x != "PLAIN");
            client.Authenticate(_userName, _password);

            client.Inbox.Open(FolderAccess.ReadWrite);
        }
        #endregion

        ~IMAPActor()
        {
            if (_idleThread != null)
            {
                if (_idleThread.IsAlive)
                    _idleThread.Abort();
            }
            if (_idleClient.IsConnected)
            {
                _idleTokenSource.Cancel();
                _idleClient.Disconnect(true);

            }
            if (_idleClient != null)
                _idleClient.Dispose();
            if (_idleTokenSource != null)
                _idleTokenSource.Dispose();
        }
    }
}

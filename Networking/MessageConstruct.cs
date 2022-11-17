using GridTransporter.Networking;
using GridTransporter.BoundrySystem;
using NLog;
using ProtoBuf;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game;



namespace GridTransporter.Networking
{
    [ProtoContract]
    public class MessageConstruct
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [ProtoMember(10)]
        public readonly MessageType Type;

        [ProtoMember(20)]
        public readonly string FromIP;

        [ProtoMember(21)]
        public int GamePort;

        [ProtoMember(40)]
        private readonly byte[] Data;

        public DateTime Timer;
                
        public MessageConstruct(MessageType Type, byte[] Data)
        {
            this.Type = Type;
            this.Data = Data;
            GetGamePort();
        }

        public MessageConstruct() { }

        private void GetGamePort()
        {
            GamePort = MyDedicatedServerOverrides.Port ?? MySandboxGame.ConfigDedicated.ServerPort;
        }


        public async Task Decompile()
        {
            switch (Type)
            {
                case MessageType.GridTransport:

                    GridTransport Transport = Utilities.NetworkUtility.Deserialize<GridTransport>(Data);
                    await Transport.SpawnAsync();
                    break;

                case MessageType.ChatMessage:

                    string msgData = Encoding.UTF8.GetString(Data);

                    long mailID = long.Parse(msgData.Remove(msgData.IndexOf('╥')));

                    if (ModMessage.readMsgs.Contains(mailID))
                        break;

                    ModMessage.readMsgs.Add(mailID);

                    ModMessage.SendChatMessageToAll(msgData);

                    if (!Main.Config.ChatRelay)
                        break;

                    msgData = msgData.Substring(msgData.IndexOf('╥')+1);

                    string Name = msgData.Remove(msgData.IndexOf(':'));
                    string msg = msgData.Substring(msgData.IndexOf(':') + 1);

                    Main.chatMngr.SendMessageAsOther(Name, msg);
                    //MyVisualScriptLogicProvider.SendChatMessage(msg,Name);

                    break;

                default:
                    Log.Info("Unkown message type!");
                    break;


            }
        }
    }

    public class MessageQueue
    {



    }

}

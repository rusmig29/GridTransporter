using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog.Fluent;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using GridTransporter.BoundrySystem;
using GridTransporter.Configs;
using GridTransporter.Networking;
using GridTransporter.Utilities;
using Sandbox.Game;

namespace GridTransporter.Networking
{
    public static class ModMessage
    {
        private static Settings Configs { get { return Main.Config; } }
        
        private static IMyMultiplayer msgSync = null;

        public static List<long> readMsgs = new List<long>();

        public static void Init()
        { 
            ushort msgID = 8844;

            msgSync = MyAPIGateway.Multiplayer;
            
            msgSync.RegisterSecureMessageHandler(msgID, ReceivedPacket);
            
          //  MyAPIGateway.Utilities.MessageRecieved += Utilities_MessageRecieved;
            
        }

        public static void Utilities_MessageRecieved(string arg1, string arg2)
        {        
        
            string msg = "";

            List<IMyPlayer> players = new List<IMyPlayer>();
            
            MyAPIGateway.Players.GetPlayers(players);
            
            long msgMailID = DateTime.Now.Hour * 100000 + DateTime.Now.Minute * 10000 + DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
                      
            msg = msgMailID.ToString() + "╥" + "[" + Configs.ServerName + "]" + arg1 + ":" + arg2;     
           
           // MyVisualScriptLogicProvider.SendChatMessage(msg, "Debug");

            readMsgs.Add(msgMailID);

            SendChatMessageToAll(msg);

        }

        

        public static void SendChatMessageToAll(string data)
        {
            MessageConstruct Construct = new MessageConstruct(MessageType.ChatMessage, Encoding.UTF8.GetBytes(data));            

            foreach (var i in Configs.ServerDestinations)
            {
                i.SendData(Utilities.NetworkUtility.Serialize(Construct));
            }
        }

        private static void ReceivedPacket(ushort id, byte[] rawData, ulong eId, bool temp) // executed when a packet is received on this machine
        {
            //MyAPIGateway.Utilities.SendMessage("Got A packet");
            String packet = "";
            try
            {
                packet = Encoding.UTF8.GetString(rawData);
                //  MyAPIGateway.Utilities.SendMessage("Did it work?" + ":" + packet.ToString());
            }
            catch (Exception e)
            {
                //  MyAPIGateway.Utilities.SendMessage("Failed?" + ":" + e.ToString());

                Log.Error("Packet message failed:" + e.ToString());
                return;
            }

            // message must be in the following format using alt code 1234 "PrimaryGridID╥ClusterID╥ServerID╥x╥y╥z"

            string[] data = packet.Split('╥');

            if(data.Length != 6)
            {
                Log.Error("Packet message Was not in right format:" + packet);
                return;
            }

            long gridId = long.Parse(data[0]);

            int clustID = int.Parse(data[1]);

            int servID = int.Parse(data[2]);

            double x = double.Parse(data[3]);

            double y = double.Parse(data[4]);

            double z = double.Parse(data[5]);

            bool found = false;

            JumpRegion transitRegion = new JumpRegion();

            foreach(var i in Configs.JumpRegionGrid)
            {
                if(i.ClusterID == clustID && i.ServerID == servID)
                {
                    found = true;
                    transitRegion = i;
                }
            }

            if (!found)
                return;

            transitRegion.ToX = x;
            transitRegion.ToY = y;
            transitRegion.ToZ = z;


            if( !IsTargetServerOnline(transitRegion, out int gamePort) )
            {
                Log.Error("Server offline");
                return;
            }

            List<MyCubeGrid> grids = new List<MyCubeGrid>();

            MyCubeGrid mainGrid = MyAPIGateway.Entities.GetEntityById(gridId) as MyCubeGrid;

            grids = mainGrid.GetConnectedGrids(GridLinkTypeEnum.Physical);

            if (!grids.Contains(mainGrid))
                grids.Add(mainGrid);

            foreach(var i in grids)
            {
                if(i.IsStatic)
                {
                    Log.Error("Attempted to transport Static Grid" + i.DisplayName + " " + i.EntityId.ToString());
                    return;
                }
            }

            new GridTransport(grids, transitRegion, gamePort);

        }

        public static bool IsTargetServerOnline(JumpRegion Target, out int GamePort)
        {
            GamePort = 0;
            if (Target == null || Target.Client == null)
                return false;

            GamePort = Target.Client.GetGamePort();
            return Target.Client.IsConnected();
        }

    }



}

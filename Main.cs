using GridTransporter.Configs;
using GridTransporter.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.API.Managers;
using Torch.Session;
using Torch.API.Session;
using Torch.Managers;
using GridTransporter.Networking;
using GridTransporter.BoundrySystem;
using GridTransporter.Utilities;

using Sandbox.Game;
using NLog.Fluent;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;


namespace GridTransporter
{
    public class Main : TorchPluginBase, IWpfPlugin
    {
        public static Settings Config => _config?.Data;
        public static Persistent<Settings> _config;

        public static IChatManagerServer chatMngr = null;

        public UserControl _control;
        public UserControl GetControl() => _control ?? (_control = new UserControlInterface());

        // public Torch.Managers.ChatManager.ChatManagerServer ChatManager;

       

        public bool IsRunning = false;
        private Networking.Networking Socket;
        private BoundaryTask BoundryChecker;

        public override void Init(ITorchBase torch)
        {
            string path = Path.Combine(StoragePath, "GridTransporter.cfg");
            _config = Persistent<Settings>.Load(path);

            TorchSessionManager TorchSession = Torch.Managers.GetManager<TorchSessionManager>();
            if (TorchSession != null)              
                TorchSession.SessionStateChanged += TorchSession_SessionStateChanged;

          //  TorchSession.CurrentSession.Managers.GetManager<IChatManagerServer>().MessageProcessing += ChatManager_MessageProcessing;

               
           // CManager = new Torch.Managers.ChatManager.ChatManagerServer(torch);

        }

     

        private void TorchSession_SessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            IsRunning = newState == TorchSessionState.Loaded;
            switch (newState)
            {
                case TorchSessionState.Loaded:

                    IsRunning = true;
                    ServerLoaded(session);
                    break;

                case TorchSessionState.Unloading:
                    IsRunning = false;
                    PluginDispose();
                    break;
            }
        }

        private void PluginDispose()
        {
            Socket?.Close();
            BoundryChecker?.Close();
        }

        private void ServerLoaded(ITorchSession session)
        {
            PluginManager Plugins = Torch.CurrentSession.Managers.GetManager<PluginManager>();
            PluginDependencyManager.InitPluginDependencyManager(Plugins);

            Socket = new Networking.Networking();
            BoundryChecker = new BoundaryTask();

            ModMessage.Init();

            chatMngr = session.Managers.GetManager<IChatManagerServer>();
            chatMngr.MessageProcessing += ChatManager_MessageProcessing;


          //  CManager.MessageRecieved += ChatManager_MessageRecieved;
          //  CManager.MessageProcessing += ChatManager_MessageProcessing;
           
        }


        private void ChatManager_MessageProcessing(TorchChatMessage msg, ref bool consumed)
        {

           // MyVisualScriptLogicProvider.SendChatMessage("debug1", "Debugger");
            if (msg.Channel != Sandbox.Game.Gui.ChatChannel.Global || !Config.ChatRelay)
                return;

            ModMessage.Utilities_MessageRecieved(msg.Author, msg.Message);
        }

    }
}

using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using static CoreSystems.Session;

namespace CoreSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, Priority = int.MinValue)]
    internal class CombinerNetwork : MySessionComponentBase
    {
        private static CombinerNetwork _;
        private HashSet<Packet> _toServerPackets = new HashSet<Packet>(); 
        private Dictionary<ulong, HashSet<Packet>> _toClientPackets = new Dictionary<ulong, HashSet<Packet>>();

        public override void LoadData()
        {
            _ = this;

            if (MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Multiplayer.RegisterMessageHandler(ServerPacketId, ProccessServerPacket);
            else
                MyAPIGateway.Multiplayer.RegisterMessageHandler(ClientPacketId, ClientReceivedPacket);
        }

        private void ClientReceivedPacket(byte[] obj)
        {
            var packets = MyAPIGateway.Utilities.SerializeFromBinary<Packet[]>(obj);
            if (packets == null)
                return;
            foreach (var packet in packets)
                CoreSystems.Session.I.ClientReceivedPacket(packet);
        }

        private void ProccessServerPacket(byte[] obj)
        {
            var packets = MyAPIGateway.Utilities.SerializeFromBinary<Packet[]>(obj);
            if (packets == null)
                return;
            foreach (var packet in packets)
                CoreSystems.Session.I.ProccessServerPacket(packet);
        }

        public override void UpdateAfterSimulation()
        {
            SendServerQueue();
            SendClientQueue();
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ServerPacketId, ProccessServerPacket);
            else
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ClientPacketId, ClientReceivedPacket);
            _ = null;
        }

        public static void SendMessageTo(Packet message, ulong recipient)
        {
            if (_ == null)
                return;

            if (_._toClientPackets.ContainsKey(recipient))
                _._toClientPackets[recipient].Add(message);
            else
                _._toClientPackets[recipient] = new HashSet<Packet>() { message };
        }

        public static void SendMessageToServer(Packet message)
        {
            _?._toServerPackets.Add(message);
        }

        public static void SendServerQueue()
        {
            if (_ == null || _._toServerPackets.Count <= 0)
                return;
            MyModAPIHelper.MyMultiplayer.Static.SendMessageToServer(ServerPacketId, MyAPIGateway.Utilities.SerializeToBinary(_._toServerPackets.ToArray()), true);
            _._toServerPackets.Clear();
        }

        public static void SendClientQueue()
        {
            if (_ == null)
                return;

            foreach (var set in _._toClientPackets)
            {
                if (set.Value.Count == 0)
                    continue;

                MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(ClientPacketId, MyAPIGateway.Utilities.SerializeToBinary(set.Value.ToArray()), set.Key, true);

                set.Value.Clear();
            }
        }
    }
}

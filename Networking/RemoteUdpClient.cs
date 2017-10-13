using System;

namespace DivergentNetwork {

    public sealed class RemoteUdpClient {

        public DateTime TimeSinceLastReceivedDatagram { get; set; }
        public DateTime TimeCreated { get; private set; }

        public string IP { get; private set; }
        public int Port { get; private set; }

        public int NetworkID { get; private set; }
        public bool IsConnected { get; set; }

        public RemoteUdpClient(string ip, int port, int networkID, DateTime timeCreated) {
            IP = ip;
            Port = port;
            NetworkID = networkID;
            TimeCreated = timeCreated;
            TimeSinceLastReceivedDatagram = TimeCreated;
        }
    }
}

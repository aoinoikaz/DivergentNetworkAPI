using System;

namespace DivergentNetwork
{

    public sealed class DatagramReceivedEventArgs : EventArgs
    {

        public DateTime TimeReceived { get; private set; }

        public string IP { get; private set; }
        public int Port { get; private set; }

        public ushort ProtocolID { get; private set; }
        public ushort OpCode { get; private set; }

        public byte[] Data { get; private set; }
        public int BytesDelivered { get; private set; }

        public DatagramReceivedEventArgs(string ip, int port, ushort protocolID, ushort opCode, byte[] data, int bytesDelivered, DateTime timeReceived)
        {
            IP = ip;
            Port = port;
            ProtocolID = protocolID;
            OpCode = opCode;
            Data = data;
            BytesDelivered = bytesDelivered;
            TimeReceived = timeReceived;
        }
    }
}
using System;
using System.IO;

namespace DivergentNetwork {

    public abstract class SendPacket {

        protected byte[] Buffer = null;
        protected object WriteLock = new object();

        // This is used to generically write data to a packet
        public abstract void Write(BinaryWriter writer);

        // This is used for sending from server to client
        public void Send(DnlUdpClient server, RemoteUdpClient receiver) {
            
            if (server == null) {
                throw new NullReferenceException("DsnTcpClient cannot be null. Ensure a valid connection has been initialized.");
            }
            if (receiver == null) {
                throw new NullReferenceException("RemoteUdpClient cannot be null. Ensure a valid remote client has been initialized.");
            }
            if (!OperationCodes.SendPacket.ContainsKey(GetType()) || OperationCodes.SendPacket.Count < 0) {
                throw new Exception("This instance type is unrecognized in DsnOperationCodes. Register it on both client and server side (if applicable) for this packet to be sent.");
            }

            MemoryStream stream = null;
            BinaryWriter writer = null;

            lock (WriteLock) {
                if (Buffer == null) {
                    stream = new MemoryStream();
                    using (writer = new BinaryWriter(stream)) {
                        if (OperationCodes.SendPacket.Count > 0) {
                            writer.Write(server.ProtocolId);
                            writer.Write(OperationCodes.SendPacket[GetType()]);
                        }
                        Write(writer);
                    }
                    Buffer = new byte[stream.GetBuffer().Length];
                    Buffer = stream.ToArray();
                }
            }
            server.Send(Buffer, Buffer.Length, receiver);
        }


        // This is used for sending from client to server
        public void Send(DnlUdpClient client) {

            if (client == null) {
                throw new NullReferenceException("DsnTcpClient cannot be null. Ensure a valid connection has been initialized.");
            }
            if (!OperationCodes.SendPacket.ContainsKey(GetType()) || OperationCodes.SendPacket.Count < 0) {
                throw new Exception("This instance type is unrecognized in DsnOperationCodes. Register it on both client and server side (if applicable) for this packet to be sent.");
            }

            MemoryStream stream = null;
            BinaryWriter writer = null;

            // Lock the sending thread to ensure we're not already trying to send from this resource
            lock (WriteLock) {
                if (Buffer == null) {
                    stream = new MemoryStream();
                    using (writer = new BinaryWriter(stream)) {
                        if (OperationCodes.SendPacket.Count > 0) {
                            writer.Write(client.ProtocolId);
                            writer.Write(OperationCodes.SendPacket[GetType()]);
                        }
                        Write(writer);
                    }
                    Buffer = new byte[stream.GetBuffer().Length];
                    Buffer = stream.ToArray();
                }
            }
            client.Send(Buffer, Buffer.Length);
        }
    }
}

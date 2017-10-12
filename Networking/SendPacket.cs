using System;
using System.IO;

namespace DivergentNetwork {

    public abstract class SendPacket {

        protected byte[] Buffer = null;
        protected object WriteLock = new object();

        // This is used to generically write data to a packet
        public abstract void Write(BinaryWriter writer);

        public void Send(DnlUdpClient client) {

            MemoryStream stream = null;
            BinaryWriter writer = null;

            // Ensure a valid client is trying to send data
            if (client == null) {
                throw new NullReferenceException("DsnTcpClient cannot be null. Ensure a valid connection has been initialized.");
            }

            // If the operation codes don't contain the initialized key or there's no initialized packets
            if (!OperationCodes.SendPacket.ContainsKey(GetType()) || OperationCodes.SendPacket.Count < 0) {
                throw new Exception("This instance type is unrecognized in DsnOperationCodes. Register it on both client and server side (if applicable) for this packet to be sent.");
            }

            // Lock the sending thread to ensure we're not already trying to send from this resource
            lock (WriteLock) {
                if (Buffer == null) {
                    stream = new MemoryStream();
                    using (writer = new BinaryWriter(stream)) {
                        if (OperationCodes.SendPacket.Count > 0) {
                            writer.Write(client.ProtocolId);
                            writer.Write(OperationCodes.SendPacket[GetType()]);
                        }
                        // Pass the writer to the callback method
                        Write(writer);
                    }
                    // Initialize the buffer to the size of the amount of data we wrote to the packet
                    Buffer = new byte[stream.GetBuffer().Length];

                    // Convert the memory stream to byte array
                    Buffer = stream.ToArray();
                }
            }
            // Use the client interface to send the packet back
            client.Send(Buffer, Buffer.Length);
        }
    }
}

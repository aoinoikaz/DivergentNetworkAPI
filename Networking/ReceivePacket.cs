using System;
using System.IO;

namespace DivergentNetwork {

    public abstract class ReceivePacket {

        // Reference to the current client and reader to 
        protected DnlUdpPeer Client;
        protected BinaryReader Reader;

        // These methods will be called when trying to read/process the developers base packet class
        public abstract void Read();
        public abstract void Process();
        public abstract void Process(RemoteUdpPeer remoteClient);

        // This process method is for processing packets from remote clients
        public void Process(DnlUdpPeer client, RemoteUdpPeer sender, byte[] buffer) {

            if (client == null) { throw new ArgumentNullException("Client"); }
            if (buffer == null) { throw new NullReferenceException("No data to process. Ensure a valid byte array has been provided."); }

            Client = client ?? throw new NullReferenceException("DsnTcpClient cannot be null. Ensure a valid connection has been initialized.");

            MemoryStream stream = new MemoryStream(buffer);
            using (Reader = new BinaryReader(stream)) {
                Read();
            }

            // Now try to process the packet based on developers needs
            Process(sender);
        }


        // This process method is for processing packets from remote clients
        public void Process(DnlUdpPeer client, byte[] buffer) {

            if (client == null) { throw new ArgumentNullException("Client"); }
            if (buffer == null) { throw new NullReferenceException("No data to process. Ensure a valid byte array has been provided."); }

            Client = client ?? throw new NullReferenceException("DsnTcpClient cannot be null. Ensure a valid connection has been initialized.");

            MemoryStream stream = new MemoryStream(buffer);
            using (Reader = new BinaryReader(stream)) {
                Read();
            }

            // Now try to process the packet based on developers needs
            Process();
        }
    }
}

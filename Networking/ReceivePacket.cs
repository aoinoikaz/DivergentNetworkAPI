using System;
using System.IO;

namespace DivergentNetwork {

    public abstract class ReceivePacket {

        // Reference to the current client and reader to 
        protected DnlUdpClient Client;
        protected BinaryReader Reader;

        // These methods will be called when trying to read/process the developers base packet class
        public abstract void Read();
        public abstract void Process(RemoteUdpClient remoteClient);

        // This process method is for processing packets from server
        public void Process(DnlUdpClient client, byte[] buffer) {

            if (client == null) {
                throw new ArgumentNullException("Client");
            }
            if (buffer == null) {
                throw new NullReferenceException("No data to process. Ensure a valid byte array has been provided.");
            }

            Client = client ?? throw new NullReferenceException("DsnTcpClient cannot be null. Ensure a valid connection has been initialized.");

            MemoryStream stream = new MemoryStream(buffer);
            using (Reader = new BinaryReader(stream)) {
                // Call the abstract read method in the developers base packet class
                Read();
            }

            // Now try to process the packet based on developers needs
            Process(null);
        }


        // This process method is for processing packets from remote clients
        public void Process(DnlUdpClient client, RemoteUdpClient receiver, byte[] buffer) {

            if (client == null) {
                throw new ArgumentNullException("Client");
            }
            if (buffer == null) {
                throw new NullReferenceException("No data to process. Ensure a valid byte array has been provided.");
            }

            Client = client ?? throw new NullReferenceException("DsnTcpClient cannot be null. Ensure a valid connection has been initialized.");

            MemoryStream stream = new MemoryStream(buffer);
            using (Reader = new BinaryReader(stream))
            {
                // Call the abstract read method in the developers base packet class
                Read();
            }

            // Now try to process the packet based on developers needs
            Process(receiver);
        }
    }
}

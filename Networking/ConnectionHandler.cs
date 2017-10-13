using System;

namespace DivergentNetwork {

    public static class ConnectionHandler {

        public static int MaxNumberOfConnections = 100;
        public static int CurrentlyConnectedClients { get; private set; }

        public static RemoteUdpClient[] RemoteClients = new RemoteUdpClient[MaxNumberOfConnections];

        private static object connectionLock = new object();

        // This client adds a new client and returns the new client id
        public static void Add(RemoteUdpClient remoteClient, int networkId) {

            if (remoteClient == null) {
                throw new ArgumentNullException("RemoteUdpClient", "RemoteUdpClient cannot be null.");
            }
            if (networkId < 0) {
                throw new ArgumentOutOfRangeException("NetworkID", "NetworkID must be greater than 0.");
            }

            lock (connectionLock) {
                RemoteClients[networkId] = remoteClient;
                CurrentlyConnectedClients++;
            }
        }


        // This function finds a free slot for a client to join the network (if any are free)
        // Returns -1 if no spot is available
        public static int GetAvailableNetworkIndex() {

            lock (connectionLock) {
                for (int i = 0; i < MaxNumberOfConnections; i++) {
                    if (RemoteClients[i] == null) {
                        return i;
                    }
                }
                // Return -1 if server is full
                return -1;
            }
        }

        // This simply searches for a client by address and returns it's client index
        // Returns -1 if the client is new
        public static int FindExistingConnectionIndex(string ip, int port) {
        
            if (string.IsNullOrEmpty(ip)) {
                throw new ArgumentNullException("RemoteUdpClient", "RemoteUdpClient cannot be null");
            }

            lock (connectionLock) {
                for (int i = 0; i < MaxNumberOfConnections; i++) {
                    if (RemoteClients[i] != null && RemoteClients[i].IP == ip && RemoteClients[i].Port == port) {
                        return i;
                    }
                }
                return -1;
            }
        }


        // This function determines whether or not an existing connection has timed out
        public static bool HasConnectionTimedOut(RemoteUdpClient remoteClient, int timeOut) {

            if (remoteClient == null) {
                throw new ArgumentNullException("RemoteUdpClient", "RemoteUdpClient cannot be null");
            }

            lock (connectionLock) {
                bool timedOut = false;

                // Get the elapsed time: current time - time since last received
                TimeSpan elapsedSpan = new TimeSpan(DateTime.Now.Ticks - remoteClient.TimeSinceLastReceivedDatagram.Ticks);
                int elapsedSeconds = elapsedSpan.Seconds;

                // If the elapsed time is less than the TimeOut value then we know the connection is still alive
                if (elapsedSeconds > timeOut) {
                    timedOut = true;
                }
                return timedOut;
            }
        }


        // Simply removes a virtual udp connection from the handler
        public static void RemoveConnection(int index) {

            lock (connectionLock) {
                RemoteClients[index] = null;
                CurrentlyConnectedClients--;
            }
        }


        // Simply determines if a client is connected or not
        public static bool IsClientConnected(int networkID) => RemoteClients[networkID].IsConnected;

        // Returns the address associated with it's client index
        public static RemoteUdpClient GetClient(int clientIndex) => RemoteClients[clientIndex];
    }
}

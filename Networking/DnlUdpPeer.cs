using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

using DivergentNetwork.Tools;

namespace DivergentNetwork
{
    public class DnlUdpPeer
    {
        // Internally used
        private int isSending;
        private int maxBufferSize;
        private byte[] receiveBuffer;
        private Queue<DnlUdpSendSegment> sendSegments;

        // Properties 
        public int ClientID { get; private set; }
        public int ConnectionTimeout { get; private set; }
        public bool IsListening { get; private set; }
        public bool IsServer { get; private set; }

        // TODO: implement reliability and congestion system
        //public QosType QosType { get; private set; }

        // The 2 byte identifier that is used internally for sending/receiving datagrams
        public ushort ProtocolId { get; private set; }

        // Socket that will be used for listening and for sending
        public Socket Socket { get; private set; }
        public IPEndPoint EndPoint { get; private set; }

        // Event handler for receiving datagrams
        public event EventHandler<DatagramReceivedEventArgs> OnDatagramReceived;

        // Construct as 'server' object
        public DnlUdpPeer(int port, ushort protocolId, int connectionTimeout, int bufferSize)
        {
            IsServer = true;
            ConnectionTimeout = connectionTimeout;
            maxBufferSize = bufferSize;
            ProtocolId = protocolId;
            receiveBuffer = new byte[maxBufferSize];
            sendSegments = new Queue<DnlUdpSendSegment>();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //Socket.Blocking = false;
            EndPoint = new IPEndPoint(IPAddress.Any, port);
            Socket.Bind(EndPoint);
            //QosType = qosType;
        }


        // Construct a 'client' object to a specific end point
        public DnlUdpPeer(string ip, int port, ushort protocolId, int bufferSize)
        {
            IsServer = false;
            ProtocolId = protocolId;
            maxBufferSize = bufferSize;
            receiveBuffer = new byte[maxBufferSize];
            sendSegments = new Queue<DnlUdpSendSegment>();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //Socket.Blocking = false;
            EndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            //QosType = qosType;
        }


        // This method starts listening 
        public void Start()
        {
            if (IsListening)
                return;

            IsListening = true;

            BeginReceive(); 
        }


        public void Stop()
        {
            if (!IsListening)
                return;

            IsListening = false;

            try
            {
                Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                DnlDebugger.LogMessage("Exception caught when trying to shut down listener: " + e.Message, true);
            }
            finally
            {
                Socket.Close();
            }
        }


        private void BeginReceive()
        {
            // Setup our socket callback args
            SocketAsyncEventArgs args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = EndPoint
            };
            args.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            args.Completed += OnOperationComplete;

            bool willRaiseEvent = Socket.ReceiveFromAsync(args);

            // If this completes synchronously, then trigger the event manually
            if (!willRaiseEvent)
            {
                BeginReceiveCallback(this, args);
            }
        }


        private void BeginReceiveCallback(object sender, SocketAsyncEventArgs args)
        {
            if (!IsListening)
            {
                return;
            }

            try
            {
                // No bytes been transferred from this async operation
                if (args.BytesTransferred <= 0)
                {
                    DnlDebugger.LogMessage("0 bytes transferred from receive operation: " + args.RemoteEndPoint.ToString() + " | " +  args.BytesTransferred, true);
                }
                else
                {
                    ushort protocolId;
                    ushort operationCode;

                    // Parse and validate protocol id and operation code
                    if ((protocolId = unchecked(BitConverter.ToUInt16(args.Buffer, 0))) != ProtocolId)
                    {
                        throw new Exception("Received datagram with different protocol id then expected. Either set your protocol id or ensure network traffic is from known clients.");
                    }

                    if (!OperationCodes.ReceivePacket.ContainsKey(operationCode = unchecked(BitConverter.ToUInt16(args.Buffer, 2))))
                    {
                        throw new Exception("Received datagram with different unregistered operation code. Either register your operation packet handler or ensure network traffic is from known clients.");
                    }

                    int payloadLength = args.BytesTransferred - (sizeof(ushort) * 2);
                    byte[] payload = new byte[payloadLength];
                    
                    // Parse payload from datagram
                    Buffer.BlockCopy(args.Buffer, 4, payload, 0, payloadLength);

                    // Parse ip and port from endpoint string
                    string[] systemAddress = !string.IsNullOrEmpty(args.RemoteEndPoint.ToString()) ? 
                        args.RemoteEndPoint.ToString().Split(':') :
                        throw new Exception("Internal issue trying to parse remote endpoint from incoming datagram. Contact support");

                    // Invoke the on received event
                    OnDatagramReceived?.Invoke(this, new DatagramReceivedEventArgs(
                        systemAddress[0],
                        int.Parse(systemAddress[1]),
                        protocolId,
                        operationCode,
                        payload,
                        payload.Length,
                        DateTime.Now));
                }
            }
            catch (Exception e)
            {
                DnlDebugger.LogMessage("Caught exception in EndReceive: " + e.Message + " | " + e.TargetSite.ToString(), true);
            }
            finally
            {
                args.Dispose();
                BeginReceive();
            }
        }


        // Used for sending from client to server
        public void Send(byte[] data, int length)
        {
            // Ensure only client instance can use this method
            if (IsServer)
            {
                return;
            }

            // Create a new byte array segment with the byte array data we want to send
            sendSegments.Enqueue(new DnlUdpSendSegment(data));

            // Ensure we aren't already trying to send data
            if (Interlocked.CompareExchange(ref isSending, 1, 0) == 0)
            {
                BeginSend();
            }
        }


        // Use for sending from server to client
        public void Send(byte[] data, int length, RemoteUdpPeer remoteUdpClient)
        {
            if (!IsServer)
            {
                return;
            }

            // Create a new byte array segment with the byte array data we want to send
            sendSegments.Enqueue(new DnlUdpSendSegment(data, remoteUdpClient));

            // Ensure we aren't already trying to send data
            if (Interlocked.CompareExchange(ref isSending, 1, 0) == 0)
            {
                BeginSend();
            }
        }


        // This is called after the byte array segment has been queued
        // When null is passed as the client, it means a client user is using this instance class
        // Otherwise this server instance is sending to a client
        private void BeginSend()
        {
            bool isClient = !IsServer;

            // Setup the async socket event args that will be used to handle callbacks and send/receive data 
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();

            // Declare a byte array segment
            DnlUdpSendSegment segment;

            // Fill the segment with the data we want to send
            if ((segment = sendSegments.Peek()) != null)
            {
                // Setup our event args callback
                args.Completed += OnOperationComplete;
                args.RemoteEndPoint = isClient ? EndPoint : new IPEndPoint(IPAddress.Parse(segment.Peer.IP), segment.Peer.Port);
                args.SetBuffer(segment.Buffer, segment.Start, segment.Length);
                bool willRaiseEvent = Socket.SendToAsync(args);
                
                // Asynchronously send the data to the socket - returns true if request is still pending
                if (!willRaiseEvent)
                {
                    // Clean up after sending the data
                    BeginSendCallback(this, args);
                }
            }
        }


        // This is called after the byte array segments have been successfully sent to the connected socket
        private void BeginSendCallback(object sender, SocketAsyncEventArgs args)
        {
            try
            {
                if (args.BytesTransferred <= 0)
                {
                    DnlDebugger.LogMessage("0 bytes transferred in the last send operation", true);
                    return;
                }

                // Another temp holder
                DnlUdpSendSegment segment;

                // If theres more segments
                if ((segment = sendSegments.Peek()) != null)
                {
                    // If the current segment has been advanced
                    if (segment.Advance(args.BytesTransferred))
                    {
                        sendSegments.Dequeue();
                    }
                    // Recursively send the data if there's more segments to send
                    if (sendSegments.Count > 0)
                    {
                        BeginSend();
                    }
                    else
                    {
                        // Set this thread's sending state to idle
                        isSending = 0;
                    }
                }
            }
            catch (Exception e)
            {
                DnlDebugger.LogMessage("Caught exception in EndSendCallback: " + e.Message + " | " + e.TargetSite.ToString(), true);
            }
            finally
            {
                args.Dispose();
            }
        }


        // This is called automatically when a read/write operation completes
        // on a socket
        private void OnOperationComplete(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    BeginReceiveCallback(this, e);
                    break;
                case SocketAsyncOperation.SendTo:
                    BeginSendCallback(this, e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send.");
            }
        }
    }
}

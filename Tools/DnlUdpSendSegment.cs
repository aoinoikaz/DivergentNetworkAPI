using System;

namespace DivergentNetwork.Tools {

    public sealed class DnlUdpSendSegment : IDisposable {

        // The byte array segments buffer
        public byte[] Buffer { get; private set; }
        
        // Start index of the segment
        public int Start { get; private set; }
        
        // How many bytes are in the segment
        public int Length { get; private set; }

        // The udp peer corresponding with this segment
        public RemoteUdpPeer Peer { get; private set; }

        // Concstructs a new byte array segment
        public DnlUdpSendSegment(byte[] buffer) {
            Start = 0;
            Buffer = buffer;
            Length = Buffer.Length;
        }


        // Concstructs a new byte array segment
        public DnlUdpSendSegment(byte[] buffer, RemoteUdpPeer peer) {
            Start = 0;
            Buffer = buffer;
            Length = Buffer.Length;
            Peer = peer;
        }


        // Constructs a new byte array segment that start offset and length
        public DnlUdpSendSegment(byte[] buffer, int start, int length) {

            if (start + length > buffer.Length) {
                throw new ArgumentOutOfRangeException("length", "The segment doesn't fit the array bounds.");
            }

            Buffer = buffer;
            Start = start;
            Length = length;
        }


        // Advance to the new byte array segment
        public bool Advance(int pLength) {
            Start += pLength;
            Length -= pLength;
            return Length <= 0;
        }


        // Dispose implementation
        public void Dispose() => Dispose();
    }
}
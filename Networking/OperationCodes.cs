using System;
using System.Collections.Generic;

namespace DivergentNetwork
{

    // This entire class is used to determine if a developer will be sending byte array segments with or without segment headers -
    // the purpose for this is used for packet identification, security and abstraction. If the developer does not add any operation 
    // codes to the system, the API will use the default value for operation headers. Developers may want to do this if they are
    // sending packets that don't need complex routing management.
    public sealed class OperationCodes
    {

        // Dictionaries that store user-set operation codes - this api uses these automatically regardless of if they are set or not
        public static Dictionary<ushort, Type> ReceivePacket = new Dictionary<ushort, Type>();
        public static Dictionary<Type, ushort> SendPacket = new Dictionary<Type, ushort>();

        // This is the default operation code - the api uses this value internally if the developer does not specify any operation codes
        public static ushort Unregistered = 0x0001;

        // This function simply adds an operation code to the system
        public static void AddOperationCode(ushort opCode, Type packetType, bool isReceive)
        {
            if (isReceive)
            {
                ReceivePacket.Add(opCode, packetType);
            }
            else
            {
                SendPacket.Add(packetType, opCode);
            }
        }
    }
}
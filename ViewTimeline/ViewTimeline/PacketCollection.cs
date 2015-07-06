using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViewTimeline
{
    public class Packet
    {
        public Packet(DateTime arrivalTime, int originalLength, byte[] payload)
        {
            Payload = payload;
            OriginalLength = originalLength;
            RecordedLength = payload.Length;
            ArrivalTime = arrivalTime;
        }

        public byte[] Payload { get; private set; }

        public DateTime ArrivalTime { get; private set; }

        public int OriginalLength { get; private set; }
        public int RecordedLength { get; private set; }
    }

    public class PacketCollection
    {
        private readonly List<Packet> _packetCollection;

        public PacketCollection()
        {
            _packetCollection = new List<Packet>();
        }

        public void AddPacket(Packet packet)
        {
            if (packet != null) _packetCollection.Add(packet);
        }

        public void Merge(PacketCollection collection)
        {
            foreach (var packet in collection.Collection)
            {
                _packetCollection.Add(packet);
            }
        }

        public List<Packet> Collection { get { return _packetCollection; } }
    }
}
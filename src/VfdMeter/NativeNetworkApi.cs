using System.Runtime.InteropServices;

namespace VfdMeter;

internal readonly record struct NetworkInterfaceCounters(
    ulong ReceivedBytes,
    ulong SentBytes);

internal static class NativeNetworkApi
{
    private const uint NoError = 0;
    private const uint IfTypeSoftwareLoopback = 24;
    private const uint IfOperStatusUp = 1;
    private const int TableEntriesOffset = 8;

    public static unsafe bool TryGetActiveInterfaceCounters(
        out Dictionary<Guid, NetworkInterfaceCounters> counters)
    {
        counters = [];
        nint table = 0;

        try
        {
            if (GetIfTable2(out table) != NoError || table == 0)
            {
                return false;
            }

            var entryCount = *(uint*)table;
            var rowSize = sizeof(MibIfRow2);
            var firstRow = (byte*)table + TableEntriesOffset;

            for (uint index = 0; index < entryCount; index++)
            {
                var row = (MibIfRow2*)(firstRow + (nuint)index * (nuint)rowSize);
                if (row->OperStatus != IfOperStatusUp || row->Type == IfTypeSoftwareLoopback)
                {
                    continue;
                }

                counters[row->InterfaceGuid] = new NetworkInterfaceCounters(
                    row->InOctets,
                    row->OutOctets);
            }

            return true;
        }
        catch (Exception exception) when (
            exception is not OutOfMemoryException and
            not StackOverflowException and
            not AccessViolationException)
        {
            counters.Clear();
            return false;
        }
        finally
        {
            if (table != 0)
            {
                FreeMibTable(table);
            }
        }
    }

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetIfTable2(out nint table);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern void FreeMibTable(nint memory);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private unsafe struct MibIfRow2
    {
        public ulong InterfaceLuid;
        public uint InterfaceIndex;
        public Guid InterfaceGuid;
        public fixed char Alias[257];
        public fixed char Description[257];
        public uint PhysicalAddressLength;
        public fixed byte PhysicalAddress[32];
        public fixed byte PermanentPhysicalAddress[32];
        public uint Mtu;
        public uint Type;
        public uint TunnelType;
        public uint MediaType;
        public uint PhysicalMediumType;
        public uint AccessType;
        public uint DirectionType;
        public byte InterfaceAndOperStatusFlags;
        public uint OperStatus;
        public uint AdminStatus;
        public uint MediaConnectState;
        public Guid NetworkGuid;
        public uint ConnectionType;
        public ulong TransmitLinkSpeed;
        public ulong ReceiveLinkSpeed;
        public ulong InOctets;
        public ulong InUcastPkts;
        public ulong InNUcastPkts;
        public ulong InDiscards;
        public ulong InErrors;
        public ulong InUnknownProtos;
        public ulong InUcastOctets;
        public ulong InMulticastOctets;
        public ulong InBroadcastOctets;
        public ulong OutOctets;
        public ulong OutUcastPkts;
        public ulong OutNUcastPkts;
        public ulong OutDiscards;
        public ulong OutErrors;
        public ulong OutUcastOctets;
        public ulong OutMulticastOctets;
        public ulong OutBroadcastOctets;
        public ulong OutQLen;
    }
}

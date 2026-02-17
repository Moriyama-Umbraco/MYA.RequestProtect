using System.Net;
using System.Runtime.CompilerServices;

namespace MYA.RequestProtect.Models;

internal readonly struct WhitelistEntry
{
    public readonly bool IsCidr;
    public readonly IPNetwork? Network;
    public readonly IPAddress? DirectIp;
    public readonly string Pattern;

    public WhitelistEntry(string pattern, bool isCidr, IPNetwork? network, IPAddress? directIp)
    {
        Pattern = pattern;
        IsCidr = isCidr;
        Network = network;
        DirectIp = directIp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Matches(IPAddress ip) => IsCidr
        ? Network?.Contains(ip) == true
   : DirectIp?.Equals(ip) == true;
}

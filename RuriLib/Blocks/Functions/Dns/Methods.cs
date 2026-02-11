using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace RuriLib.Blocks.Functions.DnsFunctions
{
    [BlockCategory("DNS Functions", "Blocks for DNS lookups", "#4fc3f7")]
    public static class Methods
    {
        [Block("Resolves a hostname to a list of IP addresses")]
        public static List<string> DnsResolve(BotData data, string hostname)
        {
            var addresses = Dns.GetHostAddresses(hostname).Select(a => a.ToString()).ToList();

            data.Logger.LogHeader();
            data.Logger.Log($"Resolved {hostname} to {addresses.Count} addresses:", LogColors.YellowGreen);
            foreach (var addr in addresses)
            {
                data.Logger.Log($"  {addr}", LogColors.YellowGreen);
            }

            return addresses;
        }

        [Block("Resolves a hostname to a list of IPv4 addresses only")]
        public static List<string> DnsResolveIPv4(BotData data, string hostname)
        {
            var addresses = Dns.GetHostAddresses(hostname)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.ToString())
                .ToList();

            data.Logger.LogHeader();
            data.Logger.Log($"Resolved {hostname} to {addresses.Count} IPv4 addresses:", LogColors.YellowGreen);
            foreach (var addr in addresses)
            {
                data.Logger.Log($"  {addr}", LogColors.YellowGreen);
            }

            return addresses;
        }

        [Block("Resolves a hostname to a list of IPv6 addresses only")]
        public static List<string> DnsResolveIPv6(BotData data, string hostname)
        {
            var addresses = Dns.GetHostAddresses(hostname)
                .Where(a => a.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(a => a.ToString())
                .ToList();

            data.Logger.LogHeader();
            data.Logger.Log($"Resolved {hostname} to {addresses.Count} IPv6 addresses:", LogColors.YellowGreen);
            foreach (var addr in addresses)
            {
                data.Logger.Log($"  {addr}", LogColors.YellowGreen);
            }

            return addresses;
        }

        [Block("Performs a reverse DNS lookup on an IP address to get the hostname")]
        public static string DnsReverseLookup(BotData data, string ipAddress)
        {
            var entry = Dns.GetHostEntry(ipAddress);
            var hostname = entry.HostName;

            data.Logger.LogHeader();
            data.Logger.Log($"Reverse lookup for {ipAddress}: {hostname}", LogColors.YellowGreen);

            return hostname;
        }

        [Block("Gets the hostname of the local machine")]
        public static string DnsGetHostName(BotData data)
        {
            var hostname = Dns.GetHostName();

            data.Logger.LogHeader();
            data.Logger.Log($"Local hostname: {hostname}", LogColors.YellowGreen);

            return hostname;
        }
    }
}

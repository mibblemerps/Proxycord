using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Proxycord
{
    public class ProxyRule
    {
        public List<ProxyEndpoint> Endpoints = new List<ProxyEndpoint>();
        public List<string> Hostnames = new List<string>();
        public List<int> ProtocolVersions = new List<int>();

        public bool DoesMatch(string hostname, int protocolVersion)
        {
            if (Hostnames.Count > 0 && !Hostnames.Contains(hostname.Trim().ToLower()))
                return false;
            if (ProtocolVersions.Count > 0 && !ProtocolVersions.Contains(protocolVersion))
                return false;

            return true;
        }

        public class ProxyEndpoint
        {
            public IPEndPoint Endpoint;

            public string DnsHostname;

            public int Port
            {
                get
                {
                    if (Endpoint != null)
                        return Endpoint.Port;

                    int port = 25565;
                    if (DnsHostname.Contains(':'))
                        int.TryParse(DnsHostname.Split(':')[1], out port);
                    return port;
                }
            }

            public ProxyEndpoint(IPEndPoint endpoint)
            {
                Endpoint = endpoint;
            }

            public ProxyEndpoint(string dnsHostname)
            {
                DnsHostname = dnsHostname;
            }

            public async Task<IPEndPoint> GetEndpoint()
            {
                if (Endpoint != null)
                    return Endpoint;

                string[] endpointParts = DnsHostname.Split(':');
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(endpointParts[0]);
                int port = 25565;
                if (endpointParts.Length >= 2)
                    int.TryParse(endpointParts[1], out port);
                Endpoint = new IPEndPoint(addresses.First(), port);
                return Endpoint;
            }

            public override string ToString()
            {
                return Endpoint == null ? DnsHostname : Endpoint.ToString();
            }
        }
    }
}

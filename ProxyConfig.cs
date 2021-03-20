using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Proxycord
{
    public class ProxyConfig
    {
        public string Path = "proxy.rules";

        public List<ProxyRule> Rules;

        public void Load()
        {
            Rules = new List<ProxyRule>();

            if (!File.Exists(Path))
            {
                Log.Info("No proxy config file exists - creating one.");
                CreateEmptyConfig();
                return;
            }

            string[] lines = File.ReadAllLines(Path);
            foreach (string line in lines)
            {
                string trimmed = line.Trim()
                    .Replace('\t', ' ') // replace tabs with spaces so they'll be split correctly
                    .Replace(",", ", "); // ensure commas have spaces after them so they'll be split correctly
                if (trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                    continue; // Comment

                List<string> parts = trimmed.Split(' ').ToList();
                if (parts.Count < 1)
                    continue;

                var rule = new ProxyRule();

                // Parse endpoints
                foreach (string part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part))
                        continue;

                    try
                    {
                        // Try and add raw IP endpoint (<ip>:<port>)
                        string partWithPort = part.Trim(',', ' ');
                        if (!partWithPort.Contains(":"))
                            partWithPort += ":25565";

                        rule.Endpoints.Add(new ProxyRule.ProxyEndpoint(IPEndPoint.Parse(partWithPort)));
                    }
                    catch
                    {
                        // Just add as a DNS endpoint. This will get resolved when the user tries to connect.
                        rule.Endpoints.Add(new ProxyRule.ProxyEndpoint(part.Trim().ToLower()));
                    }

                    if (!part.EndsWith(","))
                        break; // No comma means no more endpoints
                }
                parts.RemoveRange(0, rule.Endpoints.Count);

                if (rule.Endpoints.Count == 0)
                    continue; // No endpoints means pointless rule

                foreach (string part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part))
                        continue;

                    if (int.TryParse(part, out int intResult))
                    {
                        // Protocol version rule
                        rule.ProtocolVersions.Add(intResult);
                    }
                    else
                    {
                        // Hostname rule
                        rule.Hostnames.Add(part.Trim().ToLower());
                    }
                }

                Rules.Add(rule);
            }
        }

        private void CreateEmptyConfig()
        {
            File.WriteAllText(Path, "# Proxycord Rules\r\n" +
                                    "# <endpoint> <rules>\r\n" +
                                    "# Valid rules are a hostname, or a Minecraft protocol version. (https://wiki.vg/Protocol_version_numbers)\r\n" +
                                    "# Rules are scanned top to bottom, clients are connected to the first match. If an endpoint is down, the proxy server will try the next one that matches (if any).\r\n\r\n" +
                                    "127.0.0.1:25566 localhost\r\n");
        }
    }
}

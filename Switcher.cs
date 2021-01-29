using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;

namespace ItekiSwitcher
{
    #region Errors
#if !ONLINE_SERVERS && !FALLBACK
#error ItekiSwitcher can't compile: You need to define a proper way to get the server IP: either you use the FALLBACK method, or you use ONLINE_SERVERS(from JSON, you can back it up with FALLBACK).
#endif
    #endregion

    class Switcher
    {
        Hosts hosts = new Hosts();
        public event EventHandler<SwitcherMessageEvent> OnSwitcherMessage;
        CertificateManager certificateManager;
        internal List<HostEntry> serverEntries = new List<HostEntry>();
#if ONLINE_SERVERS
        internal bool serverSuccess = false;
#endif
        public SwitcherServer onCurrentServer = SwitcherServer.Bancho;

#if FALLBACK
        internal string[] targetFallover = {
            "c.ppy.sh",
            "c4.ppy.sh",
            "c5.ppy.sh",
            "c6.ppy.sh",
            "ce.ppy.sh",
            "i.ppy.sh",
            "delta.ppy.sh",
            "a.ppy.sh",
            "s.ppy.sh"
        };
#endif

        public void CertificateSetup()
        {
            if (certificateManager == null) certificateManager = new CertificateManager {
                ServerCertificate = new X509Certificate2(Properties.Resources.iteki)
            };
            if(!certificateManager.checkForCertificate())
                certificateManager.installCertificate();
        }

        public string GetSwitchToText()
        {
            if (onCurrentServer == SwitcherServer.Private) return "Bancho";
            return BuildInfo.ServerName;
        }

        internal async Task PerformSwitch()
        {
            OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
            {
                eventType = SwitcherEvent.ServerSwitchInProgress,
                server = onCurrentServer,
#if ONLINE_SERVERS
                serverFailure = !serverSuccess
#else
#if FALLBACK
                serverFailure = true
#else
                serverFailure = false
#endif
#endif
            });

            CertificateSetup();

            if(!certificateManager.checkForCertificate())
            {
                HostsCheck();
                return;
            }

            if (onCurrentServer == SwitcherServer.Private)
            {
                foreach (HostEntry a in serverEntries)
                    hosts.hostEntries.RemoveAll(x => x.targetDomain == a.targetDomain);
            } else {
                foreach (HostEntry a in serverEntries)
                {
                    if (hosts.hostEntries.Exists(x => x.targetDomain == a.targetDomain))
                        hosts.hostEntries.Find(x => x.targetDomain == a.targetDomain).ipAddress = a.ipAddress;
                    else
                        hosts.hostEntries.Add(a);
                }
            }

            hosts.Save();
            HostsCheck();
        }

#if ONLINE_SERVERS
        internal async Task PerformServerConnection()
        {
            var webClient = new WebClient();
            string serverOutput = webClient.DownloadString(BuildInfo.SwitcherServerList + "?" + new Random().Next());

            JToken token = JObject.Parse(serverOutput);

            string target = (string) token.SelectToken("target");
            if(target == null || !target.Equals("Iteki"))
            {
                OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
                {
                    eventType = SwitcherEvent.ServerError
                });
                return;
            }

            JToken data = token.SelectToken("data");
            if (data == null)
            {
                OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
                {
                    eventType = SwitcherEvent.ServerError
                });
                return;
            }

            JToken servers = data.SelectToken("servers");
            if(servers == null)
            {
                OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
                {
                    eventType = SwitcherEvent.ServerError
                });
                return;
            }

            foreach(JToken s in servers)
            {
                string host = (string) s.SelectToken("host");
                string targetIP = (string) s.SelectToken("newTarget");

                serverEntries.Add(new HostEntry()
                {
                    ipAddress = targetIP,
                    targetDomain = host
                });
            }

            serverSuccess = true;
        }
#endif

        internal async Task HostsCheck()
        {
            OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
            {
                eventType = SwitcherEvent.PleaseWait,
#if ONLINE_SERVERS
                serverFailure = !serverSuccess
#else
#if FALLBACK
                serverFailure = true
#else
                serverFailure = false
#endif
#endif
            });

            await hosts.Parse();

            // check if we are switched to a server. search for known DNS-es
            List<HostEntry> knownHosts = hosts.hostEntries.FindAll(host => serverEntries.Exists(s => s.targetDomain == host.targetDomain));
            List<HostEntry> ourServer = knownHosts.FindAll(known => serverEntries.Exists(s => s.ipAddress == known.ipAddress));

            if (knownHosts.Count == 0) onCurrentServer = SwitcherServer.Bancho;
            else if (ourServer.Count > 0) onCurrentServer = SwitcherServer.Private;
            else onCurrentServer = SwitcherServer.Other;

            OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
            {
                eventType = SwitcherEvent.ServerSwitch,
                server = onCurrentServer,
#if ONLINE_SERVERS
                serverFailure = !serverSuccess
#else
#if FALLBACK
                serverFailure = true
#else
                serverFailure = false
#endif
#endif
            });
        }

        public async void Prepare()
        {
            OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
            {
                eventType = SwitcherEvent.PleaseWait
            });

            if (!Utils.IsAdministrator())
            {
                OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
                {
                    eventType = SwitcherEvent.NoAdminRights
                });
                return;
            }

            OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
            {
                eventType = SwitcherEvent.ServerConnecting
            });

#if ONLINE_SERVERS
            try
            {
                await PerformServerConnection();
            } catch(WebException e)
            {
                serverSuccess = false;
            }

            if(!serverSuccess)
            {
#if FALLBACK
                foreach(string host in targetFallover)
                {
                    serverEntries.Add(new HostEntry()
                    {
                        ipAddress = BuildInfo.StaticServerIP,
                        targetDomain = host
                    });
                }
#else
                OnSwitcherMessage?.Invoke(null, new SwitcherMessageEvent()
                {
                    eventType = SwitcherEvent.ServerError
                });
                return;
#endif
            }
#endif

            await HostsCheck();
        }
    }
}


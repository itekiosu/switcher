namespace ItekiSwitcher
{
    class BuildInfo
    {
        public static string ServerName          = "Iteki";
#if FALLBACK
        public static string StaticServerIP      = "3.133.82.130";
#endif
#if ONLINE_SERVERS
        public static string SwitcherServerList  = "http://switcher.iteki.pw/json";
#endif
    }
}

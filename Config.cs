using System;

namespace AnydeskLogger
{
    public static class Config
    {
        public static string Log => GenerateLogFileName();

        public static string Anydesk => "AnyDesk";

        private static string GenerateLogFileName()
        {
            return $"ip_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
        }
    }
}

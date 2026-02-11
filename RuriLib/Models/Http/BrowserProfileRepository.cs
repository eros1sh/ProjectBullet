using System;
using System.Collections.Generic;
using System.Net.Security;

namespace RuriLib.Models.Http
{
    public static class BrowserProfileRepository
    {
        // Chrome 120 TLS fingerprint cipher suites (JA3-matching order)
        private static readonly TlsCipherSuite[] ChromeCipherSuites = new[]
        {
            TlsCipherSuite.TLS_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
            TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
            TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
        };

        // Firefox 121 TLS fingerprint cipher suites
        private static readonly TlsCipherSuite[] FirefoxCipherSuites = new[]
        {
            TlsCipherSuite.TLS_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
            TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
            TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
            TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA,
        };

        // Safari 17 TLS fingerprint cipher suites
        private static readonly TlsCipherSuite[] SafariCipherSuites = new[]
        {
            TlsCipherSuite.TLS_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
            TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
            TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
            TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
            TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA,
        };

        public static BrowserProfile Chrome120 => new()
        {
            Name = "Chrome120",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            CipherSuites = ChromeCipherSuites,
            Headers = new Dictionary<string, string>
            {
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8" },
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Accept-Encoding", "gzip, deflate, br, zstd" },
                { "Sec-CH-UA", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"" },
                { "Sec-CH-UA-Mobile", "?0" },
                { "Sec-CH-UA-Platform", "\"Windows\"" },
                { "Sec-Fetch-Site", "none" },
                { "Sec-Fetch-Mode", "navigate" },
                { "Sec-Fetch-User", "?1" },
                { "Sec-Fetch-Dest", "document" },
                { "Upgrade-Insecure-Requests", "1" },
            }
        };

        public static BrowserProfile Firefox121 => new()
        {
            Name = "Firefox121",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
            CipherSuites = FirefoxCipherSuites,
            Headers = new Dictionary<string, string>
            {
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8" },
                { "Accept-Language", "en-US,en;q=0.5" },
                { "Accept-Encoding", "gzip, deflate, br" },
                { "Sec-Fetch-Dest", "document" },
                { "Sec-Fetch-Mode", "navigate" },
                { "Sec-Fetch-Site", "none" },
                { "Sec-Fetch-User", "?1" },
                { "Upgrade-Insecure-Requests", "1" },
                { "Connection", "keep-alive" },
            }
        };

        public static BrowserProfile Safari17 => new()
        {
            Name = "Safari17",
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_2) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.2 Safari/605.1.15",
            CipherSuites = SafariCipherSuites,
            Headers = new Dictionary<string, string>
            {
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" },
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Accept-Encoding", "gzip, deflate, br" },
                { "Sec-Fetch-Dest", "document" },
                { "Sec-Fetch-Mode", "navigate" },
                { "Sec-Fetch-Site", "none" },
                { "Connection", "keep-alive" },
            }
        };

        public static BrowserProfile Edge120 => new()
        {
            Name = "Edge120",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0",
            CipherSuites = ChromeCipherSuites, // Edge uses same Chromium cipher suites
            Headers = new Dictionary<string, string>
            {
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8" },
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Accept-Encoding", "gzip, deflate, br, zstd" },
                { "Sec-CH-UA", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Microsoft Edge\";v=\"120\"" },
                { "Sec-CH-UA-Mobile", "?0" },
                { "Sec-CH-UA-Platform", "\"Windows\"" },
                { "Sec-Fetch-Site", "none" },
                { "Sec-Fetch-Mode", "navigate" },
                { "Sec-Fetch-User", "?1" },
                { "Sec-Fetch-Dest", "document" },
                { "Upgrade-Insecure-Requests", "1" },
            }
        };

        private static readonly Random _random = new();

        public static BrowserProfile RandomProfile()
        {
            var profiles = new[] { Chrome120, Firefox121, Safari17, Edge120 };
            return profiles[_random.Next(profiles.Length)];
        }

        public static BrowserProfile GetByName(string name)
        {
            return name switch
            {
                nameof(Chrome120) => Chrome120,
                nameof(Firefox121) => Firefox121,
                nameof(Safari17) => Safari17,
                nameof(Edge120) => Edge120,
                "Random" => RandomProfile(),
                _ => null
            };
        }

        public static string[] GetAllNames() => new[]
        {
            "None", "Chrome120", "Firefox121", "Safari17", "Edge120", "Random"
        };
    }
}

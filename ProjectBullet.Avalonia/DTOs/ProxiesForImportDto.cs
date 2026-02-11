using RuriLib.Models.Proxies;

namespace ProjectBullet.Avalonia.DTOs
{
    public class ProxiesForImportDto
    {
        public string[] Lines { get; set; }
        public ProxyType DefaultType { get; set; }
        public string DefaultUsername { get; set; }
        public string DefaultPassword { get; set; }
    }
}

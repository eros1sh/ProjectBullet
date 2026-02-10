using System;

namespace ProjectBullet.Native.Updater.Models;

public record RemoteVersionInfo(Version Version, string DownloadUrl, double Size);

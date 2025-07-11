﻿// This file is subject to the terms and conditions defined
// in file 'LICENSE', which is part of this source code package.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DepotDownloader
{
	class DownloadConfig
	{
		public int CellID { get; set; }
		public bool DownloadAllPlatforms { get; set; }
		public bool DownloadAllArchs { get; set; }
		public bool DownloadAllLanguages { get; set; }
		public bool DownloadManifestOnly { get; set; }
		public string InstallDirectory { get; set; }

		public bool UsingFileList { get; set; }
		public HashSet<string> FilesToDownload { get; set; }
		public List<Regex> FilesToDownloadRegex { get; set; }

		public string BetaPassword { get; set; }

		public bool VerifyAll { get; set; }

		public int MaxDownloads { get; set; } = 8;

		public bool RememberPassword { get; set; }

		// A Steam LoginID to allow multiple concurrent connections
		public uint? LoginID { get; set; }

		public bool UseQrCode { get; set; }
		public bool SkipAppConfirmation { get; set; }
	}
}
// This file is subject to the terms and conditions defined
// in file 'LICENSE', which is part of this source code package.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.IsolatedStorage;
using ProtoBuf;

namespace DepotDownloader
{
	[ProtoContract]
	class AccountSettingsStore
	{
		// Member 1 was a Dictionary<string, byte[]> for SentryData.

		[ProtoMember(2, IsRequired = false)]
		public ConcurrentDictionary<string, int> ContentServerPenalty { get; private set; } = new();

		// Member 3 was a Dictionary<string, string> for LoginKeys.

		[ProtoMember(4, IsRequired = false)]
		public Dictionary<string, string> LoginTokens { get; private set; } = new();

		[ProtoMember(5, IsRequired = false)]
		public Dictionary<string, string> GuardData { get; private set; } = new();

		string FileName;

		AccountSettingsStore()
		{
			Instance = this;
			ContentServerPenalty = new ConcurrentDictionary<string, int>();
			LoginTokens = new(StringComparer.OrdinalIgnoreCase);
			GuardData = new(StringComparer.OrdinalIgnoreCase);
		}

		static bool Loaded
		{
			get { return Instance != null; }
		}

		public static AccountSettingsStore Instance { get; private set; } = new();
		static readonly IsolatedStorageFile IsolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly();

		public static void LoadFromFile(string filename)
		{
			if (Loaded)
				throw new Exception("Config already loaded");

			if (IsolatedStorage.FileExists(filename))
			{
				try
				{
					using var fs = IsolatedStorage.OpenFile(filename, FileMode.Open, FileAccess.Read);
					using var ds = new DeflateStream(fs, CompressionMode.Decompress);
					Instance = Serializer.Deserialize<AccountSettingsStore>(ds);
				}
				catch (IOException ex)
				{
					Console.WriteLine("Failed to load account settings: {0}", ex.Message);
					Instance = new AccountSettingsStore();
				}
			}
			else
			{
				Instance = new AccountSettingsStore();
			}

			Instance.FileName = filename;
		}
	}
}
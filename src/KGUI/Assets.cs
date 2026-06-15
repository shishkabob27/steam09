namespace KGUI
{
	public static class Assets
	{
		public static string GetAssetPath(string assetName)
		{
			string basePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/resources";
			if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
			return System.IO.Path.Combine(basePath, assetName);
		}
	}
}
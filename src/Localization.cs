using Newtonsoft.Json;

public static class Localization
{
	public static Dictionary<string, string> Strings = new();

	public static void Initialize(string language)
	{
		string path = $"resources/locales/{language}.json";
		if (File.Exists(path))
		{
			string json = File.ReadAllText(path);
			Dictionary<string, string> newStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new();
			Strings = newStrings;
		}
	}

	public static string GetString(string key)
	{
		if (Strings.ContainsKey(key))
		{
			return Strings[key];
		}
		return key;
	}
}
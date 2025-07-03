using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;

public static class Utils
{
	public static string AsTimeAgo(this DateTime dateTime)
	{
		TimeSpan timeSpan = DateTime.Now.Subtract(dateTime);

		if (timeSpan.TotalSeconds == 0) return "just now";
		if (timeSpan.TotalSeconds < 60) return $"{timeSpan.Seconds} second{(timeSpan.Seconds == 1 ? "" : "s")} ago";
		if (timeSpan.TotalMinutes < 60) return $"{timeSpan.Minutes} minute{(timeSpan.Minutes == 1 ? "" : "s")} ago";
		if (timeSpan.TotalHours < 24) return $"{timeSpan.Hours} hour{(timeSpan.Hours == 1 ? "" : "s")} ago";
		if (timeSpan.TotalDays < 30) return $"{timeSpan.Days} day{(timeSpan.Days == 1 ? "" : "s")} ago";
		return $"more than 1 year ago";
	}

	public static string SerializeAppInfoFileReadable(SteamApps.PICSProductInfoCallback.PICSProductInfo appInfo)
	{
		if (appInfo == null || appInfo.KeyValues == null)
		{
			return "{}";
		}

		try
		{
			JObject result = ConvertKeyValuesToJson(appInfo.KeyValues);
			return JsonConvert.SerializeObject(result, Formatting.Indented);
		}
		catch (Exception ex)
		{
			return JsonConvert.SerializeObject(new { Error = "Failed to parse app info", Exception = ex.Message }, Formatting.Indented);
		}
	}

	/// <summary>
	/// Recursively converts Steam KeyValues structure to proper JSON
	/// </summary>
	private static JObject ConvertKeyValuesToJson(SteamKit2.KeyValue keyValue)
	{
		JObject result = new JObject();

		if (keyValue == null) return result;

		foreach (var child in keyValue.Children)
		{
			string name = child.Name;
			if (string.IsNullOrEmpty(name)) continue;

			if (child.Children.Count > 0)
			{
				// Has children - check if it's an array-like structure
				if (IsNumericSequence(child.Children))
				{
					// Convert to array if children are numbered (0, 1, 2, etc.)
					result[name] = ConvertToArray(child.Children);
				}
				else
				{
					// Convert to object
					result[name] = ConvertKeyValuesToJson(child);
				}
			}
			else if (!string.IsNullOrEmpty(child.Value))
			{
				// Has a simple value
				result[name] = JToken.FromObject(ConvertValue(child.Value));
			}
			else
			{
				// Empty value
				result[name] = null;
			}
		}

		return result;
	}

	/// <summary>
	/// Checks if the children represent a numeric sequence (array-like)
	/// </summary>
	private static bool IsNumericSequence(List<SteamKit2.KeyValue> children)
	{
		if (children == null || children.Count == 0) return false;

		// Check if ALL children have numeric names
		foreach (var child in children)
		{
			if (!int.TryParse(child.Name, out _))
			{
				// If any child is not numeric, it's not an array
				return false;
			}
		}

		// If all children are numeric, treat as array regardless of gaps
		return true;
	}

	/// <summary>
	/// Converts numeric sequence children to a JSON array
	/// </summary>
	private static JArray ConvertToArray(List<SteamKit2.KeyValue> children)
	{
		JArray array = new JArray();

		if (children == null) return array;

		// Sort by numeric key to ensure proper order
		var sortedItems = children.OrderBy(child =>
		{
			int.TryParse(child.Name, out int index);
			return index;
		});

		foreach (var child in sortedItems)
		{
			if (child.Children.Count > 0)
			{
				// Nested object in array
				array.Add(ConvertKeyValuesToJson(child));
			}
			else if (!string.IsNullOrEmpty(child.Value))
			{
				// Simple value in array
				array.Add(JToken.FromObject(ConvertValue(child.Value)));
			}
			else
			{
				// Null value in array
				array.Add(null);
			}
		}

		return array;
	}

	/// <summary>
	/// Converts a string value to the appropriate JSON type
	/// </summary>
	private static object ConvertValue(string value)
	{
		if (string.IsNullOrEmpty(value)) return null;

		// Try to parse as number
		if (long.TryParse(value, out long longValue))
		{
			return longValue;
		}

		// Try to parse as boolean
		if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		// Return as string
		return value;
	}
}
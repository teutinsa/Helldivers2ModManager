using Helldivers2ModManager.Models;
using System.Text.Json;

namespace Tests;

[TestClass]
public class ManifestTests
{
	private static readonly JsonSerializerOptions s_options;
	private static readonly string[] s_deserialize_actual = ["Foo"];

	static ManifestTests()
	{
		s_options = new()
		{
			WriteIndented = true,
			AllowTrailingCommas = true
		};
		s_options.Converters.Add(new ModManifestJsonConverter());
	}

	[TestMethod]
	public void Deserialize_Valid()
	{
		string source = """
			{
				"Guid": "08de7daf-e968-4e5a-8fbb-15c9fb4767c1",
				"Name": "Valid HD2 Mod",
				"Description": "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est.",
				"IconPath": "icon.png",
				"Options": [
					"Foo"
				]
			}
			""";

		var result = JsonSerializer.Deserialize<ModManifest>(source, s_options);
		Assert.IsNotNull(result);
		Assert.AreEqual(result.Guid, Guid.Parse("08de7daf-e968-4e5a-8fbb-15c9fb4767c1"));
		Assert.AreEqual(result.Name, "Valid HD2 Mod");
		Assert.AreEqual(result.Description, "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est.");
		Assert.AreEqual(result.IconPath, "icon.png");
		CollectionAssert.AreEqual((string[]?)result.Options, s_deserialize_actual);
	}

	[TestMethod]
	public void Deserialize_Valid_Omitted()
	{
		string source = """
			{
				"Guid": "08de7daf-e968-4e5a-8fbb-15c9fb4767c1",
				"Name": "Valid HD2 Mod",
				"Description": "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est."
			}
			""";

		var result = JsonSerializer.Deserialize<ModManifest>(source, s_options);
		Assert.IsNotNull(result);
		Assert.AreEqual(result.Guid, Guid.Parse("08de7daf-e968-4e5a-8fbb-15c9fb4767c1"));
		Assert.AreEqual(result.Name, "Valid HD2 Mod");
		Assert.AreEqual(result.Description, "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est.");
		Assert.IsNull(result.IconPath);
		Assert.IsNull(result.Options);
	}

	[TestMethod]
	public void Deserialize_Valid_Description_Empty()
	{
		string source = """
			{
				"Guid": "08de7daf-e968-4e5a-8fbb-15c9fb4767c1",
				"Name": "HD2 Mod",
				"Description": ""
			}
			""";

		_ = JsonSerializer.Deserialize<ModManifest>(source, s_options);
	}

	[TestMethod]
	[ExpectedException(typeof(JsonException))]
	public void Deserialize_Invalid_Guid_Empty()
	{
		string source = """
			{
				"Guid": "",
				"Name": "HD2 Mod",
				"Description": "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est."
			}
			""";

		_ = JsonSerializer.Deserialize<ModManifest>(source, s_options);
	}

	[TestMethod]
	[ExpectedException(typeof(JsonException))]
	public void Deserialize_Invalid_Guid_Null()
	{
		string source = """
			{
				"Guid": null,
				"Name": "HD2 Mod",
				"Description": "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est."
			}
			""";

		_ = JsonSerializer.Deserialize<ModManifest>(source, s_options);
	}

	[TestMethod]
	[ExpectedException(typeof(JsonException))]
	public void Deserialize_Invalid_Name_Empty()
	{
		string source = """
			{
				"Guid": "08de7daf-e968-4e5a-8fbb-15c9fb4767c1",
				"Name": "",
				"Description": "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est."
			}
			""";

		_ = JsonSerializer.Deserialize<ModManifest>(source, s_options);
	}

	[TestMethod]
	[ExpectedException(typeof(JsonException))]
	public void Deserialize_Invalid_Name_Null()
	{
		string source = """
			{
				"Guid": "08de7daf-e968-4e5a-8fbb-15c9fb4767c1",
				"Name": null,
				"Description": "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est."
			}
			""";

		_ = JsonSerializer.Deserialize<ModManifest>(source, s_options);
	}

	[TestMethod]
	[ExpectedException(typeof(JsonException))]
	public void Deserialize_Invalid_Description_Null()
	{
		string source = """
			{
				"Guid": "08de7daf-e968-4e5a-8fbb-15c9fb4767c1",
				"Name": "HD2 Mod",
				"Description": null
			}
			""";

		_ = JsonSerializer.Deserialize<ModManifest>(source, s_options);
	}

	[TestMethod]
	[ExpectedException(typeof(JsonException))]
	public void Deserialize_Invalid_Options()
	{
		string source = """
			{
				"Guid": "08de7daf-e968-4e5a-8fbb-15c9fb4767c1",
				"Name": "HD2 Mod",
				"Description": null,
				"Options": [
					"Foo",
					1,
					false
				]
			}
			""";

		_ = JsonSerializer.Deserialize<ModManifest>(source, s_options);
	}

	[TestMethod]
	public void Serialize()
	{
		var manifest = new ModManifest
		{
			Guid = Guid.Parse("08de7daf-e968-4e5a-8fbb-15c9fb4767c1"),
			Name = "Valid HD2 Mod",
			Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est.",
			IconPath = "icon.png",
			Options = [ "Foo" ]
		};

		string actual = """
			{
			  "Guid": "08de7daf-e968-4e5a-8fbb-15c9fb4767c1",
			  "Name": "Valid HD2 Mod",
			  "Description": "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus est.",
			  "IconPath": "icon.png",
			  "Options": [
			    "Foo"
			  ]
			}
			""";

		var result = JsonSerializer.Serialize(manifest, s_options);
		Assert.AreEqual(actual, result);
	}
}
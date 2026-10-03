using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Import.Configuration;
using AssetRipper.Import.Logging;
using AssetRipper.SourceGenerated;
using AssetRipper.Assets.Collections;
using AssetRipper.Export.UnityProjects.Shaders;
using AssetRipper.SourceGenerated.Classes.ClassID_48;
using System.Text.Json;

namespace AssetRipper.Export.UnityProjects;

public sealed partial class ProjectExporter
{
	public event Action? EventExportPreparationStarted;
	public event Action? EventExportPreparationFinished;
	public event Action? EventExportStarted;
	public event Action<int, int>? EventExportProgressUpdated;
	public event Action? EventExportFinished;

	private readonly ObjectHandlerStack<IAssetExporter> assetExporterStack = new();

	/// <summary>Adds an exporter to the stack of exporters for this asset type.</summary>
	/// <typeparam name="T">The c sharp type of this asset type. Any inherited types also get this exporter.</typeparam>
	/// <param name="exporter">The new exporter. If it doesn't work, the next one in the stack is used.</param>
	/// <param name="allowInheritance">Should types that inherit from this type also use the exporter?</param>
	public void OverrideExporter<T>(IAssetExporter exporter, bool allowInheritance = true)
	{
		assetExporterStack.OverrideHandler(typeof(T), exporter, allowInheritance);
	}

	/// <summary>Adds an exporter to the stack of exporters for this asset type.</summary>
	/// <param name="type">The c sharp type of this asset type. Any inherited types also get this exporter.</param>
	/// <param name="exporter">The new exporter. If it doesn't work, the next one in the stack is used.</param>
	/// <param name="allowInheritance">Should types that inherit from this type also use the exporter?</param>
	public void OverrideExporter(Type type, IAssetExporter exporter, bool allowInheritance)
	{
		assetExporterStack.OverrideHandler(type, exporter, allowInheritance);
	}

	/// <summary>
	/// Use the <see cref="DummyExporter"/> for the specified class type.
	/// </summary>
	/// <typeparam name="T">The base type for assets of that <paramref name="classType"/>.</typeparam>
	/// <param name="isEmptyCollection">
	/// True: an exception will be thrown if the asset is referenced by another asset.<br/>
	/// False: any references to this asset will be replaced with a missing reference.
	/// </param>
	/// <param name="isMetaType"><see cref="AssetType.Meta"/> or <see cref="AssetType.Serialized"/>?</param>
	private void OverrideDummyExporter<T>(bool isEmptyCollection, bool isMetaType)
	{
		OverrideExporter<T>(DummyAssetExporter.Get(isEmptyCollection, isMetaType), true);
	}

	public AssetType ToExportType(Type type)
	{
		foreach (IAssetExporter exporter in assetExporterStack.GetHandlerStack(type))
		{
			if (exporter.ToUnknownExportType(type, out AssetType assetType))
			{
				return assetType;
			}
		}
		throw new NotSupportedException($"There is no exporter that know {nameof(AssetType)} for unknown asset '{type}'");
	}

	private IExportCollection CreateCollection(IUnityObjectBase asset)
	{
		foreach (IAssetExporter exporter in assetExporterStack.GetHandlerStack(asset.GetType()))
		{
			if (exporter.TryCreateCollection(asset, out IExportCollection? collection))
			{
				return collection;
			}
		}
		throw new Exception($"There is no exporter that can handle '{asset}'");
	}

	public void Export(GameBundle fileCollection, CoreConfiguration options, FileSystem fileSystem)
	{
		EventExportPreparationStarted?.Invoke();
		List<IExportCollection> collections = CreateCollections(fileCollection);
		EventExportPreparationFinished?.Invoke();

		EventExportStarted?.Invoke();
		ProjectAssetContainer container = new ProjectAssetContainer(this, options, fileCollection.FetchAssets(), collections);
		int exportableCount = collections.Count(c => c.Exportable);
		int currentExportable = 0;
		HashSet<IExportCollection> successful = new();

		for (int i = 0; i < collections.Count; i++)
		{
			IExportCollection collection = collections[i];
			container.CurrentCollection = collection;
			if (collection.Exportable)
			{
				currentExportable++;
				Logger.Info(LogCategory.ExportProgress, $"({currentExportable}/{exportableCount}) Exporting '{collection.Name}'");
				bool exportedSuccessfully = collection.Export(container, options.ProjectRootPath, fileSystem);
				if (exportedSuccessfully) successful.Add(collection);
				if (!exportedSuccessfully)
				{
					Logger.Warning(LogCategory.ExportProgress, $"Failed to export '{collection.Name}' ({collection.GetType().Name})");
				}
			}
			EventExportProgressUpdated?.Invoke(i, collections.Count);
		}
		// Record the actual pointers chosen by the exporter, including subassets and scenes.
		// Names and source path IDs alone cannot identify an exported Unity object.
		using (Stream stream = fileSystem.File.Create(fileSystem.Path.Join(options.ProjectRootPath, "recovery-object-map.json")))
		using (Utf8JsonWriter writer = new(stream))
		{
			writer.WriteStartObject(); writer.WriteNumber("schema_version", 1); writer.WriteStartArray("objects");
			foreach (IExportCollection collection in collections)
			{
				container.CurrentCollection = collection;
				foreach (IUnityObjectBase asset in collection.Assets)
				{
					if (asset.Collection is not SerializedAssetCollection) continue;
					writer.WriteStartObject();
					writer.WriteString("serialized_file", asset.Collection.Name);
					writer.WriteNumber("path_id", asset.PathID);
					writer.WriteString("type", asset.ClassName);
					writer.WriteBoolean("exported", successful.Contains(collection));
					try
					{
						MetaPtr pointer = collection.CreateExportPointer(container, asset, false);
						writer.WriteString("guid", pointer.GUID.ToString());
						writer.WriteNumber("file_id", pointer.FileID);
						writer.WriteNumber("asset_type", (int)pointer.AssetType);
					}
					catch (Exception exception) { writer.WriteString("error", exception.Message); }
					writer.WriteEndObject();
				}
			}
			writer.WriteEndArray(); writer.WriteEndObject();
		}
		ExportRecoveryCompiledShaders(container, collections, options.ProjectRootPath, fileSystem);
		EventExportFinished?.Invoke();
	}

	// The bundle's compiled shaders as YAML shader assets, beside the project rather than
	// under Assets/, so the normal export is unchanged and recovery decides what to install.
	// Compiled shader data only loads in the Unity version line that built it, so shaders
	// from another line are only recorded.
	private static void ExportRecoveryCompiledShaders(ProjectAssetContainer container, List<IExportCollection> collections, string projectRoot, FileSystem fileSystem)
	{
		string root = fileSystem.Path.Join(projectRoot, "RecoveryCompiledShaders");
		fileSystem.Directory.Create(root);
		YamlShaderExporter exporter = new();
		using Stream stream = fileSystem.File.Create(fileSystem.Path.Join(root, "index.json"));
		using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });
		writer.WriteStartObject();
		writer.WriteNumber("schema_version", 1);
		writer.WriteStartArray("shaders");
		foreach (IExportCollection collection in collections)
		{
			foreach (IUnityObjectBase asset in collection.Assets)
			{
				if (asset is not IShader shader || asset.Collection is not SerializedAssetCollection) continue;
				container.CurrentCollection = collection;
				var version = asset.Collection.Version;
				writer.WriteStartObject();
				writer.WriteString("file", asset.Collection.Name);
				writer.WriteNumber("path_id", asset.PathID);
				writer.WriteString("name", shader.Has_ParsedForm() ? shader.ParsedForm.Name.String : collection.Name);
				writer.WriteString("unity_version", version.ToString());
				try { writer.WriteString("stub_guid", collection.CreateExportPointer(container, asset, false).GUID.ToString()); }
				catch (Exception exception) { writer.WriteString("stub_error", exception.Message); }
				if (version.Major != 2022 || version.Minor != 3)
				{
					writer.WriteString("skipped", $"built with Unity {version}; compiled shader data loads only in the 2022.3 line");
				}
				else
				{
					string fileName = $"{FileSystem.FixInvalidPathCharacters(asset.Collection.Name)}_{asset.PathID}.asset";
					try
					{
						exporter.Export(container, asset, fileSystem.Path.Join(root, fileName), fileSystem);
						writer.WriteString("asset", fileName);
					}
					catch (Exception exception) { writer.WriteString("error", exception.Message); }
				}
				writer.WriteEndObject();
			}
		}
		writer.WriteEndArray(); writer.WriteEndObject();
	}

	private List<IExportCollection> CreateCollections(GameBundle fileCollection)
	{
		List<IExportCollection> collections = new();
		HashSet<IUnityObjectBase> queued = new();

		foreach (IUnityObjectBase asset in fileCollection.FetchAssets())
		{
			if (!queued.Contains(asset))
			{
				IExportCollection collection = CreateCollection(asset);
				foreach (IUnityObjectBase element in collection.Assets)
				{
					queued.Add(element);
				}
				collections.Add(collection);
			}
		}

		return collections;
	}
}

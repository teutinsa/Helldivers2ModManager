using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class ContentViewModel : ObservableObject
{
	public enum ContentKind
	{
		File,
		Option,
		Root
	}

	public int Count => _items.Count;
	public bool IsReadOnly => false;

	public ContentKind Kind { get; }

	public string Name { get; }

	public string? ToolTip { get; }

	public bool CanRemove
	{
		get
		{
			if (Kind == ContentKind.Root)
				return false;
			return Count <= 0;
		}
	}

	public bool CanAddOption => Kind != ContentKind.Option && !_items.Any(static c => c.Kind == ContentKind.File);
	
	public bool CanAddFiles => !_items.Any(static c => c.Kind == ContentKind.Option);

	private readonly List<ContentViewModel> _items;
	private readonly FileInfo? _file;
	private readonly string? _option;

	public event NotifyCollectionChangedEventHandler? CollectionChanged;

	public ContentViewModel()
	{
		_items = [];
		Kind = ContentKind.Root;
		Name = "Root";
	}

	public ContentViewModel(FileInfo file)
	{
		_items = [];
		_file = file;
		Kind = ContentKind.Option;
		Name = _file.Name;
		ToolTip = _file.FullName;
	}

	public ContentViewModel(string option)
	{
		_items = [];
		_option = option;
		Kind = ContentKind.Option;
		Name = _option;
	}

	public void Add(ContentViewModel item) => _items.Add(item);

	public void Clear() => _items.Clear();

	public bool Contains(ContentViewModel item) => _items.Contains(item);

	public void CopyTo(ContentViewModel[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

	public bool Remove(ContentViewModel item) => _items.Remove(item);

	public IEnumerator<ContentViewModel> GetEnumerator() => _items.GetEnumerator();

	[RelayCommand(CanExecute = nameof(CanAddFiles))]
	void AddFiles()
	{
		var vm = new ContentViewModel(new FileInfo("file"));
		Add(vm);
		CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, vm));
	}

	[RelayCommand(CanExecute = nameof(CanAddOption))]
	void AddOption()
	{
		var vm = new ContentViewModel("option");
		Add(vm);
		CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, vm));
	}

	[RelayCommand(CanExecute = nameof(CanRemove))]
	void Remove()
	{
	}
}
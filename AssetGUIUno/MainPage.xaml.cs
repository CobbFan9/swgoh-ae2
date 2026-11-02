using AssetGetterTools.models;
using AssetGUIUno.Interfaces;
using AssetGUIUno.Models;
using AssetGUIUno.Services;
using AssetStudio;
using FMOD;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;

namespace AssetGUIUno;

public sealed partial class MainPage : Page
{
    private string version = "3.0";
    private string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Assets_swgoh");
    private MainProgram mainProgram;
    private ObservableCollection<DiffType> _diffTypes = new(Enum.GetValues(typeof(DiffType)).Cast<DiffType>());
    public ObservableCollection<DiffType> DiffTypes => _diffTypes;
    public DiffType SelectedDiffType { get; set; }
    public ObservableCollection<AssetOS> AssetOSs { get; set; }
    public AssetOS SelectedAssetOS { get; set; }
    public ObservableCollection<string> DownloadableAssets { get; set; }
    public string SelectedDownloadableAsset { get; set; }
    public ObservableCollection<string> prefixes { get; set; }
    public string SelectedPrefix { get; set; }
    public string Title => "AssetGUIUno";

    private IWindowService _windowService;

    public MainPage(IWindowService windowService)
    {
        this.InitializeComponent();

        _windowService = windowService;

        this.mainProgram = new MainProgram();
        _windowService.SetSize(1000, 410);
        setVisibilityOfSecondaryRows(false);

        this.AssetOSs = new ObservableCollection<AssetOS>() { AssetOS.Windows, AssetOS.Android, AssetOS.iOS };
        this.SelectedAssetOS = AssetOS.Windows;
        this.DownloadableAssets = new ObservableCollection<string>();
        this.prefixes = new ObservableCollection<string>();

        this.SelectedDiffType = DiffType.New;
        this.tbExportPath.Text = this.path;
        Console.WriteLine("Assetgetter Started. Current Version: " + version);
    }

    private void setVisibilityOfSecondaryRows(bool visible)
    {
        GridLength gridLength = new GridLength(0);

        if (visible)
        {
            gridLength = new GridLength(35);
        }
        else
        {
            gridLength = new GridLength(0);
        }

        rowDownloadSingle.Height = gridLength;
        rowDownloadPrefix.Height = gridLength;
        rowDownloadAll.Height = gridLength;
        rowDownloadDiff.Height = gridLength;
        rowDownloadAudio.Height = gridLength;
    }


    #region UIHandlerFuncs
    private async void downloadAudio()
    {
        await mainProgram.downloadAllAudioFiles();
    }

    private async void downloadSingle()
    {
        await mainProgram.exportSingleFile(SelectedDownloadableAsset);
    }

    private async void downloadPrefixes()
    {
        await mainProgram.exportAllWithPrefixFile(SelectedPrefix);
    }

    private async void downloadAll()
    {
        await mainProgram.exportAllFiles();
    }

    private async Task<bool> downloadDiff(string oldVersion)
    {
        var diffs = mainProgram.diffAssetVersions(oldVersion, SelectedDiffType);
        Console.WriteLine($"Number of differences: {diffs.Count}");
        foreach (var diff in diffs)
        {
            Console.WriteLine(diff);
        }
        Console.WriteLine($"Diff end...");
        Console.WriteLine($"Start downloading");
        mainProgram.exportMultipleAssets(diffs);
        Console.WriteLine($"Done downloading");
        return true;
    }

    private async void getListOfAssets()
    {
        mainProgram.AssetVersion = tbVersion.Text;
        mainProgram.workingFolder = tbExportPath.Text;
        mainProgram.targetFolder = tbExportPath.Text + "/OutPut";
        mainProgram.audioTargetFolder = this.tbExportPath.Text + "/Audio_OutPut";

        this.mainProgram.SetAssetOSPath(this.SelectedAssetOS);

        Directory.CreateDirectory(mainProgram.workingFolder);
        Directory.CreateDirectory(mainProgram.targetFolder);

        mainProgram.DownloadManifest();

        DownloadableAssets.AddRange(mainProgram.GetAssetsFromManifest());
        SelectedDownloadableAsset = DownloadableAssets.FirstOrDefault();
        prefixes.AddRange(mainProgram.GetPrefixesFromManifest());
        SelectedPrefix = prefixes.FirstOrDefault();

        _windowService.SetSize(1000, 650);
        setVisibilityOfSecondaryRows(true);
    }

    private async void refreshVersion()
    {
        Console.WriteLine("Trying to get newest active Assetversion");
        var versionGetterUrl = "https://raw.githubusercontent.com/swgoh-utils/gamedata/refs/heads/main/meta.json";
        var versionResponse = await new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Get, versionGetterUrl));
        if (versionResponse.IsSuccessStatusCode)
        {
            var metaDataShortPlain = await versionResponse.Content.ReadAsStringAsync();
            var metaDataShort = JsonConvert.DeserializeObject<MetadataShortModel>(metaDataShortPlain);

            var versionResult = metaDataShort.data.assetVersion.ToString();
            Console.WriteLine("AssetVersion: " + versionResult);
            this.tbVersion.Text = versionResult;
        }
        else
            Console.WriteLine(string.Format("Error getting newest Assetversion. Requeststatus: {0}", (object)versionResponse.StatusCode));
    }

    private async Task setPathViaPicker()
    {
        var folderPicker = new FolderPicker();

        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(((App)Application.Current).CurrentWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();

        if (folder is null)
        {
            return;
        }

        tbExportPath.Text = folder.Path;

        mainProgram.workingFolder = folder.Path;
        mainProgram.targetFolder = Path.Combine(folder.Path, "OutPut");
        mainProgram.audioTargetFolder = Path.Combine(folder.Path, "Audio_OutPut");
    }
    #endregion

    #region UIHandlers
    private void btSetExportPath_Click(object sender, RoutedEventArgs e)
    {
        this.setPathViaPicker();
    }

    private void btRefreshVersion_Click(object sender, RoutedEventArgs e)
    {
        this.refreshVersion();
    }

    private void btGetManifest_Click(object sender, RoutedEventArgs e)
    {
        getListOfAssets();
    }

    private void btDownloadAll_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(() => downloadAll());
    }

    private void btDownloadSingle_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(() => downloadSingle());
    }

    private void btDownloadPrefix_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(() => downloadPrefixes());
    }

    private void btDownloadDiff_Click(object sender, RoutedEventArgs e)
    {
        var oldVersion = tbDownloadDiff.Text;
        Task.Run(() => downloadDiff(oldVersion));
    }

    private void btDownloadAudio_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(() => downloadAudio());
    }

    private void cbShader_Checked(object sender, RoutedEventArgs e)
    {
        this.mainProgram.exportShader = this.cbShader.IsChecked.Value;
    }

    private void cbMeshes_Checked(object sender, RoutedEventArgs e)
    {
        this.mainProgram.exportMeshes = this.cbMeshes.IsChecked.Value;
    }

    private void cbAnimator_Checked(object sender, RoutedEventArgs e)
    {
        this.mainProgram.exportAnimator = this.cbAnimator.IsChecked.Value;
    }

    private void cbSpriteAtlases_Checked(object sender, RoutedEventArgs e)
    {
        this.mainProgram.exportSpriteAtlases = this.cbSpriteAtlases.IsChecked.Value;
    }
    #endregion

}
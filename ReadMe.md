<h1 align="center">VideoCatalog</h1>

Catalog of locally stored video files with quick preview, sorting, filtering functions.
<p align="center"><img src="./ReadMe-assets/main.gif" width="95%"></p>
<h3 align="center"><a href="https://github.com/MonsterS51/VideoCatalog/releases">Download</a></h2>

# Features:
* Implies working with a single folder containing many video files and low nesting subfolders.
  * Root folder - is Root album.
  * Each subfolder - is an album with all contained video files grouped by path.
* Doesn't change folder structure or file content (__Torrent friendly__).
* Catalog data is stored in a __separate file__ of the same name in the root folder.
* Tiled / List view of catalog items
  * Customizable grid size and step.
  * Display of resolution icons, number of elements, duration.
  * Customizable display of custom attributes for list mode.
* __Video preview on hover__
  * Customizable step, duration, delay.
  * Using built-in / installed codecs of Windows (recommended [K-Lite Codec Pack](https://codecguide.com/download_kl.htm) for correct MP4/MKV playback) or library [FFMPEG](https://www.gyan.dev/ffmpeg/builds/).
* __Cover display__
  * Loading image with the same name and in the same folder as the video.
  * If not found - it takes a frame from the middle of the video.
  * Ability to enable loading of covers displayed in Explorer (recommended [Icaros](https://shark007.net/forum/Forum-Icaros-Development)).
  * Ability to limit resolution to reduce memory consumption.
* __Data filling__ for catalog items (hideable sidebar on the right)
  * Assignment of readable names.
  * Assignment of description.
  * Assignment of tags.
  * Assignment of custom attributes.
  * Button for quick transition to search engines (you can add your own patterns of search engines and sites).
  * Hiding catalog items.
  * Batch processing of element names through replacement by Regex expression, trimming spaces, removing file extension.
* __Search__ by name / tags / attributes.
* __Sorting__ (alphanumeric) by name / dates / resolution / attributes with the corresponding grouping (able to be disabled).
* Filtration of not relevant / hidden catalog items.
* Opening items in tabs.
* The color scheme of the application is determined by the Windows theme.
* Quick open a folder through the explorer context menu (optional).

### Configuring K-Lite Codecs
```
Codec Tweak Tool -> Media Foundation -> Disable for these formats -> .mkv
```
### Bugs
* FFME preview incorrectly aligns video to the upper left corner.
* FFME preview crashes the application on memory access unpredictably.
* Preview using standard WPF element may not work correctly with the ForceGC option.

<p align="center"><img src="./ReadMe-assets/done.jpg" width="30%"></p>
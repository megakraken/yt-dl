# yt-dl
Just a simple front-end for youtube-dl (or rather it's current flavour [yt-dlp](https://github.com/yt-dlp/yt-dlp/)) that takes care of updating youtube-dl, downloading FFmpeg etc.

I have found youtube-dl's update option to be unreliable in the past, so this just checks for the [latest version on GitHub](https://github.com/yt-dlp/yt-dlp/releases) and downloads that if it's newer than the local version. It will also ensure *FFmpeg* exists and if not, automatically download the [latest build from GitHub](https://github.com/BtbN/FFmpeg-Builds/releases).

The first time you run `yt-dl` it will grab the latest version of *yt-dlp* and place it in the same directory as `yt-dl`. It will also add the contatining directory to the PATH environment variable, so that `yt-dl` can be called from any directory. On subsequent runs `yt-dl` will check for newer versions of *yt-dlp* and if available, automatically download them.

If `yt-dl` is invoked without specifying a URL, it will look for a URL in the clipboard. So you can just right-click copy a URL from your browser's address-bar and run yt-dl to have it downloaded.

`yt-dl` accepts a couple of extra command line options for my personal convenience. All other command line options are passed on to youtube-dl verbatim.

| Command line | Description |
| -----------  | ----------- |
| `yt-dl` | If clipboard contains a URL, pass it to youtube-dl.       |
| `yt-dl --mp3` | Same as above, but only downloads audio as an .mp3 file. |
| `yt-dl --mp4` | Same as above, but converts downloaded video to .mp4 if it isn't already. |
| `yt-dl --open` | Open explorer window of containing directory when download completed. |
| `yt-dl --no-ffmpeg` | Don't check for or download *FFmpeg.exe*. |
| `yt-dl --no-add-path` | Don't add containing directory to the *PATH* environment variable. |

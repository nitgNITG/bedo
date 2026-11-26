#if UNITY_STANDALONE_WIN && UNITY_EDITOR

using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Ookii.Dialogs;

namespace SFB {
    // For fullscreen support
    // - WindowWrapper class and GetActiveWindow() are required for modal file dialog.
    // - "PlayerSettings/Visible In Background" should be enabled, otherwise when file dialog opened app window minimizes automatically.

    public class WindowWrapper : IWin32Window {
        private IntPtr _hwnd;
        public WindowWrapper(IntPtr handle) { _hwnd = handle; }
        public IntPtr Handle { get { return _hwnd; } }
    }

    public class StandaloneFileBrowserWindows : IStandaloneFileBrowser {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect) {
            var fd = new VistaOpenFileDialog();
            fd.Title = title;
            if (extensions != null) {
                fd.Filter = GetFilterFromFileExtensionList(extensions);
                fd.FilterIndex = 1;
            }
            else {
                fd.Filter = string.Empty;
            }
            fd.Multiselect = multiselect;
            if (!string.IsNullOrEmpty(directory)) {
                fd.FileName = GetDirectoryPath(directory);
            }
            var res = fd.ShowDialog(new WindowWrapper(GetActiveWindow()));
            var filenames = res == DialogResult.OK ? fd.FileNames : new string[0];
            fd.Dispose();
            return filenames;
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb) {
            cb.Invoke(OpenFilePanel(title, directory, extensions, multiselect));
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect) {
            var fd = new VistaFolderBrowserDialog();
            fd.Description = title;
            if (!string.IsNullOrEmpty(directory)) {
                fd.SelectedPath = GetDirectoryPath(directory);
            }
            var res = fd.ShowDialog(new WindowWrapper(GetActiveWindow()));
            var filenames = res == DialogResult.OK ? new []{ fd.SelectedPath } : new string[0];
            fd.Dispose();
            return filenames;
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb) {
            cb.Invoke(OpenFolderPanel(title, directory, multiselect));
        }

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions) {
            var fd = new VistaSaveFileDialog();
            fd.Title = title;

            var finalFilename = "";

            if (!string.IsNullOrEmpty(directory)) {
                finalFilename = GetDirectoryPath(directory);
            }

            if (!string.IsNullOrEmpty(defaultName)) {
                finalFilename += defaultName;
            }

            fd.FileName = finalFilename;
            if (extensions != null) {
                fd.Filter = GetFilterFromFileExtensionList(extensions);
                fd.FilterIndex = 1;
                fd.DefaultExt = extensions[0].Extensions[0];
                fd.AddExtension = true;
            }
            else {
                fd.DefaultExt = string.Empty;
                fd.Filter = string.Empty;
                fd.AddExtension = false;
            }
            var res = fd.ShowDialog(new WindowWrapper(GetActiveWindow()));
            var filename = res == DialogResult.OK ? fd.FileName : "";
            fd.Dispose();
            return filename;
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb) {
            cb.Invoke(SaveFilePanel(title, directory, defaultName, extensions));
        }

        // .NET Framework FileDialog Filter format
        // https://msdn.microsoft.com/en-us/library/microsoft.win32.filedialog.filter
        private static string GetFilterFromFileExtensionList(ExtensionFilter[] extensions) {
            var filterString = "";
            foreach (var filter in extensions) {
                filterString += filter.Name + "(";

                foreach (var ext in filter.Extensions) {
                    filterString += "*." + ext + ",";
                }

                filterString = filterString.Remove(filterString.Length - 1);
                filterString += ") |";

                foreach (var ext in filter.Extensions) {
                    filterString += "*." + ext + "; ";
                }

                filterString += "|";
            }
            filterString = filterString.Remove(filterString.Length - 1);
            return filterString;
        }

        private static string GetDirectoryPath(string directory) {
            var directoryPath = Path.GetFullPath(directory);
            if (!directoryPath.EndsWith("\\")) {
                directoryPath += "\\";
            }
            if (Path.GetPathRoot(directoryPath) == directoryPath) {
                return directory;
            }
            return Path.GetDirectoryName(directoryPath) + Path.DirectorySeparatorChar;
        }
    }
}

#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SFB
{
    public class StandaloneFileBrowserWindows : IStandaloneFileBrowser
    {
        private const int FileBufferSize = 32768;
        private const uint CommonFlags = 0x00080000 | 0x00000800 | 0x00000008; // Explorer, existing path, preserve working directory.

        [StructLayout(LayoutKind.Sequential)]
        private struct OpenFileName
        {
            public int Size;
            public IntPtr Owner, Instance, Filter, CustomFilter;
            public int MaxCustomFilter, FilterIndex;
            public IntPtr File;
            public int MaxFile;
            public IntPtr FileTitle;
            public int MaxFileTitle;
            public IntPtr InitialDirectory, Title;
            public uint Flags;
            public ushort FileOffset, FileExtension;
            public IntPtr DefaultExtension, CustomData, Hook, Template, Reserved;
            public int ReservedValue, ExtendedFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BrowseInfo
        {
            public IntPtr Owner, Root, DisplayName, Title;
            public uint Flags;
            public IntPtr Callback, Data;
            public int Image;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int BrowseCallback(IntPtr window, uint message, IntPtr parameter, IntPtr data);
        private static readonly BrowseCallback OnBrowse = BrowseInitialized;

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr SendMessageW(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("comdlg32.dll", ExactSpelling = true)]
        private static extern int GetOpenFileNameW(ref OpenFileName dialog);
        [DllImport("comdlg32.dll", ExactSpelling = true)]
        private static extern int GetSaveFileNameW(ref OpenFileName dialog);
        [DllImport("comdlg32.dll")]
        private static extern uint CommDlgExtendedError();
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern IntPtr SHBrowseForFolderW(ref BrowseInfo dialog);
        [DllImport("shell32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SHGetPathFromIDListW(IntPtr item, StringBuilder path);
        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr reserved, uint apartment);
        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            return ShowFileDialog(title, directory, null, extensions, false, multiselect);
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb)
        {
            cb(OpenFilePanel(title, directory, extensions, multiselect));
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect)
        {
            int apartment = CoInitializeEx(IntPtr.Zero, 2); // STA
            var dialog = new BrowseInfo
            {
                Owner = GetActiveWindow(),
                Flags = apartment >= 0 ? 0x0041u : 0x0001u, // File system folders, modern UI when STA.
            };
            IntPtr item = IntPtr.Zero;
            try
            {
                dialog.Title = Marshal.StringToHGlobalUni(title ?? string.Empty);
                dialog.DisplayName = Marshal.AllocHGlobal(260 * sizeof(char));
                if (!string.IsNullOrEmpty(directory))
                {
                    dialog.Data = Marshal.StringToHGlobalUni(Path.GetFullPath(directory));
                    dialog.Callback = Marshal.GetFunctionPointerForDelegate(OnBrowse);
                }
                item = SHBrowseForFolderW(ref dialog);
                var path = new StringBuilder(260);
                if (item != IntPtr.Zero && SHGetPathFromIDListW(item, path) != 0)
                    return new[] { path.ToString() };

                return Array.Empty<string>();
            }
            finally
            {
                Marshal.FreeCoTaskMem(item);
                Marshal.FreeHGlobal(dialog.Data);
                Marshal.FreeHGlobal(dialog.DisplayName);
                Marshal.FreeHGlobal(dialog.Title);
                if (apartment >= 0)
                    CoUninitialize();
            }
        }

        [AOT.MonoPInvokeCallback(typeof(BrowseCallback))]
        private static int BrowseInitialized(IntPtr window, uint message, IntPtr parameter, IntPtr data)
        {
            if (message == 1 && data != IntPtr.Zero)
                SendMessageW(window, 0x467, new IntPtr(1), data); // BFFM_SETSELECTIONW

            return 0;
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb)
        {
            cb(OpenFolderPanel(title, directory, multiselect));
        }

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions)
        {
            var paths = ShowFileDialog(title, directory, defaultName, extensions, true, false);
            return paths.Length == 0 ? string.Empty : paths[0];
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
        {
            cb(SaveFilePanel(title, directory, defaultName, extensions));
        }

        private static string[] ShowFileDialog(string title, string directory, string defaultName, ExtensionFilter[] extensions, bool save, bool multiselect)
        {
            var dialog = new OpenFileName
            {
                Size = Marshal.SizeOf<OpenFileName>(),
                Owner = GetActiveWindow(),
                MaxFile = FileBufferSize,
                FilterIndex = 1,
                Flags = CommonFlags | (save ? 0x00000002u : 0x00001000u), // Overwrite prompt or existing file.
            };
            if (multiselect)
                dialog.Flags |= 0x00000200;

            try
            {
                dialog.File = Marshal.AllocHGlobal(FileBufferSize * sizeof(char));
                var buffer = new char[FileBufferSize];
                if (!string.IsNullOrEmpty(defaultName))
                    defaultName.CopyTo(0, buffer, 0, Math.Min(defaultName.Length, buffer.Length - 1));

                Marshal.Copy(buffer, 0, dialog.File, buffer.Length);
                dialog.Title = Marshal.StringToHGlobalUni(title ?? string.Empty);
                if (!string.IsNullOrEmpty(directory))
                    dialog.InitialDirectory = Marshal.StringToHGlobalUni(Path.GetFullPath(directory));

                string filter = BuildFilter(extensions, out string defaultExtension);
                dialog.Filter = Marshal.StringToHGlobalUni(filter);
                if (save && defaultExtension != null)
                    dialog.DefaultExtension = Marshal.StringToHGlobalUni(defaultExtension);

                int result = save ? GetSaveFileNameW(ref dialog) : GetOpenFileNameW(ref dialog);
                if (result == 0)
                {
                    uint error = CommDlgExtendedError();
                    if (error != 0)
                        UnityEngine.Debug.LogWarning($"Windows file dialog failed (0x{error:X}).");

                    return Array.Empty<string>();
                }

                string first = Marshal.PtrToStringUni(dialog.File);
                int offset = first.Length + 1;
                if (!multiselect || dialog.FileOffset < offset || offset >= buffer.Length)
                    return new[] { first };

                var paths = new List<string>();
                while (offset < buffer.Length)
                {
                    string name = Marshal.PtrToStringUni(IntPtr.Add(dialog.File, offset * sizeof(char)));
                    if (string.IsNullOrEmpty(name))
                        break;

                    paths.Add(Path.Combine(first, name));
                    offset += name.Length + 1;
                }

                return paths.Count == 0 ? new[] { first } : paths.ToArray();
            }
            finally
            {
                Marshal.FreeHGlobal(dialog.DefaultExtension);
                Marshal.FreeHGlobal(dialog.Filter);
                Marshal.FreeHGlobal(dialog.InitialDirectory);
                Marshal.FreeHGlobal(dialog.Title);
                Marshal.FreeHGlobal(dialog.File);
            }
        }

        private static string BuildFilter(ExtensionFilter[] extensions, out string defaultExtension)
        {
            defaultExtension = null;
            var filters = new StringBuilder();
            foreach (var filter in extensions ?? Array.Empty<ExtensionFilter>())
            {
                var patterns = new List<string>();
                foreach (string value in filter.Extensions ?? Array.Empty<string>())
                {
                    string extension = value?.Trim().TrimStart('.');
                    if (string.IsNullOrEmpty(extension))
                        continue;

                    if (extension != "*" && defaultExtension == null && filters.Length == 0 && patterns.Count == 0)
                        defaultExtension = extension;

                    patterns.Add("*." + extension);
                }

                if (patterns.Count == 0)
                    continue;

                string pattern = string.Join(";", patterns);
                filters.Append(string.IsNullOrEmpty(filter.Name) ? pattern : filter.Name)
                    .Append('\0').Append(pattern).Append('\0');
            }

            return filters.Length == 0 ? "All files\0*.*\0" : filters.ToString();
        }
    }
}
#endif


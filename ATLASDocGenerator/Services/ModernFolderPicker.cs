using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ATLASDocGenerator.Services
{
    /// <summary>
    /// Ouvre le sélecteur de dossiers moderne de l'Explorateur Windows.
    /// Il remplace FolderBrowserDialog et son ancienne vue arborescente.
    /// </summary>
    internal static class ModernFolderPicker
    {
        private const uint FosPickFolders = 0x00000020;
        private const uint FosForceFileSystem = 0x00000040;
        private const uint FosPathMustExist = 0x00000800;
        private const uint FosDontAddToRecent = 0x02000000;
        private const uint SigDnFileSystemPath = 0x80058000;
        private const int ErrorCancelled = unchecked((int)0x800704C7);

        internal static bool TrySelectFolder(
            IWin32Window owner,
            string title,
            string initialDirectory,
            out string selectedPath)
        {
            selectedPath = string.Empty;

            try
            {
                return TrySelectWithWindowsExplorer(
                    owner,
                    title,
                    initialDirectory,
                    out selectedPath);
            }
            catch (COMException)
            {
                return TrySelectWithModernFileDialog(
                    owner,
                    title,
                    initialDirectory,
                    out selectedPath);
            }
            catch (PlatformNotSupportedException)
            {
                return TrySelectWithModernFileDialog(
                    owner,
                    title,
                    initialDirectory,
                    out selectedPath);
            }
        }

        private static bool TrySelectWithWindowsExplorer(
            IWin32Window owner,
            string title,
            string initialDirectory,
            out string selectedPath)
        {
            selectedPath = string.Empty;
            IFileDialog dialog = (IFileDialog)new FileOpenDialogComObject();
            IShellItem initialFolder = null;
            IShellItem result = null;

            try
            {
                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(
                    options
                    | FosPickFolders
                    | FosForceFileSystem
                    | FosPathMustExist
                    | FosDontAddToRecent);

                if (!string.IsNullOrWhiteSpace(title))
                    dialog.SetTitle(title);

                if (!string.IsNullOrWhiteSpace(initialDirectory)
                    && Directory.Exists(initialDirectory))
                {
                    Guid shellItemId = typeof(IShellItem).GUID;
                    int createResult = SHCreateItemFromParsingName(
                        Path.GetFullPath(initialDirectory),
                        IntPtr.Zero,
                        ref shellItemId,
                        out initialFolder);
                    if (createResult == 0 && initialFolder != null)
                        dialog.SetFolder(initialFolder);
                }

                int showResult = dialog.Show(owner == null ? IntPtr.Zero : owner.Handle);
                if (showResult == ErrorCancelled)
                    return false;
                Marshal.ThrowExceptionForHR(showResult);

                dialog.GetResult(out result);
                IntPtr pathPointer;
                result.GetDisplayName(SigDnFileSystemPath, out pathPointer);
                try
                {
                    selectedPath = Marshal.PtrToStringUni(pathPointer) ?? string.Empty;
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pathPointer);
                }

                return Directory.Exists(selectedPath);
            }
            finally
            {
                if (result != null)
                    Marshal.ReleaseComObject(result);
                if (initialFolder != null)
                    Marshal.ReleaseComObject(initialFolder);
                Marshal.ReleaseComObject(dialog);
            }
        }

        private static bool TrySelectWithModernFileDialog(
            IWin32Window owner,
            string title,
            string initialDirectory,
            out string selectedPath)
        {
            selectedPath = string.Empty;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = title;
                dialog.AutoUpgradeEnabled = true;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.ValidateNames = false;
                dialog.DereferenceLinks = true;
                dialog.RestoreDirectory = true;
                dialog.FileName = "Sélectionner ce dossier";
                if (!string.IsNullOrWhiteSpace(initialDirectory)
                    && Directory.Exists(initialDirectory))
                {
                    dialog.InitialDirectory = initialDirectory;
                }

                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return false;

                string candidate = dialog.FileName;
                selectedPath = Directory.Exists(candidate)
                    ? candidate
                    : Path.GetDirectoryName(candidate);
                return !string.IsNullOrWhiteSpace(selectedPath)
                    && Directory.Exists(selectedPath);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            IntPtr bindContext,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        [ClassInterface(ClassInterfaceType.None)]
        private class FileOpenDialogComObject
        {
        }

        [ComImport]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);
            void SetFileTypes(uint count, IntPtr filters);
            void SetFileTypeIndex(uint index);
            void GetFileTypeIndex(out uint index);
            void Advise(IntPtr events, out uint cookie);
            void Unadvise(uint cookie);
            void SetOptions(uint options);
            void GetOptions(out uint options);
            void SetDefaultFolder(IShellItem shellItem);
            void SetFolder(IShellItem shellItem);
            void GetFolder(out IShellItem shellItem);
            void GetCurrentSelection(out IShellItem shellItem);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
            void GetResult(out IShellItem shellItem);
            void AddPlace(IShellItem shellItem, uint alignment);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
            void Close(int errorCode);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr filter);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr bindContext, ref Guid handlerId, ref Guid riid, out IntPtr interfacePointer);
            void GetParent(out IShellItem parent);
            void GetDisplayName(uint displayName, out IntPtr name);
            void GetAttributes(uint mask, out uint attributes);
            void Compare(IShellItem shellItem, uint hint, out int order);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AutoHDR
{
    public class HdrController
    {
        private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
        private const int ERROR_SUCCESS = 0;

        // Stare API (Windows 10 / Windows 11 < 24H2)
        private const int DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;
        private const int DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10;

        // Nowe API (Windows 11 24H2+, build 26100+)
        private const int DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2 = 15;
        private const int DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE = 16;

        private static readonly bool s_useNewApi = UseNewHdrApi();

        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements,
            [In, Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint numModeInfoArrayElements,
            [In, Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_DEVICE_INFO_HEADER requestPacket);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_DEVICE_INFO_HEADER setPacket);

        private enum DISPLAYCONFIG_ADVANCED_COLOR_MODE : uint
        {
            SDR = 0,
            WCG = 1,
            HDR = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public uint scanLineOrdering;
            public int targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 80)]
        private struct DISPLAYCONFIG_MODE_INFO
        {
            [FieldOffset(0)] public uint infoType;
            [FieldOffset(4)] public uint id;
            [FieldOffset(8)] public LUID adapterId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public int type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint colorEncoding;
            public uint bitsPerColorChannel;
            public uint value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint enableAdvancedColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
            public uint colorEncoding;
            public uint bitsPerColorChannel;
            public uint activeColorMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_SET_HDR_STATE
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
        }

        private class MonitorState
        {
            public LUID AdapterId;
            public uint Id;
            public bool WasEnabled;
        }

        private struct AdvancedColorResult
        {
            public bool Supported;
            public bool Enabled;
        }

        private List<MonitorState> _savedStates;
        private bool _gameMode;

        public bool GameModeActive => _gameMode;

        public bool IsHDREnabled()
        {
            var paths = QueryActivePaths();
            if (paths == null) return false;

            foreach (var path in paths)
            {
                var info = GetAdvancedColorInfo(path.targetInfo.adapterId, path.targetInfo.id);
                if (info.Enabled)
                    return true;
            }

            return false;
        }

        public bool IsHDRSupported()
        {
            var paths = QueryActivePaths();
            if (paths == null) return false;

            foreach (var path in paths)
            {
                var info = GetAdvancedColorInfo(path.targetInfo.adapterId, path.targetInfo.id);
                if (info.Supported)
                    return true;
            }

            return false;
        }

        public int BeginGameMode()
        {
            if (_gameMode) return 0;

            var paths = QueryActivePaths();
            if (paths == null) return 0;

            _savedStates = new List<MonitorState>();

            foreach (var path in paths)
            {
                var info = GetAdvancedColorInfo(path.targetInfo.adapterId, path.targetInfo.id);
                if (!info.Supported) continue;

                _savedStates.Add(new MonitorState
                {
                    AdapterId = path.targetInfo.adapterId,
                    Id = path.targetInfo.id,
                    WasEnabled = info.Enabled
                });

                if (!info.Enabled)
                    SetAdvancedColor(path.targetInfo.adapterId, path.targetInfo.id, true);
            }

            _gameMode = _savedStates.Count > 0;
            return _savedStates.Count;
        }

        public int EndGameMode()
        {
            if (!_gameMode || _savedStates == null) return 0;

            int restored = 0;
            foreach (var state in _savedStates)
            {
                var info = GetAdvancedColorInfo(state.AdapterId, state.Id);
                if (info.Enabled != state.WasEnabled)
                {
                    SetAdvancedColor(state.AdapterId, state.Id, state.WasEnabled);
                    restored++;
                }
            }

            _gameMode = false;
            _savedStates = null;
            return restored;
        }

        private DISPLAYCONFIG_PATH_INFO[] QueryActivePaths()
        {
            int err = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint pathCount, out uint modeCount);
            if (err != ERROR_SUCCESS)
                throw new Win32Exception(err);

            if (pathCount == 0) return null;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

            uint pc = pathCount;
            uint mc = modeCount;

            err = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pc, paths, ref mc, modes, IntPtr.Zero);
            if (err != ERROR_SUCCESS)
                throw new Win32Exception(err);

            Array.Resize(ref paths, (int)pc);
            return paths;
        }

        private static bool UseNewHdrApi()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    string build = key?.GetValue("CurrentBuildNumber") as string;
                    if (!string.IsNullOrEmpty(build) && int.TryParse(build, out int buildNumber))
                        return buildNumber >= 26100;
                }
            }
            catch { }
            return false;
        }

        private AdvancedColorResult GetAdvancedColorInfo(LUID adapterId, uint id)
        {
            if (s_useNewApi)
            {
                var info = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2();
                info.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2;
                info.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2));
                info.header.adapterId = adapterId;
                info.header.id = id;

                int err = DisplayConfigGetDeviceInfo(ref info.header);
                if (err != ERROR_SUCCESS)
                    throw new Win32Exception(err);

                bool supported = (info.value & 0x10) != 0; // highDynamicRangeSupported
                bool enabled = info.activeColorMode == (uint)DISPLAYCONFIG_ADVANCED_COLOR_MODE.HDR;
                return new AdvancedColorResult { Supported = supported, Enabled = enabled };
            }
            else
            {
                var info = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
                info.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
                info.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO));
                info.header.adapterId = adapterId;
                info.header.id = id;

                int err = DisplayConfigGetDeviceInfo(ref info.header);
                if (err != ERROR_SUCCESS)
                    throw new Win32Exception(err);

                bool supported = (info.value & 0x1) != 0;
                bool enabled = (info.value & 0x2) != 0;
                return new AdvancedColorResult { Supported = supported, Enabled = enabled };
            }
        }

        private void SetAdvancedColor(LUID adapterId, uint id, bool enable)
        {
            if (s_useNewApi)
            {
                var state = new DISPLAYCONFIG_SET_HDR_STATE();
                state.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE;
                state.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SET_HDR_STATE));
                state.header.adapterId = adapterId;
                state.header.id = id;
                state.value = enable ? 1u : 0u;

                int err = DisplayConfigSetDeviceInfo(ref state.header);
                if (err != ERROR_SUCCESS)
                    throw new Win32Exception(err);
            }
            else
            {
                var state = new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE();
                state.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE;
                state.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE));
                state.header.adapterId = adapterId;
                state.header.id = id;
                state.enableAdvancedColor = enable ? 1u : 0u;

                int err = DisplayConfigSetDeviceInfo(ref state.header);
                if (err != ERROR_SUCCESS)
                    throw new Win32Exception(err);
            }
        }
    }
}

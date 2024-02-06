using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using R3E.Data;
using ReHUD;

namespace R3E
{
    internal delegate Task SharedMemoryCallback(Shared data);

    class SharedMemory : IDisposable
    {
        private bool found = false;
        private bool Mapped
        {
            get { return _file != null; }
        }

        private Shared _data;
        private MemoryMappedFile? _file;
        private byte[]? _buffer;
        private static TimeSpan timeInterval = TimeSpan.FromMilliseconds(16.6); // ~60fps

        public static long FrameRate
        {
            get { return (long)(1000.0 / timeInterval.TotalMilliseconds); }
            set {
                timeInterval = TimeSpan.FromMilliseconds(1000.0 / value);
            }
        }

        public void Dispose()
        {
            if (_file != null)
                _file.Dispose();
        }

        public static bool IsRunning { get; private set; } = false;

        public async Task Run(SharedMemoryCallback callback)
        {
            var timeLast = DateTime.UtcNow;

            Startup.logger.Info("Looking for RRRE.exe...");

            while (true)
            {
                var timeNow = DateTime.UtcNow;

                var timeDiff = timeNow.Subtract(timeLast);
                if (timeDiff < timeInterval)
                {
                    await Task.Delay(timeInterval - timeDiff);
                    continue;
                }

                timeLast = timeNow;

                if (Utilities.IsRaceRoomRunning() && !Mapped)
                {
                    if (!found)
                        Startup.logger.Info("Found RRRE.exe, mapping shared memory...");

                    found = true;

                    if (Map())
                    {
                        Startup.logger.Info("Memory mapped successfully");

                        _buffer = new byte[Marshal.SizeOf(typeof(Shared))];
                    }
                }

                if (Mapped && Read())
                {
                    IsRunning = true;
                    await callback(_data);
                }
                else
                {
                    IsRunning = false;
                }
            }
        }

        private bool Map()
        {
            try
            {
                _file = MemoryMappedFile.OpenExisting(Constant.sharedMemoryName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Read()
        {
            if (_file == null)
                return false;

            try
            {
                var _view = _file.CreateViewStream();
                BinaryReader _stream = new(_view);
                _buffer = _stream.ReadBytes(Marshal.SizeOf(typeof(Shared)));
                GCHandle _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);

                var res = Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(Shared));
                if (res == null)
                    return false;

                _data = (Shared)res;
                _handle.Free();

                return true;
            }
            catch (Exception e)
            {
                Startup.logger.Error("Error reading shared memory", e);
                return false;
            }
        }
    }
}
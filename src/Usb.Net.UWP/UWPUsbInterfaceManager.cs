﻿using Device.Net;
using Device.Net.Exceptions;
using Device.Net.UWP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using windowsUsbDevice = Windows.Devices.Usb.UsbDevice;

namespace Usb.Net.UWP
{
    public class UWPUsbInterfaceManager : UWPDeviceBase<windowsUsbDevice>, IUsbInterfaceManager
    {
        #region Fields
        private bool disposed;
        private readonly ushort? _WriteBufferSize;
        private readonly ushort? _ReadBufferSize;
        #endregion

        #region Public Properties
        public UsbInterfaceManager UsbInterfaceHandler { get; }
        #endregion

        #region Public Override Properties
        public override ushort WriteBufferSize => _WriteBufferSize ?? WriteUsbInterface.WriteBufferSize;
        public override ushort ReadBufferSize => _ReadBufferSize ?? ReadUsbInterface.WriteBufferSize;

        public IUsbInterface ReadUsbInterface
        {
            get => UsbInterfaceHandler.ReadUsbInterface;
            set => UsbInterfaceHandler.ReadUsbInterface = value;
        }

        public IUsbInterface WriteUsbInterface
        {
            get => UsbInterfaceHandler.WriteUsbInterface;
            set => UsbInterfaceHandler.WriteUsbInterface = value;
        }

        public IList<IUsbInterface> UsbInterfaces => UsbInterfaceHandler.UsbInterfaces;
        #endregion

        #region Constructors
        public UWPUsbInterfaceManager(
            ConnectedDeviceDefinition connectedDeviceDefinition,
            ILoggerFactory loggerFactory = null,
            ushort? readBufferSize = null,
            ushort? writeBufferSize = null) : base(connectedDeviceDefinition?.DeviceId, loggerFactory, (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<UWPUsbInterfaceManager>())
        {
            ConnectedDeviceDefinition = connectedDeviceDefinition ?? throw new ArgumentNullException(nameof(connectedDeviceDefinition));
            UsbInterfaceHandler = new UsbInterfaceManager(loggerFactory);
            _WriteBufferSize = writeBufferSize;
            _ReadBufferSize = readBufferSize;
        }
        #endregion

        #region Private Methods
        public override async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (disposed) throw new ValidationException(Messages.DeviceDisposedErrorMessage);

            await GetDeviceAsync(DeviceId, cancellationToken);

            if (ConnectedDevice != null)
            {
                if (ConnectedDevice.Configuration.UsbInterfaces == null || ConnectedDevice.Configuration.UsbInterfaces.Count == 0)
                {
                    ConnectedDevice.Dispose();
                    throw new DeviceException(Messages.ErrorMessageNoInterfaceFound);
                }

                var interfaceIndex = 0;
                foreach (var usbInterface in ConnectedDevice.Configuration.UsbInterfaces)
                {
                    var uwpUsbInterface = new UWPUsbInterface(usbInterface, Logger, _ReadBufferSize, _WriteBufferSize);

                    UsbInterfaceHandler.UsbInterfaces.Add(uwpUsbInterface);
                    interfaceIndex++;
                }
            }
            else
            {
                throw new DeviceException(Messages.GetErrorMessageCantConnect(DeviceId));
            }

            UsbInterfaceHandler.RegisterDefaultInterfaces();
        }

        protected override IAsyncOperation<windowsUsbDevice> FromIdAsync(string id) => windowsUsbDevice.FromIdAsync(id);

        #endregion

        #region Public Methods
        public override void Dispose()
        {
            if (disposed) return;
            disposed = true;

            UsbInterfaceHandler?.Dispose();
            base.Dispose();
        }

        public Task WriteAsync(byte[] data) => WriteUsbInterface.WriteAsync(data);

        public Task<ConnectedDeviceDefinition> GetConnectedDeviceDefinitionAsync(CancellationToken cancellationToken = default) => Task.FromResult(ConnectedDeviceDefinition);
        #endregion
    }
}

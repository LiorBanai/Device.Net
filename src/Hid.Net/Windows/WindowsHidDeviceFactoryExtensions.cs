using Device.Net;
using Device.Net.Exceptions;
using Device.Net.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hid.Net.Windows
{

    public static class WindowsHidDeviceFactoryExtensions
    {
        public static IDeviceFactory CreateWindowsHidDeviceFactory(
        ILoggerFactory loggerFactory = null,
        IHidApiService hidApiService = null,
        Guid? classGuid = null,
        ushort? readBufferSize = null,
        ushort? writeBufferSize = null)
        {
            return CreateWindowsHidDeviceFactory(
                new ReadOnlyCollection<FilterDeviceDefinition>(new List<FilterDeviceDefinition>()),
                loggerFactory,
                hidApiService,
                classGuid,
                readBufferSize,
                writeBufferSize
                );
        }

        //TODO: this is named incorrectly. This needs to be fixed

        public static IDeviceFactory CreateWindowsHidDeviceManager(
        this FilterDeviceDefinition filterDeviceDefinition,
        ILoggerFactory loggerFactory = null,
        IHidApiService hidApiService = null,
        Guid? classGuid = null,
        ushort? readBufferSize = null,
        ushort? writeBufferSize = null)
        {
            var factory = CreateWindowsHidDeviceFactory(
                filterDeviceDefinition,
                loggerFactory,
                hidApiService,
                classGuid,
                readBufferSize,
                writeBufferSize
                );

            return new DeviceManager(new ReadOnlyCollection<IDeviceFactory>(new List<IDeviceFactory> { factory }), loggerFactory);
        }

        public static IDeviceFactory CreateWindowsHidDeviceFactory(
        this FilterDeviceDefinition filterDeviceDefinition,
        ILoggerFactory loggerFactory = null,
        IHidApiService hidApiService = null,
        Guid? classGuid = null,
        ushort? readBufferSize = null,
        ushort? writeBufferSize = null,
        byte? defaultReportId = null)
        {
            return CreateWindowsHidDeviceFactory(
                new ReadOnlyCollection<FilterDeviceDefinition>(new List<FilterDeviceDefinition> { filterDeviceDefinition }),
                loggerFactory,
                hidApiService,
                classGuid,
                readBufferSize,
                writeBufferSize,
                defaultReportId
                );
        }

        public static IDeviceFactory CreateWindowsHidDeviceFactory(
            this IEnumerable<FilterDeviceDefinition> filterDeviceDefinitions,
            ILoggerFactory loggerFactory = null,
            IHidApiService hidApiService = null,
            Guid? classGuid = null,
            ushort? readBufferSize = null,
            ushort? writeBufferSize = null,
            byte? defaultReportId = null)
        {
            if (filterDeviceDefinitions == null) throw new ArgumentNullException(nameof(filterDeviceDefinitions));

            loggerFactory ??= NullLoggerFactory.Instance;

            var selectedHidApiService = hidApiService ?? new WindowsHidApiService(loggerFactory);

            classGuid ??= selectedHidApiService.GetHidGuid();

            var windowsDeviceEnumerator = new WindowsDeviceEnumerator(
                loggerFactory.CreateLogger<WindowsDeviceEnumerator>(),
                classGuid.Value,
                (d, guid) => GetDeviceDefinition(d, selectedHidApiService, loggerFactory.CreateLogger(nameof(WindowsHidDeviceFactoryExtensions))),
                c => Task.FromResult(!filterDeviceDefinitions.Any() || filterDeviceDefinitions.FirstOrDefault(f => f.IsDefinitionMatch(c, DeviceType.Hid)) != null)
                );

            return new DeviceFactory(
                loggerFactory,
                windowsDeviceEnumerator.GetConnectedDeviceDefinitionsAsync,
                (c, cancellationToken) => Task.FromResult<IDevice>(new WindowsHidDevice
                (
                    c.DeviceId,
                    loggerFactory: loggerFactory,
                    hidService: selectedHidApiService,
                    readBufferSize: readBufferSize,
                    writeBufferSize: writeBufferSize,
                    defaultReportId: defaultReportId
                )),
                (c, cancellationToken) => Task.FromResult(c.DeviceType == DeviceType.Hid));
        }

        private static ConnectedDeviceDefinition GetDeviceDefinition(string deviceId, IHidApiService HidService, ILogger logger)
        {
            logger ??= NullLogger.Instance;

            using var logScope = logger.BeginScope("DeviceId: {deviceId} Call: {call}", deviceId, nameof(GetDeviceDefinition));

            try
            {
                using var safeFileHandle = HidService.CreateReadConnection(deviceId, FileAccessRights.None);

                if (safeFileHandle.IsInvalid) throw new DeviceException($"{nameof(HidService.CreateReadConnection)} call with Id of {deviceId} failed.");

                logger.LogDebug(Messages.InformationMessageFoundDevice);

                return HidService.GetDeviceDefinition(deviceId, safeFileHandle);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Messages.ErrorMessageCouldntGetDevice);
                return null;
            }
        }
    }

}

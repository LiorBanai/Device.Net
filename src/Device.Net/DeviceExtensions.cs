using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Device.Net
{
    public static class DeviceExtensions
    {
        public static IDeviceFactory Aggregate(this IList<IDeviceFactory> deviceFactories, ILoggerFactory loggerFactory = null)
            => deviceFactories == null ? throw new ArgumentNullException(nameof(deviceFactories)) :
            new DeviceManager(new ReadOnlyCollection<IDeviceFactory>(deviceFactories), loggerFactory);


        public static IDeviceFactory Aggregate(this IDeviceFactory deviceFactory, IDeviceFactory newDeviceFactory, ILoggerFactory loggerFactory = null)
            => deviceFactory == null ? throw new ArgumentNullException(nameof(deviceFactory)) :
            new DeviceManager(
                new ReadOnlyCollection<IDeviceFactory>(
                    new ReadOnlyCollection<IDeviceFactory>(
                        new List<IDeviceFactory> { deviceFactory, newDeviceFactory })), loggerFactory);

        public static DeviceDataStreamer CreateDeviceDataStreamer(
            this IDeviceFactory deviceFactory,
            ProcessData processData,
            Func<IDevice, Task> initializeFunc = null) =>
            new DeviceDataStreamer(
                processData,
                deviceFactory,
                initializeFunc: initializeFunc);

        public static bool IsDefinitionMatch(this FilterDeviceDefinition filterDevice, ConnectedDeviceDefinition actualDevice, DeviceType deviceType)
        {
            if (actualDevice == null) throw new ArgumentNullException(nameof(actualDevice));

            if (filterDevice == null) return true;

            var vendorIdPasses = !filterDevice.VendorId.HasValue || filterDevice.VendorId == actualDevice.VendorId;
            var productIdPasses = !filterDevice.ProductId.HasValue || filterDevice.ProductId == actualDevice.ProductId;
            var deviceTypePasses = actualDevice.DeviceType == deviceType;
            var usagePagePasses = !filterDevice.UsagePage.HasValue || filterDevice.UsagePage == actualDevice.UsagePage;
            var classGuidPasses = !filterDevice.ClassGuid.HasValue || filterDevice.ClassGuid == actualDevice.ClassGuid;

            var returnValue =
                vendorIdPasses &&
                productIdPasses &&
                deviceTypePasses &&
                usagePagePasses &&
                classGuidPasses;

            return returnValue;
        }

        public static async Task<IDevice> GetFirstDeviceAsync(this IDeviceFactory deviceFactory)
            => deviceFactory != null ?
            await deviceFactory.GetDeviceAsync(await (await deviceFactory.GetConnectedDeviceDefinitionsAsync()).FirstOrDefaultAsync())
            : throw new ArgumentNullException(nameof(deviceFactory));

        public static async Task<IDevice> ConnectFirstAsync(this IDeviceFactory deviceFactory)
        {
            var device = await GetFirstDeviceAsync(deviceFactory);
            await device.InitializeAsync();
            return device;
        }

    }
}
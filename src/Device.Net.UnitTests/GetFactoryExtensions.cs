﻿
using Hid.Net.Windows;
using Microsoft.Extensions.Logging;
using Usb.Net.Windows;

namespace Device.Net.UnitTests
{
    public static class GetFactoryExtensions
    {
        public static IDeviceFactory GetUsbDeviceFactory(this FilterDeviceDefinition filterDeviceDefinition, ILoggerFactory loggerFactory) =>
            filterDeviceDefinition.CreateWindowsUsbDeviceFactory(loggerFactory);

        public static IDeviceFactory GetHidDeviceFactory(this FilterDeviceDefinition filterDeviceDefinition, ILoggerFactory loggerFactory, byte? defultReportId = null) =>
            filterDeviceDefinition.CreateWindowsHidDeviceFactory(loggerFactory, defaultReportId: defultReportId);
    }
}


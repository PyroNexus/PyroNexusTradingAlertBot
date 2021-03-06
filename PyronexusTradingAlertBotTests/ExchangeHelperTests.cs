using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PyroNexusTradingAlertBot.Storage;
using System;
using System.Data.Common;
using PyroNexusTradingAlertBot.Helpers;

namespace PyronexusTradingAlertBotTests
{
    [TestClass]
    public class ExchangeHelperTests
    {
        //[TestMethod]
        //public void Test_Write()
        //{
        //    var mockLogger = new Mock<ILogger<ExchangeHelperTests>>();
        //    mockLogger.Setup(m => m.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        //    mockLogger.Setup(m => m.Log(
        //        It.IsAny<LogLevel>(),
        //        It.IsAny<EventId>(),
        //        It.IsAny<It.IsAnyType>(),
        //        It.IsAny<Exception>(),
        //        (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
        //        ));
        //    var logger = new CryptoExchangeHelper.TextWriterILogger(mockLogger.Object);

        //    for (int i = 0; i < 1000000000; i++)
        //    {
        //        logger.Write('\r');
        //        logger.Write('\n');
        //    }
            
        //}
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PyroNexusTradingAlertBot.Storage;
using System;
using System.Data.Common;

namespace PyronexusTradingAlertBotTests
{
    

    [TestClass]
    public class SqliteTests
    {

        [TestMethod]
        public void TestBuildSchema_FailsToBuildSchema()
        {
            var mockDataReader = new Mock<DbDataReader>();
            mockDataReader.Setup(m => m.HasRows).Returns(() => false);

            var mockSqliteConnection = new Mock<SqliteConnection>();
            var mockLogger = new Mock<ILogger<SqliteService>>();

            var mockSqlite = new Mock<SqliteService>(mockSqliteConnection.Object, mockLogger.Object);
            mockSqlite.Setup(m => m.ExecuteNonQuery(It.IsAny<string>()));
            mockSqlite.Setup(m => m.ExecuteReader(It.IsAny<string>())).Returns(() => mockDataReader.Object);

            var ex = Assert.ThrowsException<Exception>(() => mockSqlite.Object.BuildSchema());
            Assert.AreEqual(ex.Message, "Schema was not created as expected");
        }
    }
}

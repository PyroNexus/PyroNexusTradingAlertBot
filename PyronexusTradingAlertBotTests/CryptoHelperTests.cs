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
    public class CryptoHelperTests
    {
        [TestMethod]
        public void Test_EncryptDecryptNewKey()
        {
            var plaintext = "hello";

            var helper = new CryptoHelper(CryptoHelper.NewKeyIV());

            var encrypted = helper.EncryptString(plaintext);

            var decrypted = helper.DecryptString(encrypted);

            Assert.IsTrue(plaintext == decrypted);
        }

        [TestMethod]
        public void Test_Decrypt()
        {
            var helper = new CryptoHelper("s/XmBX61n7hqVgx1tzxrCMqysnAXKKafKpOOrpcvi8E=", "DPmvJtZDGVgr2JJ68foltA==");

            var decrypted = helper.DecryptString("It9U488Qsp5DYAzR2zty9w==");

            Assert.IsTrue("hello" == decrypted);
        }

        [TestMethod]
        public void Test_Encrypt()
        {
            var helper = new CryptoHelper("s/XmBX61n7hqVgx1tzxrCMqysnAXKKafKpOOrpcvi8E=", "DPmvJtZDGVgr2JJ68foltA==");

            var encrypted = helper.EncryptString("hello");

            Assert.IsTrue("It9U488Qsp5DYAzR2zty9w==" == encrypted);
        }

        //[TestMethod]
        //public void Test_Encrypt2()
        //{
        //    var helper = new CryptoHelper();

        //    var encrypted = helper.EncryptString("");
        //    var encrypted2 = helper.EncryptString("");
        //    var encrypted3 = helper.EncryptString("");
        //}
    }
}

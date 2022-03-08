using FileLoggerKata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using Xunit;

namespace FileLoggerTests
{
    [TestClass]
    public class FileLoggerTests
    {
        private static readonly DateTime Saturday = new DateTime(2018, 9, 1);
        private static readonly DateTime Sunday = new DateTime(2018, 9, 2);
        private const string TestMessage = "STANDARD_LOG_MESSAGE";
        private FileLogger Logger { get; }
        private Mock<IFileSystem> FileSystemMock { get; }
        private Mock<IDateProvider> DateProviderMock { get; }
        private DateTime DefaultToday => new DateTime(2018, 1, 1); // monday
        private string DefaultLogFileName => $"log{DefaultToday:yyyyMMdd}.txt";

        public FileLoggerTests()
        {
            FileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);
            FileSystemMock.Setup(fs => fs.Append(It.IsNotNull<string>(), It.IsNotNull<string>()));
            FileSystemMock.Setup(fs => fs.Create(It.IsNotNull<string>()));
            FileSystemMock.Setup(fs => fs.Exists(It.IsNotNull<string>())).Returns(true);
            FileSystemMock.Setup(fs => fs.GetLastWriteTime(It.IsNotNull<string>())).Returns(DateTime.Now);
            FileSystemMock.Setup(fs => fs.Rename(It.IsNotNull<string>(), It.IsNotNull<string>()));

            DateProviderMock = new Mock<IDateProvider>(MockBehavior.Strict);
            DateProviderMock.Setup(dp => dp.Today).Returns(DefaultToday);

            Logger = new FileLogger(FileSystemMock.Object, DateProviderMock.Object);
        }

        [TestMethod]
        public void LogAppendsMessageToExistingLogFile()
        {
            Logger.Log(TestMessage);

            FileSystemMock.Verify(fs => fs.Exists(DefaultLogFileName), Times.Once);
            FileSystemMock.Verify(fs => fs.Create(DefaultLogFileName), Times.Never);
            FileSystemMock.Verify(fs => fs.Append(DefaultLogFileName, TestMessage), Times.Once);
        }

        [TestMethod]
        public void LogCreatesLogFileIfNotExistsAndAppendsMessage()
        {
            FileSystemMock.Setup(fs => fs.Exists(DefaultLogFileName)).Returns(false);

            Logger.Log(TestMessage);

            FileSystemMock.Verify(fs => fs.Exists(DefaultLogFileName), Times.Once);
            FileSystemMock.Verify(fs => fs.Create(DefaultLogFileName), Times.Once);
            FileSystemMock.Verify(fs => fs.Append(DefaultLogFileName, TestMessage), Times.Once);
        }

        [TestMethod]
        public void LoggerUsersIDateProviderToChooseLogFile()
        {
            Logger.Log(TestMessage);

            DateProviderMock.VerifyGet(dp => dp.Today, Times.AtLeastOnce);
            FileSystemMock.Verify(fs => fs.Append(DefaultLogFileName, TestMessage), Times.Once);
        }

        [TestMethod]
        public void LoggerLogsToWeekendTxtFileOnSaturdays()
        {
            var expectedLogFile = "weekend.txt";
            DateProviderMock.Setup(dp => dp.Today).Returns(Saturday);

            Logger.Log(TestMessage);

            DateProviderMock.VerifyGet(dp => dp.Today, Times.AtLeastOnce);
            FileSystemMock.Verify(fs => fs.Append(expectedLogFile, TestMessage), Times.Once);
        }

        [TestMethod]
        public void LoggerLogsToWeekendTxtFileOnSundays()
        {
            var expectedLogFile = "weekend.txt";
            DateProviderMock.Setup(dp => dp.Today).Returns(Sunday);

            Logger.Log(TestMessage);

            DateProviderMock.VerifyGet(dp => dp.Today, Times.AtLeastOnce);
            FileSystemMock.Verify(fs => fs.Append(expectedLogFile, TestMessage), Times.Once);
        }

        [TestMethod]
        public void LoggerAppendsToWeekendTxtLogWithinSameWeekend()
        {
            var expectedLogFile = "weekend.txt";
            DateProviderMock.Setup(dp => dp.Today).Returns(Saturday);

            Logger.Log(TestMessage);

            DateProviderMock.Setup(dp => dp.Today).Returns(Sunday);

            Logger.Log(TestMessage);

            DateProviderMock.VerifyGet(dp => dp.Today, Times.AtLeastOnce);
            FileSystemMock.Verify(fs => fs.Append(expectedLogFile, TestMessage), Times.Exactly(2));
        }

        [TestMethod]
        public void WeekendLogFileRotatesWhenNewLogStartsOnSaturday()
        {
            var lastSunday = Saturday.AddDays(-6);
            var secondOfLastSunday = lastSunday.Add(new TimeSpan(23, 59, 59));
            var expectedLogFile = "weekend.txt";
            var expectedArchivedLogFile = $"weekend-{lastSunday:yyyyMMdd}.txt";
            DateProviderMock.Setup(dp => dp.Today).Returns(Saturday);
            FileSystemMock.Setup(fs => fs.GetLastWriteTime(expectedLogFile)).Returns(secondOfLastSunday);

            Logger.Log(TestMessage);

            FileSystemMock.Verify(fs => fs.Rename(expectedLogFile, expectedArchivedLogFile), Times.Once);
            FileSystemMock.Verify(fs => fs.GetLastWriteTime(expectedLogFile), Times.AtLeastOnce);
        }

        [TestMethod]
        public void WeekendLogFileRotatesWhenNewLogStartsOnSunday()
        {
            var lastSunday = Sunday.AddDays(-7);
            var secondOfLastSunday = lastSunday.Add(new TimeSpan(23, 59, 59));
            var expectedLogFile = "weekend.txt";
            var expectedArchivedLogFile = $"weekend-{lastSunday:yyyyMMdd}.txt";
            DateProviderMock.Setup(dp => dp.Today).Returns(Sunday);
            FileSystemMock.Setup(fs => fs.GetLastWriteTime(expectedLogFile)).Returns(secondOfLastSunday);

            Logger.Log(TestMessage);

            FileSystemMock.Verify(fs => fs.Rename(expectedLogFile, expectedArchivedLogFile), Times.Once);
            FileSystemMock.Verify(fs => fs.GetLastWriteTime(expectedLogFile), Times.AtLeastOnce);
        }
    }
}

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PredictLeague.Controllers;
using Xunit;

namespace PredictLeague.Tests
{
    public class FootballNewsServiceTests
    {
        [Fact]
        public async Task GetNewsAsync_WithEmptyApiKey_ReturnsEmptyList()
        {
            // Arrange
            var mockHttp = new HttpClient();
            var mockLogger = new Mock<ILogger<FootballNewsService>>();
            var mockConfig = new Mock<IConfiguration>();
            
            // Setup IConfiguration to return empty API key
            mockConfig.Setup(c => c["ApiKeys:NewsDataIo"]).Returns(string.Empty);

            var service = new FootballNewsService(mockHttp, mockLogger.Object, mockConfig.Object);

            // Act
            var result = await service.GetNewsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNewsAsync_WithNullApiKey_ReturnsEmptyList()
        {
            // Arrange
            var mockHttp = new HttpClient();
            var mockLogger = new Mock<ILogger<FootballNewsService>>();
            var mockConfig = new Mock<IConfiguration>();
            
            // Setup IConfiguration to return null API key
            mockConfig.Setup(c => c["ApiKeys:NewsDataIo"]).Returns((string)null);

            var service = new FootballNewsService(mockHttp, mockLogger.Object, mockConfig.Object);

            // Act
            var result = await service.GetNewsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}

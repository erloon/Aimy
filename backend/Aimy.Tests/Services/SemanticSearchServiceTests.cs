using Aimy.Core.Application.Configuration;
using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Services;
using Aimy.Core.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Aimy.Tests.Services;

[TestFixture]
public class SemanticSearchServiceTests
{
    private Mock<IVectorSearchPort> _vectorSearchPortMock = null!;
    private Mock<IKnowledgeItemRepository> _knowledgeItemRepositoryMock = null!;
    private Mock<IOptions<SemanticSearchOptions>> _optionsMock = null!;
    private SemanticSearchService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _vectorSearchPortMock = new Mock<IVectorSearchPort>();
        _knowledgeItemRepositoryMock = new Mock<IKnowledgeItemRepository>();
        _optionsMock = new Mock<IOptions<SemanticSearchOptions>>();

        _optionsMock
            .Setup(o => o.Value)
            .Returns(new SemanticSearchOptions { MaxResults = 50, ScoreThreshold = 0.85 });

        _sut = new SemanticSearchService(
            _vectorSearchPortMock.Object,
            _optionsMock.Object,
            _knowledgeItemRepositoryMock.Object);
    }

    [Test]
    public async Task SearchAsync_WithValidQuery_ReturnsPagedResults()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var item = new KnowledgeItem { Title = "Test Item", SourceUploadId = uploadId };

        _vectorSearchPortMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new VectorSearchResult(uploadId.ToString(), 0.95)]);

        _knowledgeItemRepositoryMock
            .Setup(r => r.GetBySourceUploadIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        // Act
        var result = await _sut.SearchAsync("test query", 1, 10, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items[0].Score.Should().Be(0.95);
        result.Items[0].ItemResponse.Title.Should().Be("Test Item");
    }

    [Test]
    public async Task SearchAsync_WithEmptyVectorResults_ReturnsEmptyPagedResult()
    {
        // Arrange
        _vectorSearchPortMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _knowledgeItemRepositoryMock
            .Setup(r => r.GetBySourceUploadIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.SearchAsync("test query", 1, 10, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Test]
    public async Task SearchAsync_WithMissingKnowledgeItems_SkipsOrphanedResults()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var orphanedUploadId = Guid.NewGuid();
        var item = new KnowledgeItem { Title = "Valid Item", SourceUploadId = uploadId };

        _vectorSearchPortMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new VectorSearchResult(uploadId.ToString(), 0.95),
                new VectorSearchResult(orphanedUploadId.ToString(), 0.90)  // no matching KnowledgeItem
            ]);

        _knowledgeItemRepositoryMock
            .Setup(r => r.GetBySourceUploadIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);  // only returns item for first uploadId

        // Act
        var result = await _sut.SearchAsync("test query", 1, 10, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].ItemResponse.Title.Should().Be("Valid Item");
    }

    [Test]
    public async Task SearchAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var uploadIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var items = uploadIds.Select((id, i) => new KnowledgeItem { Title = $"Item {i}", SourceUploadId = id }).ToList();
        var vectorResults = uploadIds.Select((id, i) => new VectorSearchResult(id.ToString(), 0.95 - i * 0.01)).ToList();

        _vectorSearchPortMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResults);

        _knowledgeItemRepositoryMock
            .Setup(r => r.GetBySourceUploadIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var result = await _sut.SearchAsync("test query", page: 2, pageSize: 2, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }

    [Test]
    public async Task SearchAsync_WithMultipleItemsPerUpload_IncludesAllItems()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var item1 = new KnowledgeItem { Title = "Item A", SourceUploadId = uploadId };
        var item2 = new KnowledgeItem { Title = "Item B", SourceUploadId = uploadId };

        _vectorSearchPortMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new VectorSearchResult(uploadId.ToString(), 0.92)]);

        _knowledgeItemRepositoryMock
            .Setup(r => r.GetBySourceUploadIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item1, item2]);

        // Act
        var result = await _sut.SearchAsync("test query", 1, 10, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Select(r => r.ItemResponse.Title).Should().BeEquivalentTo(["Item A", "Item B"]);
        result.Items.Should().AllSatisfy(r => r.Score.Should().Be(0.92));
    }

    [Test]
    public async Task SearchAsync_ResultsOrderedByScoreDescending()
    {
        // Arrange
        var upload1 = Guid.NewGuid();
        var upload2 = Guid.NewGuid();
        var upload3 = Guid.NewGuid();
        var item1 = new KnowledgeItem { Title = "Low Score", SourceUploadId = upload1 };
        var item2 = new KnowledgeItem { Title = "High Score", SourceUploadId = upload2 };
        var item3 = new KnowledgeItem { Title = "Mid Score", SourceUploadId = upload3 };

        _vectorSearchPortMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new VectorSearchResult(upload1.ToString(), 0.80),
                new VectorSearchResult(upload2.ToString(), 0.99),
                new VectorSearchResult(upload3.ToString(), 0.90),
            ]);

        _knowledgeItemRepositoryMock
            .Setup(r => r.GetBySourceUploadIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item1, item2, item3]);

        // Act
        var result = await _sut.SearchAsync("test query", 1, 10, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Score.Should().Be(0.99);
        result.Items[1].Score.Should().Be(0.90);
        result.Items[2].Score.Should().Be(0.80);
        result.Items[0].ItemResponse.Title.Should().Be("High Score");
    }

    [Test]
    public async Task SearchAsync_UsesConfigValues_NotHardcoded()
    {
        // Arrange
        const int customMaxResults = 25;
        const double customScoreThreshold = 0.70;

        _optionsMock
            .Setup(o => o.Value)
            .Returns(new SemanticSearchOptions { MaxResults = customMaxResults, ScoreThreshold = customScoreThreshold });

        _sut = new SemanticSearchService(
            _vectorSearchPortMock.Object,
            _optionsMock.Object,
            _knowledgeItemRepositoryMock.Object);

        _vectorSearchPortMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _knowledgeItemRepositoryMock
            .Setup(r => r.GetBySourceUploadIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _sut.SearchAsync("test query", 1, 10, CancellationToken.None);

        // Assert
        _vectorSearchPortMock.Verify(
            p => p.SearchAsync(
                It.IsAny<string>(),
                customMaxResults,
                customScoreThreshold,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
